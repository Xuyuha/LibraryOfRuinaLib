using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Combat;
using Library.Models;

namespace Library.Powers;
public sealed class LibraryStaggerPower : LibraryPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    private bool Stunned { get; set; } = false;

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (Amount * 2 >= Owner.MaxHp && target == Owner)
        {
            Flash();
            if (Owner.IsEnemy)
                await CreatureCmd.Stun(Owner);
            if (Owner.IsPlayer)
                Stunned = true;
            await PowerCmd.Remove(this);
        }
    }
    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, ICombatState combatState)
    {
        if (side == CombatSide.Player && Stunned && Owner.IsPlayer)
        {
            Stunned = false;
            await PlayerCmd.LoseEnergy(Owner.Player!.MaxEnergy, Owner.Player);
        }
    }

}