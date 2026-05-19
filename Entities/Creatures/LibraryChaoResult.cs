
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.ValueProps;

public class LibraryChaoResult//相当于DamageResult，用于存储Chao值变化结果
{
    public LibraryChaoResult(Creature receiver, ValueProp props)
    {
		Receiver = receiver;
		Props = props;
	}

    public Creature Receiver { get; }
    public ValueProp Props { get; }
    public int ChaoAmount { get; set; }
    public int OverStunChao { get; set; }
    public bool WasStun { get; init; }
}