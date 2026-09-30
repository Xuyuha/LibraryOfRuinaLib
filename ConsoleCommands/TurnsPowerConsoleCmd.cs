using System.Linq;
using System.Reflection;
using LibraryLib.Commands;
using LibraryLib.Models;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace LibraryLib.ConsoleCommands;

/// <summary>
///     控制台命令：turnspower [powername] [amount] [turns] [ispermanent] [index]
///     用于快速给指定单位按正常规则叠加施加带持续回合/层数计划的 Library 能力。
/// </summary>
public sealed class TurnsPowerConsoleCmd : AbstractConsoleCmd
{
	private static List<PowerModel>? _allPowers;

	private static readonly MethodInfo ApplyTurnsMethod = typeof(LibraryPowerCmd)
		.GetMethods(BindingFlags.Public | BindingFlags.Static)
		.Single(m => m.Name == nameof(LibraryPowerCmd.Apply) && m.IsGenericMethodDefinition
			&& m.GetParameters() is { Length: 6 } applyParams
			&& applyParams[0].ParameterType == typeof(Creature)
			&& applyParams[1].ParameterType == typeof(decimal)
			&& applyParams[2].ParameterType == typeof(int)
			&& applyParams[5].ParameterType == typeof(bool));

	private static readonly MethodInfo ApplyWithDurationMethod = typeof(LibraryDurationPowerModel)
		.GetMethods(BindingFlags.Public | BindingFlags.Static)
		.Single(m => m.Name == nameof(LibraryDurationPowerModel.ApplyWithDuration) && m.IsGenericMethodDefinition
			&& m.GetParameters() is { Length: 6 } durationParams
			&& durationParams[0].ParameterType == typeof(Creature)
			&& durationParams[1].ParameterType == typeof(decimal)
			&& durationParams[2].ParameterType == typeof(int)
			&& durationParams[5].ParameterType == typeof(bool));

	public override string CmdName => "turnspower";

	public override string Args => "<powername:string> <amount:int> <turns:int> <ispermanent:bool> <index:int>";

	public override string Description => "Stack a Library turns/duration power on target index (0 is player) using normal stacking rules.";

	public override bool IsNetworked => true;

	private static IEnumerable<PowerModel> AllPowers
	{
		get
		{
			if (_allPowers == null)
			{
				_allPowers = ModelDb.AllAbstractModelSubtypes
					.Where(t => typeof(LibraryTurnsPowerModel).IsAssignableFrom(t) || typeof(LibraryDurationPowerModel).IsAssignableFrom(t))
					.Select(ModelDb.DebugPower)
					.ToList();
			}
			return _allPowers;
		}
	}

	public override CmdResult Process(Player? issuingPlayer, string[] args)
	{
		if (!CombatManager.Instance.IsInProgress)
		{
			return new CmdResult(success: false, "This doesn't appear to be a combat!");
		}
		if (args.Length != 5)
		{
			return new CmdResult(success: false, "There must be 5 args: <powername> <amount> <turns> <ispermanent> <index>.");
		}
		string powerId = args[0].ToUpperInvariant();
		PowerModel? power = AllPowers.FirstOrDefault(p => p.Id.Entry == powerId);
		if (power == null)
		{
			return new CmdResult(success: false, "The power id " + powerId + " does not exist.");
		}
		if (!int.TryParse(args[1], out int amount))
		{
			return new CmdResult(success: false, "Arg 2 must be the amount of power to be applied.");
		}
		if (!int.TryParse(args[2], out int turns))
		{
			return new CmdResult(success: false, "Arg 3 must be the number of turns.");
		}
		if (!TryParseBool(args[3], out bool isPermanent))
		{
			return new CmdResult(success: false, "Arg 4 must be true/false or 1/0.");
		}
		if (!isPermanent && turns < 0)
		{
			return new CmdResult(success: false, "Arg 3 must be non-negative when ispermanent is false.");
		}
		if (!int.TryParse(args[4], out int index))
		{
			return new CmdResult(success: false, "Arg 5 must be the target index.");
		}
		IReadOnlyList<Creature> creatures = CombatManager.Instance.DebugOnlyGetState()!.Creatures;
		if (index < 0 || index >= creatures.Count)
		{
			return new CmdResult(success: false, $"Invalid target index {index}. Valid range: 0-{creatures.Count - 1}");
		}
		Creature creature = creatures[index];
		Task task = ApplyTurnsPowerAsync(power.GetType(), creature, amount, turns, isPermanent);
		return new CmdResult(task, success: true, "AppliedPower: [" + (creature.IsPlayer ? "PLAYER" : creature.Monster!.Id.Entry) + "]");
	}

	private static async Task ApplyTurnsPowerAsync(Type powerType, Creature target, int amount, int turns, bool isPermanent)
	{
		int effectiveTurns = isPermanent ? -1 : turns;
		if (typeof(LibraryDurationPowerModel).IsAssignableFrom(powerType))
		{
			// Duration 类按 ApplyWithDuration 规则叠加：合并剩余回合、层数取较大值。
			MethodInfo applyWithDuration = ApplyWithDurationMethod.MakeGenericMethod(powerType);
			await (Task)applyWithDuration.Invoke(null, new object?[] { target, (decimal)amount, effectiveTurns, null, null, false })!;
			return;
		}
		// Turns 类按 Apply 规则叠加：层数相加，并为每次施加追加独立的衰减计划。
		MethodInfo apply = ApplyTurnsMethod.MakeGenericMethod(powerType);
		await (Task)apply.Invoke(null, new object?[] { target, (decimal)amount, effectiveTurns, null, null, false })!;
	}

	private static bool TryParseBool(string input, out bool result)
	{
		if (bool.TryParse(input, out result))
		{
			return true;
		}
		if (int.TryParse(input, out int numeric) && (numeric == 0 || numeric == 1))
		{
			result = numeric == 1;
			return true;
		}
		result = false;
		return false;
	}

	public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
	{
		if (args.Length <= 1)
		{
			List<string> candidates = AllPowers.Select(p => p.Id.Entry).ToList();
			return CompleteArgument(candidates, Array.Empty<string>(), args.FirstOrDefault() ?? "");
		}
		// 第四个参数是 ispermanent，补全 true/false/1/0。
		if (args.Length == 4)
		{
			return CompleteArgument(new[] { "true", "false", "1", "0" }, args.Take(3).ToArray(), args[3]);
		}
		// 第五个参数是目标索引（0 为玩家，之后为敌人），按当前战斗生物列表补全。
		if (args.Length >= 5 && CombatManager.Instance.IsInProgress)
		{
			IReadOnlyList<Creature> creatures = CombatManager.Instance.DebugOnlyGetState()!.Creatures;
			List<string> candidates = new List<string>(creatures.Count);
			for (int i = 0; i < creatures.Count; i++)
			{
				candidates.Add(i.ToString());
			}
			return CompleteArgument(candidates, args.Take(args.Length - 1).ToArray(), args[^1]);
		}
		return new CompletionResult
		{
			Type = CompletionType.Argument,
			ArgumentContext = CmdName
		};
	}
}
