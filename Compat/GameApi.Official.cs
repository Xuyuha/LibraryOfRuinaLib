#if STS2_0_111_0
#nullable enable
using System.Reflection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
namespace LibraryLib.Compat;
internal static partial class GameApi
{
    internal static AttackCommand FromCard(AttackCommand command, CardModel card, CardPlay? play) => command.FromCard(card, play);
    internal static AttackCommand FromOsty(AttackCommand command, Creature osty, CardModel card, CardPlay? play) => command.FromOsty(osty, card, play);
    internal static Task<AttackContext> CreateContext(ICombatState combat, PlayerChoiceContext context, CardPlay play) => AttackContext.CreateAsync(combat, context, play);
    internal static Task<IEnumerable<DamageResult>> Damage(PlayerChoiceContext context, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? card, CardPlay? play) => CreatureCmd.Damage(context, target, amount, props, dealer, card, play);
    internal static Task AfterBlockBroken(AbstractModel model, PlayerChoiceContext context, Creature target, Creature? breaker) => model.AfterBlockBroken(context, target, breaker);
    internal static decimal ModifyDamageAdditive(AbstractModel model, Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? card, CardPlay? play) => model.ModifyDamageAdditive(target, amount, props, dealer, card, play);
    internal static decimal ModifyDamageMultiplicative(AbstractModel model, Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? card, CardPlay? play) => model.ModifyDamageMultiplicative(target, amount, props, dealer, card, play);
    internal static decimal ModifyDamageCap(AbstractModel model, Creature? target, ValueProp props, Creature? dealer, CardModel? card, CardPlay? play) => model.ModifyDamageCap(target, props, dealer, card, play);
    internal static IEnumerable<Assembly> Assemblies(Mod mod) => mod.assemblies;
    internal static decimal ModifyDamage(IRunState run, ICombatState? combat, Creature? target, Creature? dealer, decimal amount, ValueProp props, CardModel? card, CardPlay? play, ModifyDamageHookType hook, CardPreviewMode preview, out IEnumerable<AbstractModel> modifiers) => Hook.ModifyDamage(run, combat, target, dealer, amount, props, card, play, hook, preview, out modifiers);
}
#endif
