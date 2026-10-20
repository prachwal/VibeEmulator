namespace VibeEmulator.Core.Cpu;

/// <summary>Tryby adresowania — strategia wyliczania adresu efektywnego.</summary>
public enum AddressingMode
{
    Implied,
    Immediate,
    ZeroPage,
    ZeroPageX,
    Absolute,
    AbsoluteX,
    Indirect,
}

/// <summary>
/// Wynik rozwiązania trybu adresowania. Dla trybów pamięciowych <see cref="Value"/>
/// jest wartością spod adresu efektywnego (odczyt następuje już przy dekodowaniu,
/// tak jak na realnej magistrali).
/// </summary>
public readonly record struct Operand(ushort Address, byte Value);

public static class Addressing
{
    /// <summary>Wylicza operand i przesuwa <see cref="CpuState.PC"/> o pobrane bajty.</summary>
    public static Operand Resolve(AddressingMode mode, ref CpuState state, IMemoryBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);

        return mode switch
        {
            AddressingMode.Implied => new Operand(0, 0),
            AddressingMode.Immediate => ReadImmediate(ref state, bus),
            AddressingMode.ZeroPage => ReadZeroPage(ref state, bus, 0),
            AddressingMode.ZeroPageX => ReadZeroPage(ref state, bus, state.X),
            AddressingMode.Absolute => ReadAbsolute(ref state, bus, 0),
            AddressingMode.AbsoluteX => ReadAbsolute(ref state, bus, state.X),
            AddressingMode.Indirect => ReadIndirect(ref state, bus),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported addressing mode."),
        };
    }

    private static Operand ReadImmediate(ref CpuState state, IMemoryBus bus)
    {
        ushort address = NextByte(ref state);
        return new Operand(address, bus.Read(address));
    }

    private static Operand ReadZeroPage(ref CpuState state, IMemoryBus bus, byte index)
    {
        // Indeksowanie w stronie zerowej zawija się w obrębie 0x00-0xFF.
        byte address = (byte)(bus.Read(NextByte(ref state)) + index);
        return new Operand(address, bus.Read(address));
    }

    private static Operand ReadAbsolute(ref CpuState state, IMemoryBus bus, byte index)
    {
        ushort address = (ushort)(FetchWord(ref state, bus) + index);
        return new Operand(address, bus.Read(address));
    }

    private static Operand ReadIndirect(ref CpuState state, IMemoryBus bus)
    {
        ushort pointer = FetchWord(ref state, bus);
        byte low = bus.Read(pointer);
        byte high = bus.Read((ushort)(pointer + 1));
        ushort address = (ushort)(low | (high << 8));

        return new Operand(address, bus.Read(address));
    }

    private static ushort NextByte(ref CpuState state)
    {
        ushort address = state.PC;
        state.PC = (ushort)(address + 1);
        return address;
    }

    private static ushort FetchWord(ref CpuState state, IMemoryBus bus)
    {
        byte low = bus.Read(NextByte(ref state));
        byte high = bus.Read(NextByte(ref state));

        return (ushort)(low | (high << 8));
    }
}
