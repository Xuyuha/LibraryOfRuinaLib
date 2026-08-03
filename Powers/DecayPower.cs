using LibraryLib.Commands;
using LibraryLib.Models;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace LibraryLib.Powers;
public sealed class LibraryDecayPower : LibraryPowerModel//腐蚀，效果为被击中时将追加承受等同于“腐蚀”层数的伤害与混乱伤害，每一幕结束时受到层数的伤害
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if(target != Owner)return;
        if(!props.IsPoweredAttack())return;
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(),Owner,Amount,ValueProp.Unpowered,Owner);
        await LibraryCreatureCmd.ChaoDamage(new ThrowingPlayerChoiceContext(),Owner,Amount,ValueProp.Unpowered,Owner,null);        
    }
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(),Owner,Amount,ValueProp.Unpowered,Owner);
        await PowerCmd.Decrement(this);
    }
}
