using System.Linq;
using System.Threading.Tasks;
using LibraryLib.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.ConsoleCommands;

/// <summary>
///     控制台命令：chaodamage，参数与原版 damage 一致，仅造成混乱伤害。
/// </summary>
public sealed class ChaoDamageConsoleCmd : AbstractConsoleCmd
{
	public override string CmdName => "chaodamage";

	public override string Args => "<amount:int> <target-index:int>";

	public override string Description => "Deal chaos damage to all enemies, or target creature if index is given (0 is player).";

	public override bool IsNetworked => true;

	public override CmdResult Process(Player? issuingPlayer, string[] args)
	{
		if (!CombatManager.Instance.IsInProgress)
		{
			return new CmdResult(success: false, "This doesn't appear to be a combat!");
		}
		if (args.Length < 1 || args.Length > 2)
		{
			return new CmdResult(success: false, "There must be 1 or 2 args.");
		}
		if (!int.TryParse(args[0], out int amount))
		{
			return new CmdResult(success: false, "Arg 1 must be the amount of damage.");
		}
		if (amount < 0)
		{
			return new CmdResult(success: false, "The damage amount cannot be negative.");
		}
		CombatState combatState = CombatManager.Instance.DebugOnlyGetState()!;
		IEnumerable<Creature> targets;
		if (args.Length < 2)
		{
			targets = combatState.Enemies;
		}
		else
		{
			if (!int.TryParse(args[1], out int index))
			{
				return new CmdResult(success: false, "Arg 2 must be the target index if specified.");
			}
			if (index < 0 || index >= combatState.Creatures.Count)
			{
				return new CmdResult(success: false, $"Invalid target index {index}. Valid range: 0-{combatState.Creatures.Count - 1}");
			}
			targets = new[] { combatState.Creatures[index] };
		}
		IEnumerable<string> values = targets.Select(c => c.IsPlayer ? "PLAYER" : c.Monster!.Id.Entry);
		Task task = ChaosDamageAndCheckWinCondition(targets, amount);
		return new CmdResult(task, success: true, "ChaosDamaged: [" + string.Join(",", values) + "]");
	}

	public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
	{
		// 第一个参数是伤害数值，没有可补全候选。
		if (args.Length <= 1)
		{
			return new CompletionResult
			{
				Type = CompletionType.Argument,
				ArgumentContext = CmdName
			};
		}
		// 第二个参数是目标索引（0 为玩家，之后为敌人），按当前战斗生物列表补全。
		if (CombatManager.Instance.IsInProgress)
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

	private static async Task ChaosDamageAndCheckWinCondition(IEnumerable<Creature> creatures, int amount)
	{
		await LibraryCreatureCmd.ChaoDamage(
			new BlockingPlayerChoiceContext(),
			creatures.ToList(),
			amount,
			ValueProp.Unpowered,
			dealer: null,
			cardSource: null,
			cardPlay: null);
		await CombatManager.Instance.CheckWinCondition();
	}
}
