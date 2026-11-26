// Offline boundaries only: no Godot, real combat, Harmony execution, saves or networking.
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace MegaCrit.Sts2.Core.Entities.Creatures
{
    public class Creature
    {
        public List<PowerModel> Powers { get; } = [];
        public bool IsPlayer { get; set; }
        public bool IsHittable => Powers.All(power => power.ShouldAllowHitting(this));
    }
    public record DamageTargetData(Creature target)
    {
        public string Vfx { get; init; } = "default";
        public int Marker { get; init; }
    }
}
namespace MegaCrit.Sts2.Core.Entities.Powers
{
    public enum PowerType { None, Buff, Debuff }
    public enum PowerStackType { Single, Counter }
}
namespace MegaCrit.Sts2.Core.Models
{
    public abstract class PowerModel
    {
        public required Creature Owner { get; init; }
        public virtual bool ShouldAllowHitting(Creature creature) => true;
        public virtual bool ShouldAllowTargeting(Creature creature) => true;
    }
    public class CardModel
    {
        public bool IsValidTarget(Creature? target) => target != null;
    }
}
namespace LibraryLib.Models
{
    public abstract class DynamicPowerModel : PowerModel
    {
        protected abstract string LegacyPowerId { get; }
        public string LocalizationPrefix => LegacyPowerId;
        public abstract string PackedIconPath { get; }
        public abstract string ResolvedBigIconPath { get; }
        public abstract PowerType Type { get; }
        public abstract PowerStackType StackType { get; }
    }
}
namespace MegaCrit.Sts2.Core.Commands.Builders { public class AttackCommand { } }
namespace LibraryLib.Commands { public class LibraryAttackCommand { } }
namespace MegaCrit.Sts2.Core.Commands { public static class CreatureCmd { public static void Stun() { } } }
namespace MegaCrit.Sts2.Core.Logging { public static class Log { public static void Error(string message) { } } }
namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class HarmonyPatch(params object[] arguments) : Attribute
    {
        public object[] Arguments { get; } = arguments;
    }
    public sealed class HarmonyPostfix : Attribute { }
    public sealed class HarmonyPrefix : Attribute { }
    public sealed class HarmonyPriority(int priority) : Attribute
    {
        public int Value { get; } = priority;
    }
    public static class Priority { public const int Last = 0; }
}
