#nullable enable

namespace Library.Resistance;

/// <summary>
///     存储单个生物的物理抗性和混乱抗性数据。
///     包含斩击/打击/穿刺三种伤害类型的物理与混乱抗性等级。
/// </summary>
public sealed class LibraryCreatureResistanceData
{
    public class Resistance{
        public Resistance(LibraryResistanceLevel level){
            Slash = level;
            Blunt = level;
            Pierce = level;
        }
        public Resistance():this(LibraryResistanceLevel.Normal){
        }
        public Resistance(Resistance other)
        {
            Slash = other.Slash;
            Blunt = other.Blunt;
            Pierce = other.Pierce;
        }
        public LibraryResistanceLevel Slash;
        public LibraryResistanceLevel Blunt;
        public LibraryResistanceLevel Pierce;
    }
        public LibraryCreatureResistanceData(LibraryResistanceLevel level){
            PhysicalResistance = new(level);
            ChaosResistance = new(level);
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
    public Resistance PhysicalResistance = new(){
    };
    public Resistance ChaosResistance= new(){
    };
}
