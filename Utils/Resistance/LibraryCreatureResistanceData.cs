#nullable enable

namespace LibraryLib.Utils.Resistance;

/// <summary>
///     存储单个生物的物理抗性和混乱抗性数据。
///     包含斩击/打击/穿刺三种伤害类型的物理与混乱抗性等级。
/// </summary>
public sealed class LibraryCreatureResistanceData
{
    public class Resistance{
        public Resistance(LibraryResistanceLevel level){
            Slash = level;
            Pierce = level;
            Blunt = level;
        }
        public Resistance():this(LibraryResistanceLevel.Normal){
        }
        public Resistance(Resistance other)
        {
            Slash = other.Slash;
            Pierce = other.Pierce;
            Blunt = other.Blunt;
        }
        public LibraryResistanceLevel Slash;
        public LibraryResistanceLevel Pierce;
        public LibraryResistanceLevel Blunt;
    }
        public LibraryCreatureResistanceData(LibraryResistanceLevel level){
            PhysicalResistance = new Resistance(level);
            ChaosResistance = new Resistance(level);
        }
        public LibraryCreatureResistanceData(){
            PhysicalResistance = new(LibraryResistanceLevel.Normal);
            ChaosResistance = new(LibraryResistanceLevel.Immune);
        }
        public LibraryCreatureResistanceData(Resistance other)
        {
            PhysicalResistance = new(other);
            ChaosResistance = new(other);
        }
        public LibraryCreatureResistanceData(LibraryCreatureResistanceData other)
        {
            PhysicalResistance = new(other.PhysicalResistance);
            ChaosResistance = new(other.ChaosResistance);
        }
    public Resistance PhysicalResistance;
    public Resistance ChaosResistance;
}
