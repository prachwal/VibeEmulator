using VibeEmulator.Memory;

namespace VibeEmulator.Mos6502;

/// <summary>
/// Operacje procesora. Każda przyjmuje stan przez <c>ref</c>, więc działa bezpośrednio
/// na rejestrach — to jest poziom, z którego manipuluje się A/X/Y/PC/P.
/// </summary>
public static class Operations
{
    public static void Lda(ref CpuState state, in Operand operand, IMemoryBus bus)
    {
        state.A = operand.Value;
        SetZeroNegative(ref state, state.A);
    }

    public static void Ldx(ref CpuState state, in Operand operand, IMemoryBus bus)
    {
        state.X = operand.Value;
        SetZeroNegative(ref state, state.X);
    }

    public static void Ldy(ref CpuState state, in Operand operand, IMemoryBus bus)
    {
        state.Y = operand.Value;
        SetZeroNegative(ref state, state.Y);
    }

    public static void Sta(ref CpuState state, in Operand operand, IMemoryBus bus) => bus.Write(operand.Address, state.A);

    public static void Stx(ref CpuState state, in Operand operand, IMemoryBus bus) => bus.Write(operand.Address, state.X);

    public static void Sty(ref CpuState state, in Operand operand, IMemoryBus bus) => bus.Write(operand.Address, state.Y);

    public static void Adc(ref CpuState state, in Operand operand, IMemoryBus bus)
    {
        int carry = state.P.Has(StatusFlags.Carry) ? 1 : 0;
        int sum = state.A + operand.Value + carry;
        byte result = (byte)sum;
        bool overflow = (~(state.A ^ operand.Value) & (state.A ^ result) & 0x80) != 0;

        state.A = result;
        state.P = state.P
            .With(StatusFlags.Carry, sum > 0xFF)
            .With(StatusFlags.Overflow, overflow);

        SetZeroNegative(ref state, result);
    }

    public static void Sbc(ref CpuState state, in Operand operand, IMemoryBus bus)
    {
        int borrow = state.P.Has(StatusFlags.Carry) ? 0 : 1;
        int difference = state.A - operand.Value - borrow;
        byte result = (byte)difference;
        bool overflow = ((state.A ^ operand.Value) & (state.A ^ result) & 0x80) != 0;

        state.A = result;
        state.P = state.P
            .With(StatusFlags.Carry, difference >= 0)
            .With(StatusFlags.Overflow, overflow);

        SetZeroNegative(ref state, result);
    }

    public static void Inc(ref CpuState state, in Operand operand, IMemoryBus bus)
    {
        byte value = (byte)(bus.Read(operand.Address) + 1);
        bus.Write(operand.Address, value);
        SetZeroNegative(ref state, value);
    }

    public static void Dec(ref CpuState state, in Operand operand, IMemoryBus bus)
    {
        byte value = (byte)(bus.Read(operand.Address) - 1);
        bus.Write(operand.Address, value);
        SetZeroNegative(ref state, value);
    }

    public static void Inx(ref CpuState state, in Operand operand, IMemoryBus bus)
    {
        state.X++;
        SetZeroNegative(ref state, state.X);
    }

    public static void Dex(ref CpuState state, in Operand operand, IMemoryBus bus)
    {
        state.X--;
        SetZeroNegative(ref state, state.X);
    }

    public static void Iny(ref CpuState state, in Operand operand, IMemoryBus bus)
    {
        state.Y++;
        SetZeroNegative(ref state, state.Y);
    }

    public static void Dey(ref CpuState state, in Operand operand, IMemoryBus bus)
    {
        state.Y--;
        SetZeroNegative(ref state, state.Y);
    }

    public static void Tax(ref CpuState state, in Operand operand, IMemoryBus bus)
    {
        state.X = state.A;
        SetZeroNegative(ref state, state.X);
    }

    public static void Tay(ref CpuState state, in Operand operand, IMemoryBus bus)
    {
        state.Y = state.A;
        SetZeroNegative(ref state, state.Y);
    }

    public static void Txa(ref CpuState state, in Operand operand, IMemoryBus bus)
    {
        state.A = state.X;
        SetZeroNegative(ref state, state.A);
    }

    public static void Tya(ref CpuState state, in Operand operand, IMemoryBus bus)
    {
        state.A = state.Y;
        SetZeroNegative(ref state, state.A);
    }

    public static void Jmp(ref CpuState state, in Operand operand, IMemoryBus bus) => state.PC = operand.Address;

    public static void Nop(ref CpuState state, in Operand operand, IMemoryBus bus)
    {
        // Instrukcja pusta — celowo nic nie robi.
    }

    public static void Brk(ref CpuState state, in Operand operand, IMemoryBus bus) =>
        state.P = state.P.With(StatusFlags.Break, true);

    /// <summary>65C02: zapis zera pod adres operandu.</summary>
    public static void Stz(ref CpuState state, in Operand operand, IMemoryBus bus) => bus.Write(operand.Address, 0);

    /// <summary>65C02: INC A — zwiększa akumulator, ustawia N i Z.</summary>
    public static void IncA(ref CpuState state, in Operand operand, IMemoryBus bus)
    {
        state.A++;
        SetZeroNegative(ref state, state.A);
    }

    /// <summary>65C02: DEC A — zmniejsza akumulator, ustawia N i Z.</summary>
    public static void DecA(ref CpuState state, in Operand operand, IMemoryBus bus)
    {
        state.A--;
        SetZeroNegative(ref state, state.A);
    }

    private static void SetZeroNegative(ref CpuState state, byte value) =>
        state.P = state.P
            .With(StatusFlags.Zero, value == 0)
            .With(StatusFlags.Negative, (value & 0x80) != 0);
}
