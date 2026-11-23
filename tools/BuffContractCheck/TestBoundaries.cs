// Offline test boundaries, not game implementations. No Godot, saves, networking or patches are run.
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.ValueProps
{
    [Flags] public enum ValueProp { Unpowered = 0, Move = 1, Powered = 2 }
    public static class Props { public static bool IsPoweredAttack(this ValueProp p) => p.HasFlag(ValueProp.Move | ValueProp.Powered); }
}
namespace MegaCrit.Sts2.Core.Entities.Creatures
{
    public class Creature
    {
        public CombatSide Side { get; set; }
        public bool IsPlayer { get; set; }
        public bool IsDead { get; set; }
        public Player? PetOwner { get; set; }
        public ICombatState? CombatState { get; set; }
    }
    public class Player { public required Creature Creature { get; init; } }
    public record DamageResult(int TotalDamage);
}
namespace MegaCrit.Sts2.Core.Combat
{
    public enum CombatSide { Player, Enemy }
    public interface ICombatState { CombatSide CurrentSide { get; } List<AbstractModel> Listeners { get; } }
    public class CombatState : ICombatState
    {
        public CombatSide CurrentSide { get; set; }
        public List<AbstractModel> Listeners { get; } = [];
    }
    public class CombatManager
    {
        public static CombatManager Instance { get; } = new();
        public List<Player> PlayersTakingExtraTurn { get; } = [];
    }
}
namespace MegaCrit.Sts2.Core.Models
{
    public class AbstractModel { }
    public class CardModel : AbstractModel { }
}
namespace MegaCrit.Sts2.Core.Entities.Cards
{
    public class CardPlay { }
    public enum CardPreviewMode { None, Normal }
}
namespace MegaCrit.Sts2.Core.Entities.Powers
{
    public enum PowerType { None, Buff, Debuff }
    public enum PowerStackType { Counter, Single }
}
namespace MegaCrit.Sts2.Core.GameActions.Multiplayer { public class PlayerChoiceContext { } }
namespace MegaCrit.Sts2.Core.Hooks
{
    public static class Hook { public static IEnumerable<AbstractModel> IterateHookListeners(this ICombatState combat) => combat.Listeners; }
}
namespace LibraryLib.Utils.Resistance { public enum LibraryDamageType { None, Slash, Pierce, Blunt } }
namespace LibraryLib.Models
{
    using MegaCrit.Sts2.Core.Entities.Powers;
    using MegaCrit.Sts2.Core.GameActions.Multiplayer;
    public abstract class DynamicPowerModel : AbstractModel
    {
        public required Creature Owner { get; set; }
        public Creature? Applier { get; set; }
        public int Amount { get; set; }
        public bool Removed { get; set; }
        public abstract PowerType Type { get; }
        public abstract PowerStackType StackType { get; }
        public void Flash() { }
        public virtual Task AfterApplied(Creature? applier, CardModel? card) => Task.CompletedTask;
        public virtual Task AfterDamageReceived(PlayerChoiceContext c, Creature t, DamageResult r, ValueProp p, Creature? d, CardModel? card) => Task.CompletedTask;
        public virtual Task AfterSideTurnEnd(PlayerChoiceContext c, CombatSide s, IEnumerable<Creature> p) => Task.CompletedTask;
        public virtual Task AfterSideTurnEndLate(PlayerChoiceContext c, CombatSide s, IEnumerable<Creature> p) => Task.CompletedTask;
    }
}
namespace MegaCrit.Sts2.Core.Commands
{
    using LibraryLib.Models;
    public static class PowerCmd
    {
        public static Task Decrement(DynamicPowerModel power) { power.Amount--; power.Removed = power.Amount <= 0; return Task.CompletedTask; }
        public static Task Remove(DynamicPowerModel power) { power.Removed = true; return Task.CompletedTask; }
    }
}
namespace LibraryLib.Compat
{
    using MegaCrit.Sts2.Core.Entities.Cards;
    using MegaCrit.Sts2.Core.GameActions.Multiplayer;
    public static class GameApi
    {
        public record Hit(Creature Target, decimal Amount, ValueProp Props, Creature? Dealer, CardModel? Card);
        public static List<Hit> Hits { get; } = [];
        public static Func<Hit, Task>? FollowUp { get; set; }
        public static async Task Damage(PlayerChoiceContext c, Creature t, decimal a, ValueProp p, Creature? d, CardModel? card, CardPlay? play)
        {
            var hit = new Hit(t, a, p, d, card);
            Hits.Add(hit);
            if (Hits.Count > 10) throw new Exception("Recursive corrosion damage");
            if (FollowUp != null) await FollowUp(hit);
        }
    }
}
namespace LibraryLib.Combat.HealthBars
{
    public readonly record struct LibraryHealthBarForecastContext(Creature Creature)
    { public ICombatState? CombatState => Creature.CombatState; }
    public record LibraryHealthBarDamageForecast(decimal Damage, string Color, ValueProp Props, int Order = 0, Creature? Dealer = null);
    public static class LibraryHealthBarForecastColors { public const string Corrosion = "corrosion"; }
    public interface ILibraryHealthBarDamageForecastSource
    { IEnumerable<LibraryHealthBarDamageForecast> GetLibraryHealthBarDamageForecasts(LibraryHealthBarForecastContext c); }
}
