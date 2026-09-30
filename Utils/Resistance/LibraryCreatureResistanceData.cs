#nullable enable

namespace LibraryLib.Utils.Resistance;

/// <summary>
///     存储单个生物的物理抗性和混乱抗性数据。
///     包含斩击/打击/穿刺三种伤害类型的物理与混乱抗性等级。
/// </summary>
public sealed class LibraryCreatureResistanceData
{
    public class Resistance
    {
        public LibraryResistanceLevel Slash;
        public LibraryResistanceLevel Pierce;
        public LibraryResistanceLevel Blunt;

        public Resistance(LibraryResistanceLevel level)
        {
            Slash = level;
            Pierce = level;
            Blunt = level;
        }

        public Resistance() : this(LibraryResistanceLevel.Normal)
        {
        }

        public Resistance(Resistance other)
        {
            Slash = other.Slash;
            Pierce = other.Pierce;
            Blunt = other.Blunt;
        }

        /// <summary>该伤害类型的等级；无类型（None）视为 Normal。</summary>
        internal LibraryResistanceLevel Get(LibraryDamageType type) => type switch
        {
            LibraryDamageType.Blunt => Blunt,
            LibraryDamageType.Slash => Slash,
            LibraryDamageType.Pierce => Pierce,
            _ => LibraryResistanceLevel.Normal,
        };

        /// <summary>设置该伤害类型的等级；无类型（None）忽略。</summary>
        internal void Set(LibraryDamageType type, LibraryResistanceLevel level)
        {
            switch (type)
            {
                case LibraryDamageType.Blunt:
                    Blunt = level;
                    break;
                case LibraryDamageType.Slash:
                    Slash = level;
                    break;
                case LibraryDamageType.Pierce:
                    Pierce = level;
                    break;
            }
        }
    }

    public Resistance PhysicalResistance;
    public Resistance ChaosResistance;

    public LibraryCreatureResistanceData(LibraryResistanceLevel level)
    {
        PhysicalResistance = new Resistance(level);
        ChaosResistance = new Resistance(level);
    }

    public LibraryCreatureResistanceData()
    {
        PhysicalResistance = new Resistance(LibraryResistanceLevel.Normal);
        ChaosResistance = new Resistance(LibraryResistanceLevel.Immune);
    }

    public LibraryCreatureResistanceData(Resistance other)
    {
        PhysicalResistance = new Resistance(other);
        ChaosResistance = new Resistance(other);
    }

    public LibraryCreatureResistanceData(LibraryCreatureResistanceData other)
    {
        PhysicalResistance = new Resistance(other.PhysicalResistance);
        ChaosResistance = new Resistance(other.ChaosResistance);
    }
}
