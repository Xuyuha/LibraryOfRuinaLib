#nullable enable
using Godot;
using HarmonyLib;
using LibraryLib.Combat;
using LibraryLib.Compat;
using LibraryLib.Entities.Creatures;
using LibraryLib.Hooks;
using LibraryLib.Localization.Dice;
using LibraryLib.Localization.LibraryDynamicVars;
using LibraryLib.Models;
using LibraryLib.Utils.Resistance;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Commands;

public class LibraryAttackCommand : AttackCommand
{
	private const int _maxAdditionalDiceUses = 32;
	private LibraryDamageType _damageType = LibraryDamageType.None;
	private readonly List<DiceRollResult> _rollResults = new List<DiceRollResult>();
	private readonly List<List<LibraryChaoResult>> _chaoResults = new List<List<LibraryChaoResult>>();

	private Func<Task>? _afterDiceResult;
	public LibraryDice? Dice { get; private set; }
	public List<DiceRollResult> RollResults => _rollResults;
	public IEnumerable<List<LibraryChaoResult>> ChaoResults => _chaoResults;
	public IEnumerable<List<DamageResult>> DamageResults => Results;
	public LibraryAttackCommand FromPetMonster(MonsterModel monster, CardModel card, CardPlay? cardPlay = null)
	{
		AttackCommandAccess.AttackerSetter(this,monster.Creature);
        AttackCommandAccess.ModelSourceSetter(this,card);
        AttackCommandAccess.CardPlaySetter(this,cardPlay);
        AttackCommandAccess.AttackerAnimNameRef(this) = "Attack";
        AttackCommandAccess.AttackerAnimDelayRef(this) = 0.3f;
        AttackCommandAccess.SourceTypeRef(this) = 2;
        return this;
	}
	public LibraryAttackCommand FromCreature(Creature creature)
	{
		if (Attacker != null)
		{
			throw new InvalidOperationException("Attacker has already been set.");
		}
		AttackCommandAccess.AttackerSetter(this,creature);
        AttackCommandAccess.AttackerAnimNameRef(this) = "Attack";
        AttackCommandAccess.AttackerAnimDelayRef(this) = creature.Player?.Character.AttackAnimDelay ?? 0.3f;
        AttackCommandAccess.SourceTypeRef(this) = 2;
        return this;
	}
	public LibraryAttackCommand Unblockable()
	{
		AttackCommandAccess.DamagePropsSetter(this,DamageProps|ValueProp.Unblockable) ;
		return this;
	}

	public LibraryAttackCommand WithAttackerAnim(string? animName, float delay, LibraryCreature? visualAttacker = null)
	{
		if (AttackCommandAccess.AttackerAnimNameRef(this) == null)
		{
			throw new InvalidOperationException("WithAttackerAnim was called before FromCard/FromMonster/FromOsty, should be called after.");
		}
		AttackCommandAccess.AttackerAnimNameRef(this) = animName;
		AttackCommandAccess.AttackerAnimDelayRef(this) = delay;
		AttackCommandAccess.VisualAttackerRef(this) = visualAttacker;
		return this;
	}

	public LibraryAttackCommand AfterDiceResult(Func<Task> afterDiceResult)
	{
		ArgumentNullException.ThrowIfNull(afterDiceResult);
		if (Dice == null)
		{
			throw new InvalidOperationException(
				"AfterDiceResult requires a dice-backed attack.");
		}
		_afterDiceResult = afterDiceResult;
		return this;
	}
	public void AddChaoResultsInternal(IEnumerable<LibraryChaoResult> results)
	{
		_chaoResults.Add(results.ToList());
	}
	public void AddRollResultsInternal(DiceRollResult results){
		_rollResults.Add(results);
	}
	// Registered ILibraryAttackTargetFilter implementations narrow the result; see LibraryAttackTargets.
	private IReadOnlyList<Creature> GetPossibleTargets()
	{
		return LibraryAttackTargets.Filter(this, GetUnfilteredTargets());
	}

	private IReadOnlyList<Creature> GetUnfilteredTargets()
	{
		if (IsSingleTargeted)
		{
			return new List<Creature>{AttackCommandAccess.SingleTargetRef(this)!};
		}
		if (IsMultiTargeted)
		{
			if (AttackCommandAccess.SourceTypeRef(this) == 2)
			{
				return AttackCommandAccess.CombatStateRef(this)!.PlayerCreatures;
			}
			if (Attacker == null)
			{
				throw new InvalidOperationException("We require an attacker to be able to grab its opponents");
			}
			return AttackCommandAccess.CombatStateRef(this)!.GetOpponentsOf(Attacker);
		}
		throw new InvalidOperationException("No targets set, a Targeting method must be called before Execute");
	}
	
	public static CombatSide GetOppositeSide(CombatSide side)
	{
		return side switch
		{
			CombatSide.None => CombatSide.None, 
			CombatSide.Player => CombatSide.Enemy, 
			CombatSide.Enemy => CombatSide.Player, 
			_ => throw new ArgumentOutOfRangeException("side", side, null), 
		};
	}
	public LibraryAttackCommand WithDamageType(LibraryDamageType type)
	{
		_damageType = type;
		return this;
	}
	public LibraryAttackCommand(decimal damagePerHit):base(damagePerHit)
	{
	}	
	public LibraryAttackCommand(LibraryDice dice):base(dice.BaseValue)
	{
		Dice = dice;
		_damageType = dice.DamageType;
	}	
	public LibraryAttackCommand(CalculatedDamageVar calculatedDamageVar):base(calculatedDamageVar)
	{
	}



	public async Task<LibraryAttackCommand> Execute(PlayerChoiceContext? choiceContext,CardPlay? cardPlay = null)
	{ 
		ICombatState? combatState = Attacker?.CombatState;
		if (Attacker == null)
		{
			throw new InvalidOperationException("No attacker set.");
		}
		if (CombatManager.Instance.IsOverOrEnding && (combatState == null || combatState.IsLiveCombat()))
		{
			return this;
		}
		if (combatState == null)
		{
			throw new InvalidOperationException("No combat state even though combat is not over.");
		}
		if (Attacker.IsDead)
		{
			return this;
		}
		if (!IsSingleTargeted && !IsMultiTargeted)
		{
			throw new InvalidOperationException("No targets set.");
		}
		await LibraryHooks.BeforeAttack(combatState, this);
		// Registered ILibraryDamageTypeModifier implementations can change the type; see LibraryDamageTypes.
		LibraryDamageType damageType = LibraryDamageTypes.Modify(
			new LibraryDamageTypeContext(ModelSource as CardModel, Attacker, IsSingleTargeted ? AttackCommandAccess.SingleTargetRef(this) : null, null, this, false),
			_damageType);
		decimal attackCount = LibraryHooks.ModifyAttackHitCount(combatState, this, (Dice?.EnableCustomUseTimes ?? false) ? Dice.UseTimes : AttackCommandAccess.HitCountRef(this));
		int additionalUses = 0;
		for (int i = 0; i < attackCount; i++)
		{
			if (Attacker.IsDead)
			{
				break;
			}
			List<Creature> validTargets = (from c in GetPossibleTargets()
				where c.IsAlive
				select c).ToList();
			if (validTargets.Count == 0 && combatState.IsLiveCombat())
			{
				break;
			}
			Creature? singleTarget;
			if (!IsRandomlyTargeted)
			{
				singleTarget = (validTargets.Count != 1) ? null : validTargets[0];
			}
			else
			{
				if (!AttackCommandAccess.DoesRandomTargetingAllowDuplicatesRef(this))
				{
					validTargets = validTargets.Where((Creature c) => Results.SelectMany((List<DamageResult> r) => r).All((DamageResult r) => r.Receiver != c)).ToList();
					if (validTargets.Count == 0)
					{
						throw new InvalidOperationException("No valid targets for attack with duplicates disallowed. If you're in a test, you probably need to add more enemies. If you're in real gameplay, something is wrong.");
					}
				}
				Rng combatTargets = (Attacker.Player ?? Attacker.PetOwner).RunState.Rng.CombatTargets;
				singleTarget = combatTargets.NextItem(validTargets) ;
			}

			async Task PlayAttackPresentationAsync()
			{
				if (AttackCommandAccess.PlayOnEveryHitRef(this) || i == 0)
				{
					if (AttackCommandAccess.AttackerVfxRef(this) != null)
					{
						VfxCmd.PlayOnCreatureCenter(Attacker, AttackCommandAccess.AttackerVfxRef(this));
					}
					foreach (Func<Node2D> customAttackerVfxNode in AttackCommandAccess.CustomAttackerVfxNodesRef(this))
					{
						Attacker.GetVfxContainer()?.AddChildSafely(customAttackerVfxNode());
					}
					if (AttackCommandAccess.AttackerSfxRef(this) != null)
					{
						SfxCmd.Play(AttackCommandAccess.AttackerSfxRef(this));
					}
					else if (AttackCommandAccess.TmpAttackerSfxRef(this) != null)
					{
						NDebugAudioManager.Instance?.Play(AttackCommandAccess.TmpAttackerSfxRef(this));
					}
					if (AttackCommandAccess.AttackerAnimNameRef(this) != null && AttackCommandAccess.ShouldPlayAnimationRef(this))
					{
						await CreatureCmd.TriggerAnim(AttackCommandAccess.VisualAttackerRef(this) ?? Attacker, AttackCommandAccess.AttackerAnimNameRef(this), AttackCommandAccess.AttackerAnimDelayRef(this));
					}
					if (AttackCommandAccess.AfterAttackerAnimRef(this) != null)
					{
						await AttackCommandAccess.AfterAttackerAnimRef(this)();
					}
				}
				if (HitSfx != null)
				{
					SfxCmd.Play(HitSfx);
				}
				else if (TmpHitSfx != null)
				{
					NDebugAudioManager.Instance?.Play(TmpHitSfx);
				}
				if (AttackCommandAccess.WaitBeforeHitRef(this).Any((float w) => w > 0f))
				{
					await Cmd.CustomScaledWait(AttackCommandAccess.WaitBeforeHitRef(this)[0], AttackCommandAccess.WaitBeforeHitRef(this)[1]);
				}
				foreach (Func<Creature, Node2D> customHitVfxNode in AttackCommandAccess.CustomHitVfxNodesRef(this))
				{
					if (singleTarget != null)
					{
						singleTarget.GetVfxContainer()?.AddChildSafely(customHitVfxNode(singleTarget));
						continue;
					}
					foreach (Creature item in validTargets)
					{
						item.GetVfxContainer()?.AddChildSafely(customHitVfxNode(item));
					}
				}
				if (HitVfx != null)
				{
					if (singleTarget != null)
					{
						if (AttackCommandAccess.SpawnVfxOnCreatureCenterRef(this))
						{
							VfxCmd.PlayOnCreatureCenter(singleTarget, HitVfx);
						}
						else
						{
							VfxCmd.PlayOnCreature(singleTarget, HitVfx);
						}
					}
					else if (AttackCommandAccess.SpawnVfxOnEachCreatureRef(this))
					{
						if (AttackCommandAccess.SpawnVfxOnCreatureCenterRef(this))
						{
							VfxCmd.PlayOnCreatureCenters(validTargets, HitVfx);
						}
						else
						{
							VfxCmd.PlayOnCreatures(validTargets, HitVfx);
						}
					}
					else
					{
						VfxCmd.PlayOnSide(GetOppositeSide(Attacker.Side), HitVfx, combatState);
					}
				}
				if (AttackCommandAccess.BeforeDamageRef(this) != null)
				{
					await AttackCommandAccess.BeforeDamageRef(this)();
				}
			}
			bool deferAttackPresentation =
				Dice != null && _afterDiceResult != null;
			if (!deferAttackPresentation)
			{
				await PlayAttackPresentationAsync();
			}
			IReadOnlyList<Creature> targets = singleTarget != null ? [singleTarget] : validTargets;
			DiceRollResult? rollResult = await LibraryDice.GetResultWithRoll(combatState,choiceContext,Dice,targets.ToList());
			AddRollResultsInternal(rollResult);
			decimal damage = Dice != null ? rollResult.CurrentValue:((AttackCommandAccess.CalculatedDamageVarRef(this) == null) ?AttackCommandAccess.DamagePerHitRef(this) : AttackCommandAccess.CalculatedDamageVarRef(this).Calculate(singleTarget));
			List<int> Blocks = targets.Select(c => c.Block).ToList();
			Func<Task>? beforeApplyingDamage = null;
			if (deferAttackPresentation)
			{
				beforeApplyingDamage = async () =>
				{
					await _afterDiceResult!();
					await PlayAttackPresentationAsync();
				};
			}
			IEnumerable<DamageResult> damageResults = await LibraryCreatureCmd.Damage(damageAmount: damage, choiceContext: choiceContext ?? new BlockingPlayerChoiceContext(), targets: targets, props: DamageProps, dealer: Attacker ,cardSource: ModelSource as CardModel, cardPlay: cardPlay, type : damageType, beforeApplyingDamage: beforeApplyingDamage);
			AddResultsInternal(damageResults);
			List<LibraryChaoResult> chaoResults = [];
			for (int j = 0 ; j < Blocks.Count ; j++){
				decimal chaoDamage = DamageProps.HasFlag(ValueProp.Unblockable)
					? damage
					: Math.Max(0, damage - Blocks[j]);
				IEnumerable<LibraryChaoResult>? results = await LibraryCreatureCmd.ChaoDamage(damageAmount: chaoDamage, choiceContext: choiceContext ?? new BlockingPlayerChoiceContext(), target: targets[j], props: DamageProps, dealer: Attacker, cardSource: ModelSource as CardModel, cardPlay: cardPlay, type: damageType, damageResults: damageResults);
				if(results != null)
					chaoResults.AddRange(results);
			}
			if (chaoResults.Count > 0)
			{
				AddChaoResultsInternal(chaoResults);
			}
			if (Dice != null && cardPlay != null)
			{
				await Dice.TriggerDiceEffect(choiceContext ?? new BlockingPlayerChoiceContext(), cardPlay, rollResult, targets);
			}
			if (Dice != null && LibraryHooks.ShouldReuse(combatState,targets,Dice,rollResult,out ILibraryAbstractModel? trigger1))
			{
				if (additionalUses >= _maxAdditionalDiceUses)
				{
					Log.Warn($"[LibraryOfRuinaLib.Dice] Reuse limit reached for {Dice.Name}.");
				}
				else
				{
					additionalUses++;
					attackCount++;
					if(trigger1 != null)
						await trigger1.AfterReusing(choiceContext ?? new BlockingPlayerChoiceContext(),targets,Dice,rollResult);
				}
			}
		}
		CombatManager.Instance.History.CreatureAttacked(combatState, Attacker, Results.SelectMany((List<DamageResult> r) => r).ToList());
		await LibraryHooks.AfterAttack(combatState, choiceContext ?? new BlockingPlayerChoiceContext(), this);
		return this;
	}
	private static class AttackCommandAccess
	{
		public static readonly AccessTools.FieldRef<AttackCommand, decimal> DamagePerHitRef =
			AccessTools.FieldRefAccess<AttackCommand, decimal>("_damagePerHit");

		public static readonly AccessTools.FieldRef<AttackCommand, CalculatedDamageVar?> CalculatedDamageVarRef =
			AccessTools.FieldRefAccess<AttackCommand, CalculatedDamageVar?>("_calculatedDamageVar");


		public static readonly AccessTools.FieldRef<AttackCommand, float[]> WaitBeforeHitRef =
			AccessTools.FieldRefAccess<AttackCommand, float[]>("_waitBeforeHit");

		public static readonly AccessTools.FieldRef<AttackCommand, List<Func<Node2D?>>> CustomAttackerVfxNodesRef =
			AccessTools.FieldRefAccess<AttackCommand, List<Func<Node2D?>>>("_customAttackerVfxNodes");

		public static readonly AccessTools.FieldRef<AttackCommand, List<Func<Creature, Node2D?>>> CustomHitVfxNodesRef =
			AccessTools.FieldRefAccess<AttackCommand, List<Func<Creature, Node2D?>>>("_customHitVfxNodes");


		public static readonly AccessTools.FieldRef<AttackCommand, int> HitCountRef =
			AccessTools.FieldRefAccess<AttackCommand, int>("_hitCount");

		public static readonly AccessTools.FieldRef<AttackCommand, ICombatState?> CombatStateRef =
			AccessTools.FieldRefAccess<AttackCommand, ICombatState?>("_combatState");

		public static readonly AccessTools.FieldRef<AttackCommand, Creature?> SingleTargetRef =
			AccessTools.FieldRefAccess<AttackCommand, Creature?>("_singleTarget");

		public static readonly AccessTools.FieldRef<AttackCommand, bool> SpawnVfxOnEachCreatureRef =
			AccessTools.FieldRefAccess<AttackCommand, bool>("_spawnVfxOnEachCreature");

		public static readonly AccessTools.FieldRef<AttackCommand, bool> SpawnVfxOnCreatureCenterRef =
			AccessTools.FieldRefAccess<AttackCommand, bool>("_spawnVfxOnCreatureCenter");

		public static readonly AccessTools.FieldRef<AttackCommand, bool> DoesRandomTargetingAllowDuplicatesRef =
			AccessTools.FieldRefAccess<AttackCommand, bool>("_doesRandomTargetingAllowDuplicates");

		public static readonly AccessTools.FieldRef<AttackCommand, bool> ShouldPlayAnimationRef =
			AccessTools.FieldRefAccess<AttackCommand, bool>("_shouldPlayAnimation");

		public static readonly AccessTools.FieldRef<AttackCommand, string?> AttackerAnimNameRef =
			AccessTools.FieldRefAccess<AttackCommand, string?>("_attackerAnimName");

		public static readonly AccessTools.FieldRef<AttackCommand, float> AttackerAnimDelayRef =
			AccessTools.FieldRefAccess<AttackCommand, float>("_attackerAnimDelay");

		public static readonly AccessTools.FieldRef<AttackCommand, Creature?> VisualAttackerRef =
			AccessTools.FieldRefAccess<AttackCommand, Creature?>("_visualAttacker");

		public static readonly AccessTools.FieldRef<AttackCommand, bool> PlayOnEveryHitRef =
			AccessTools.FieldRefAccess<AttackCommand, bool>("_playOnEveryHit");

		public static readonly AccessTools.FieldRef<AttackCommand, string?> AttackerVfxRef =
			AccessTools.FieldRefAccess<AttackCommand, string?>("_attackerVfx");

		public static readonly AccessTools.FieldRef<AttackCommand, string?> AttackerSfxRef =
			AccessTools.FieldRefAccess<AttackCommand, string?>("_attackerSfx");

		public static readonly AccessTools.FieldRef<AttackCommand, string?> TmpAttackerSfxRef =
			AccessTools.FieldRefAccess<AttackCommand, string?>("_tmpAttackerSfx");

		public static readonly AccessTools.FieldRef<AttackCommand, Func<Task>?> AfterAttackerAnimRef =
			AccessTools.FieldRefAccess<AttackCommand, Func<Task>?>("_afterAttackerAnim");

		public static readonly AccessTools.FieldRef<AttackCommand, Func<Task>?> BeforeDamageRef =
			AccessTools.FieldRefAccess<AttackCommand, Func<Task>?>("_beforeDamage");

		public static readonly Action<AttackCommand, Creature?> AttackerSetter =
			AccessTools.MethodDelegate<Action<AttackCommand, Creature?>>(
				AccessTools.PropertySetter(typeof(AttackCommand), "Attacker"));

		public static readonly Action<AttackCommand, AbstractModel?> ModelSourceSetter =
			AccessTools.MethodDelegate<Action<AttackCommand, AbstractModel?>>(
				AccessTools.PropertySetter(typeof(AttackCommand), "ModelSource"));
		public static readonly Action<AttackCommand, CardPlay?> CardPlaySetter =
			AccessTools.MethodDelegate<Action<AttackCommand, CardPlay?>>(
				AccessTools.PropertySetter(typeof(AttackCommand), "CardPlay"));

		public static readonly Action<AttackCommand, CombatSide> TargetSideSetter =
			AccessTools.MethodDelegate<Action<AttackCommand, CombatSide>>(
				AccessTools.PropertySetter(typeof(AttackCommand), "TargetSide"));

		public static readonly Action<AttackCommand, ValueProp> DamagePropsSetter =
			AccessTools.MethodDelegate<Action<AttackCommand, ValueProp>>(
				AccessTools.PropertySetter(typeof(AttackCommand), "DamageProps"));
		public static readonly AccessTools.FieldRef<AttackCommand, int> SourceTypeRef =
			AccessTools.FieldRefAccess<AttackCommand, int>("_sourceType");
	}
	public new LibraryAttackCommand FromCard(CardModel card, CardPlay? cardPlay = null)
	{
		return (LibraryAttackCommand)base.FromCard(card,cardPlay);
	}

	public new LibraryAttackCommand FromOsty(Creature osty, CardModel card, CardPlay? cardPlay = null)
	{
		return (LibraryAttackCommand)base.FromOsty(osty,card,cardPlay);
	}

	public new LibraryAttackCommand FromMonster(MonsterModel monster)
	{
		return (LibraryAttackCommand)base.FromMonster(monster);
	}

	public new LibraryAttackCommand Targeting(Creature target )
	{
		return (LibraryAttackCommand)base.Targeting(target);
	}

	public new LibraryAttackCommand TargetingAllOpponents(ICombatState combatState)
	{	
		return (LibraryAttackCommand)base.TargetingAllOpponents(combatState);
	}

	public new LibraryAttackCommand TargetingRandomOpponents(ICombatState combatState, bool allowDuplicates = true)
	{	
		return (LibraryAttackCommand)base.TargetingRandomOpponents(combatState,allowDuplicates);
	}

	public new LibraryAttackCommand Unpowered()
	{
		return (LibraryAttackCommand)base.Unpowered();
	}
	public new LibraryAttackCommand WithNoAttackerAnim()
	{
		return (LibraryAttackCommand)base.WithNoAttackerAnim();
	}

	public new LibraryAttackCommand AfterAttackerAnim(Func<Task> afterAttackerAnim)
	{
		return (LibraryAttackCommand)base.AfterAttackerAnim(afterAttackerAnim);
	}

	public new LibraryAttackCommand WithAttackerFx(string? vfx = null, string? sfx = null, string? tmpSfx = null)
	{
		return (LibraryAttackCommand)base.WithAttackerFx(vfx,sfx,tmpSfx);
	}

	public new LibraryAttackCommand WithAttackerFx(Func<Node2D?> createAttackerVfx)
	{
		return (LibraryAttackCommand)base.WithAttackerFx(createAttackerVfx);
	}

	public new LibraryAttackCommand WithWaitBeforeHit(float fastSeconds, float standardSeconds)
	{
		return (LibraryAttackCommand)base.WithWaitBeforeHit(fastSeconds,standardSeconds);
	}

	public new LibraryAttackCommand WithHitFx(string? vfx = null, string? sfx = null, string? tmpSfx = null)
	{
		return (LibraryAttackCommand)base.WithHitFx(vfx,sfx,tmpSfx);
	}

	public new LibraryAttackCommand SpawningHitVfxOnEachCreature()
	{
		return (LibraryAttackCommand)base.SpawningHitVfxOnEachCreature();
	}

	public new LibraryAttackCommand WithHitVfxSpawnedAtBase()
	{
		return (LibraryAttackCommand)base.WithHitVfxSpawnedAtBase();
	}

	public new LibraryAttackCommand WithHitVfxNode(Func<Creature, Node2D?> createHitVfxNode)
	{
		return (LibraryAttackCommand)base.WithHitVfxNode(createHitVfxNode);
	}

	public new LibraryAttackCommand OnlyPlayAnimOnce()
	{
		return (LibraryAttackCommand)base.OnlyPlayAnimOnce();
	}

	public new LibraryAttackCommand WithHitCount(int hitCount)
	{
		return (LibraryAttackCommand)base.WithHitCount(hitCount);
	}

	public new LibraryAttackCommand BeforeDamage(Func<Task> beforeDamage)
	{
		return (LibraryAttackCommand)base.BeforeDamage(beforeDamage);
	}

	public new static async Task<AttackContext> CreateContextAsync(ICombatState combatState, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{	
		return await LibraryLib.Compat.GameApi.CreateContext(combatState, choiceContext, cardPlay);
	}


	public new void IncrementHitsInternal()
	{
		base.IncrementHitsInternal();
	}
}
