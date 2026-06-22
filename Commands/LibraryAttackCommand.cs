#nullable enable
using Godot;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Library.Utils;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Monsters;
using Library.Entities.Creatures;
using Library.Hooks;
using MegaCrit.Sts2.Core.Entities.Cards;
using Library.Models;
using MegaCrit.Sts2.Core.Logging;

public class LibraryAttackCommand//重置了原版的AttackCommand,经我研究，AttackCommand几乎没有与任何其他类交互，可以放心重置
//todo：骰子相关的方法
{
	private enum SourceType
	{
		None,
		Card,
		Monster
	}
    private LibraryDamageType _damageType = LibraryDamageType.None;
	private decimal _damagePerHit;
	private readonly CalculatedDamageVar? _calculatedDamageVar;

	private int _hitCount = 1;

	private SourceType _sourceType;

	private ICombatState? _combatState;

	private LibraryCreature? _singleTarget;

	private bool _spawnVfxOnEachCreature;

	private bool _spawnVfxOnCreatureCenter = true;

	private bool _doesRandomTargetingAllowDuplicates = true;

	private bool _shouldPlayAnimation = true;

	private readonly List<List<DamageResult>> _damageResults = new List<List<DamageResult>>();
	private readonly List<List<LibraryChaoResult>> _chaoResults = new List<List<LibraryChaoResult>>();

	private string? _attackerAnimName;

	private float _attackerAnimDelay;

	private LibraryCreature? _visualAttacker;

	private bool _playOnEveryHit = true;

	private string? _attackerVfx;

	private string? _attackerSfx;

	private string? _tmpAttackerSfx;

	private readonly float[] _waitBeforeHit = new float[2] { -1f, -1f };

	private readonly List<Func<Node2D?>> _customAttackerVfxNodes = new List<Func<Node2D?>>();

	private readonly List<Func<Creature, Node2D?>> _customHitVfxNodes = new List<Func<Creature, Node2D?>>();

	private Func<Task>? _afterAttackerAnim;

	private Func<Task>? _beforeDamage;
	public LibraryDice? Dice { get; private set; }

	public Creature? Attacker { get; private set; }

	public AbstractModel? ModelSource { get; private set; }

	public CombatSide TargetSide { get; private set; }

	public ValueProp DamageProps { get; private set; } = ValueProp.Move;

	public bool IsSingleTargeted => _singleTarget != null;

	public bool IsMultiTargeted => _combatState != null;

	public bool IsRandomlyTargeted { get; private set; }

	public IEnumerable<List<DamageResult>> DamageResults => _damageResults;

	public IEnumerable<List<LibraryChaoResult>> ChaoResults => _chaoResults;

	public string? HitSfx { get; private set; }

	public string? TmpHitSfx { get; private set; }

	public string? HitVfx { get; private set; }
	public AttackCommand ToAttackCommand { get; private set; }
	public LibraryAttackCommand FromCard(CardModel card)
	{
		ToAttackCommand = ToAttackCommand.FromCard(card);
		if (Attacker != null)
		{
			throw new InvalidOperationException("Attacker has already been set.");
		}
		if (ModelSource != null)
		{
			throw new InvalidOperationException("ModelSource has already been set.");
		}
		Player owner = card.Owner;
		Attacker = owner.Creature;
		_attackerAnimName = "Attack";
		_attackerAnimDelay = owner.Character.AttackAnimDelay;
		ModelSource = card;
		_sourceType = SourceType.Card;
		return this;
	}

	public LibraryAttackCommand FromOsty(Creature osty, CardModel card)
	{
		ToAttackCommand = ToAttackCommand.FromOsty(osty, card);
		if (!(osty.Monster is Osty))
		{
			throw new ArgumentException("Creature is not Osty");
		}
		Attacker = osty as LibraryCreature;
		ModelSource = card;
		_attackerAnimName = "Attack";
		_attackerAnimDelay = 0.3f;
		_sourceType = SourceType.Card;
		return WithAttackerFx(null, "event:/sfx/characters/osty/osty_attack");
	}

	public LibraryAttackCommand FromMonster(MonsterModel monster)
	{
		ToAttackCommand = ToAttackCommand.FromMonster(monster);
		if (Attacker != null)
		{
			throw new InvalidOperationException("Attacker has already been set.");
		}
		Attacker = monster.Creature as LibraryCreature;
		_attackerAnimName = "Attack";
		_sourceType = SourceType.Monster;
		return TargetingAllOpponents(monster.Creature.CombatState);
	}

	public LibraryAttackCommand Targeting(Creature target )
	{
		ToAttackCommand = ToAttackCommand.Targeting(target);	
		if (_singleTarget != null)
		{
			throw new InvalidOperationException("Targets already set.");
		}
		if (_combatState != null)
		{
			throw new InvalidOperationException("Already set to target opponents of attacker");
		}
		_singleTarget = target as LibraryCreature;
		TargetSide = target.Side;
		return this;
	}

	public LibraryAttackCommand TargetingAllOpponents(ICombatState combatState)
	{	
		ToAttackCommand = ToAttackCommand.TargetingAllOpponents(combatState);
		if (_singleTarget != null)
		{
			throw new InvalidOperationException("Targets already set.");
		}
		if (_combatState != null)
		{
			throw new InvalidOperationException("Already set to target opponents of attacker");
		}
		if (Attacker == null)
		{
			throw new InvalidOperationException("We require an attacker to be able to grab its opponents");
		}
		_combatState = combatState;
		TargetSide = (Attacker.Side == CombatSide.Enemy) ? CombatSide.Player : CombatSide.Enemy;
		return this;
	}

	public LibraryAttackCommand TargetingRandomOpponents(ICombatState combatState, bool allowDuplicates = true)
	{	
		ToAttackCommand = ToAttackCommand.TargetingRandomOpponents(combatState, allowDuplicates);
		if (_singleTarget != null)
		{
			throw new InvalidOperationException("Targets already set.");
		}
		if (_combatState != null)
		{
			throw new InvalidOperationException("Already set to target opponents of attacker");
		}
		if (Attacker == null)
		{
			throw new InvalidOperationException("We require an attacker to be able to grab its opponents");
		}
		_combatState = combatState;
		IsRandomlyTargeted = true;
		_doesRandomTargetingAllowDuplicates = allowDuplicates;
		return this;
	}

	public LibraryAttackCommand Unpowered()
	{
		ToAttackCommand = ToAttackCommand.Unpowered();
		DamageProps |= ValueProp.Unpowered;
		return this;
	}

	public LibraryAttackCommand WithAttackerAnim(string? animName, float delay, LibraryCreature? visualAttacker = null)
	{
		ToAttackCommand = ToAttackCommand.WithAttackerAnim(animName, delay, visualAttacker);
		if (_attackerAnimName == null)
		{
			throw new InvalidOperationException("WithAttackerAnim was called before FromCard/FromMonster/FromOsty, should be called after.");
		}
		_attackerAnimName = animName;
		_attackerAnimDelay = delay;
		_visualAttacker = visualAttacker;
		return this;
	}

	public LibraryAttackCommand WithNoAttackerAnim()
	{
		ToAttackCommand = ToAttackCommand.WithNoAttackerAnim();
		_shouldPlayAnimation = false;
		return this;
	}

	public LibraryAttackCommand AfterAttackerAnim(Func<Task> afterAttackerAnim)
	{
		ToAttackCommand = ToAttackCommand.AfterAttackerAnim(afterAttackerAnim);
		_afterAttackerAnim = afterAttackerAnim;
		return this;
	}

	public LibraryAttackCommand WithAttackerFx(string? vfx = null, string? sfx = null, string? tmpSfx = null)
	{
		ToAttackCommand = ToAttackCommand.WithAttackerFx(vfx, sfx, tmpSfx);
		_attackerVfx = vfx;
		_attackerSfx = sfx;
		_tmpAttackerSfx = tmpSfx;
		return this;
	}

	public LibraryAttackCommand WithAttackerFx(Func<Node2D?> createAttackerVfx)
	{
		ToAttackCommand = ToAttackCommand.WithAttackerFx(createAttackerVfx);
		_customAttackerVfxNodes.Add(createAttackerVfx);
		return this;
	}

	public LibraryAttackCommand WithWaitBeforeHit(float fastSeconds, float standardSeconds)
	{
		ToAttackCommand = ToAttackCommand.WithWaitBeforeHit(fastSeconds, standardSeconds);
		_waitBeforeHit[0] = fastSeconds;
		_waitBeforeHit[1] = standardSeconds;
		return this;
	}

	public LibraryAttackCommand WithHitFx(string? vfx = null, string? sfx = null, string? tmpSfx = null)
	{
		ToAttackCommand = ToAttackCommand.WithHitFx(vfx, sfx, tmpSfx);
		HitVfx = vfx;
		HitSfx = sfx;
		TmpHitSfx = tmpSfx;
		return this;
	}

	public LibraryAttackCommand SpawningHitVfxOnEachCreature()
	{
		ToAttackCommand = ToAttackCommand.SpawningHitVfxOnEachCreature();
		_spawnVfxOnEachCreature = true;
		return this;
	}

	public LibraryAttackCommand WithHitVfxSpawnedAtBase()
	{
		ToAttackCommand = ToAttackCommand.WithHitVfxSpawnedAtBase();
		_spawnVfxOnCreatureCenter = false;
		return this;
	}

	public LibraryAttackCommand WithHitVfxNode(Func<Creature, Node2D?> createHitVfxNode)
	{
		ToAttackCommand = ToAttackCommand.WithHitVfxNode(createHitVfxNode);
		_customHitVfxNodes.Add(createHitVfxNode);
		return this;
	}

	public LibraryAttackCommand OnlyPlayAnimOnce()
	{
		ToAttackCommand = ToAttackCommand.OnlyPlayAnimOnce();
		_playOnEveryHit = false;
		return this;
	}

	public LibraryAttackCommand WithHitCount(int hitCount)
	{
		ToAttackCommand = ToAttackCommand.WithHitCount(hitCount);
		_hitCount = hitCount;
		return this;
	}

	public LibraryAttackCommand BeforeDamage(Func<Task> beforeDamage)
	{
		ToAttackCommand = ToAttackCommand.BeforeDamage(beforeDamage);
		_beforeDamage = beforeDamage;
		return this;
	}

	public static async Task<AttackContext> CreateContextAsync(ICombatState combatState, PlayerChoiceContext choiceContext, CardModel cardSource)
	{	
		return await AttackContext.CreateAsync(combatState, choiceContext, cardSource);
	}


	public void IncrementHitsInternal()
	{
		ToAttackCommand.IncrementHitsInternal();
		_hitCount++;
	}

	public void AddDamageResultsInternal(IEnumerable<DamageResult> results)
	{
		ToAttackCommand.AddResultsInternal(results);
		_damageResults.Add(results.ToList());
	}
	public void AddChaoResultsInternal(IEnumerable<LibraryChaoResult> results)
	{
		_chaoResults.Add(results.ToList());
	}
	private IReadOnlyList<Creature> GetPossibleTargets()
	{
		if (IsSingleTargeted)
		{
			return new List<Creature>{_singleTarget!};
		}
		if (IsMultiTargeted)
		{
			if (_sourceType == SourceType.Monster)
			{
				return _combatState!.PlayerCreatures;
			}
			if (Attacker == null)
			{
				throw new InvalidOperationException("We require an attacker to be able to grab its opponents");
			}
			return _combatState!.GetOpponentsOf(Attacker);
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

	public LibraryAttackCommand(decimal damagePerHit)
	{
		Dice = null;
		ToAttackCommand = new AttackCommand(damagePerHit);
		_damagePerHit = damagePerHit;
		_calculatedDamageVar = null;
	}	
	public LibraryAttackCommand(LibraryDice dice)
	{
		Dice = dice;
		ToAttackCommand = new AttackCommand(dice.BaseValue);
		_damageType = dice.DamageType;
		_damagePerHit = 0;
		_calculatedDamageVar = null;
	}	
	public LibraryAttackCommand(CalculatedDamageVar calculatedDamageVar)
	{
		Dice = null;
		ToAttackCommand = new AttackCommand(calculatedDamageVar);
		_damagePerHit = -1m;
		_calculatedDamageVar = calculatedDamageVar;
	}



	public async Task<LibraryAttackCommand> Execute(PlayerChoiceContext? choiceContext,CardPlay cardPlay = null)
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
		if(Dice != null)
			Dice.HasUseTimes = 0;
		decimal attackCount = LibraryHooks.ModifyAttackHitCount(combatState, this, (Dice?.EnableCustomUseTimes ?? false) ? Dice.UseTimes : _hitCount);
		for (int i = 0; i < attackCount; i++)
		{
			if (Attacker.IsDead)
			{
				break;
			}
			List<LibraryCreature> validTargets = (from c in GetPossibleTargets()
				where c.IsAlive
				select c as LibraryCreature).ToList();
			if (validTargets.Count == 0 && combatState.IsLiveCombat())
			{
				break;
			}
			if (_playOnEveryHit || i == 0)
			{
				if (_attackerVfx != null)
				{
					VfxCmd.PlayOnCreatureCenter(Attacker, _attackerVfx);
				}
				foreach (Func<Node2D> customAttackerVfxNode in _customAttackerVfxNodes)
				{
					Attacker.GetVfxContainer()?.AddChildSafely(customAttackerVfxNode());
				}
				if (_attackerSfx != null)
				{
					SfxCmd.Play(_attackerSfx);
				}
				else if (_tmpAttackerSfx != null)
				{
					NDebugAudioManager.Instance?.Play(_tmpAttackerSfx);
				}
				if (_attackerAnimName != null && _shouldPlayAnimation)
				{
					await CreatureCmd.TriggerAnim(_visualAttacker ?? Attacker, _attackerAnimName, _attackerAnimDelay);
				}
				if (_afterAttackerAnim != null)
				{
					await _afterAttackerAnim();
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
			LibraryCreature? singleTarget;
			if (!IsRandomlyTargeted)
			{
				singleTarget = (validTargets.Count != 1) ? null : validTargets[0];
			}
			else
			{
				if (!_doesRandomTargetingAllowDuplicates)
				{
					validTargets = validTargets.Where((LibraryCreature c) => _damageResults.SelectMany((List<DamageResult> r) => r).All((DamageResult r) => r.Receiver != c)).ToList();
					if (validTargets.Count == 0)
					{
						throw new InvalidOperationException("No valid targets for attack with duplicates disallowed. If you're in a test, you probably need to add more enemies. If you're in real gameplay, something is wrong.");
					}
				}
				Rng combatTargets = (Attacker.Player ?? Attacker.PetOwner).RunState.Rng.CombatTargets;
				singleTarget = combatTargets.NextItem(validTargets) ;
			}
			if (_waitBeforeHit.Any((float w) => w > 0f))
			{
				await Cmd.CustomScaledWait(_waitBeforeHit[0], _waitBeforeHit[1]);
			}
			foreach (Func<Creature, Node2D> customHitVfxNode in _customHitVfxNodes)
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
					if (_spawnVfxOnCreatureCenter)
					{
						VfxCmd.PlayOnCreatureCenter(singleTarget, HitVfx);
					}
					else
					{
						VfxCmd.PlayOnCreature(singleTarget, HitVfx);
					}
				}
				else if (_spawnVfxOnEachCreature)
				{
					if (_spawnVfxOnCreatureCenter)
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
			if (_beforeDamage != null)
			{
				await _beforeDamage();
			}
			if(Dice != null)
				Dice.HasUseTimes++;
			IEnumerable<LibraryCreature> targets = _singleTarget != null ? [_singleTarget] : validTargets;
			int RollCount = 1;
			for(int j = 0 ; j < RollCount ; j++)
			{
				await LibraryHooks.BeforeDiceRoll(combatState, choiceContext ?? new BlockingPlayerChoiceContext(),_singleTarget != null ? [_singleTarget] : validTargets, Dice);
				Dice?.Roll(Attacker.Player);
				await LibraryHooks.AfterDiceRoll(combatState, choiceContext ?? new BlockingPlayerChoiceContext(),_singleTarget != null ? [_singleTarget] : validTargets, Dice);
				if(Dice != null && LibraryHooks.ShouldReroll(combatState,targets,Dice,out ILibraryAbstractModel? trigger))
				{
					j++;
					if(trigger != null)
						await trigger.AfterRerolling(choiceContext ?? new BlockingPlayerChoiceContext(),targets,Dice);
				}
			}
			decimal damage = Dice != null ? Dice.CurrentBaseValue:((_calculatedDamageVar == null) ?_damagePerHit : _calculatedDamageVar.Calculate(singleTarget));
			List<int> Blocks = (_singleTarget != null ? [_singleTarget] :validTargets).Select(c => c.Block).ToList();
			IEnumerable<DamageResult> damageResults = await LibraryCreatureCmd.Damage(damageAmount: damage, choiceContext: choiceContext ?? new BlockingPlayerChoiceContext(), targets: (singleTarget != null) ? [singleTarget]  : ((IEnumerable<LibraryCreature>)validTargets), props: DamageProps, dealer: Attacker ,cardSource: ModelSource as CardModel,type : _damageType);
			AddDamageResultsInternal(damageResults);
			List<LibraryChaoResult> chaoResults = [];
			for (int j = 0 ; j < Blocks.Count ; j++){
                IEnumerable<LibraryChaoResult>? results = await LibraryCreatureCmd.ChaoDamage(damageAmount: damage - Blocks[j], choiceContext: choiceContext ?? new BlockingPlayerChoiceContext(), target: singleTarget ?? validTargets.ElementAt(j), props: DamageProps, dealer: Attacker, cardSource: ModelSource as CardModel, type: _damageType, damageResults: damageResults);
				if(results != null)
					chaoResults.AddRange(results);
			}
			if (chaoResults.Count > 0)
			{
				AddChaoResultsInternal(chaoResults);
			}
			Dice?.TriggerDiceEffect(choiceContext ?? new BlockingPlayerChoiceContext(),cardPlay);
			if (Dice != null && LibraryHooks.ShouldReuse(combatState,targets,Dice,out ILibraryAbstractModel? trigger1))
			{
				attackCount++;
				if(trigger1 != null)
					await trigger1.AfterReusing(choiceContext ?? new BlockingPlayerChoiceContext(),targets,Dice);
			}
		}
		CombatManager.Instance.History.CreatureAttacked(combatState, Attacker, _damageResults.SelectMany((List<DamageResult> r) => r).ToList());
		await LibraryHooks.AfterAttack(combatState, choiceContext ?? new BlockingPlayerChoiceContext(), this);
		return this;
	}
}
