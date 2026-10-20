namespace CycleModel;

/// <summary>Magistrala = zegar maszyny. Każdy dostęp to jeden cykl CPU i 3 doty PPU.</summary>
internal interface IBus
{
    int Cycle { get; set; }

    byte Read(ushort address);

    void Write(ushort address, byte value);
}

internal readonly record struct BusEvent(int Cycle, int Dots, char Kind, ushort Address, byte Value)
{
    public override string ToString() => $"cykl {Cycle,3}  dot {Dots,4}  {Kind} ${Address:X4} = ${Value:X2}";
}

internal sealed class RecordingBus : IBus
{
    private readonly byte[] _memory = new byte[0x10000];

    public int Cycle { get; set; }

    /// <summary>PPU rysuje 3 doty na każdy cykl CPU (5,37 MHz / 1,79 MHz).</summary>
    public int Dots => Cycle * 3;

    public bool Recording { get; set; } = true;

    public List<BusEvent> Log { get; } = [];

    public byte Read(ushort address)
    {
        byte value = _memory[address];

        if (Recording)
        {
            Log.Add(new BusEvent(Cycle, Dots, 'R', address, value));
        }

        return value;
    }

    public void Write(ushort address, byte value)
    {
        _memory[address] = value;

        if (Recording)
        {
            Log.Add(new BusEvent(Cycle, Dots, 'W', address, value));
        }
    }

    public void Load(ushort address, ReadOnlySpan<byte> data) => data.CopyTo(_memory.AsSpan(address));
}
