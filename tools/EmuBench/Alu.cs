using System.Runtime.CompilerServices;

namespace EmuBench;

/// <summary>
/// Emulacja ALU: szerokość rejestru, flagi (eager vs lazy), mnożenie, dzielenie, rotacje.
/// </summary>
internal static class Alu
{
    public static IReadOnlyList<Harness.Result> Run(int ops, int rounds)
    {
        var operands = new byte[1 << 12];
        new Random(3).NextBytes(operands);

        return
        [
            new("add 8-bit masked", Harness.Measure(ops, () => Add8(operands, ops), rounds), "& 0xFF"),
            new("add 32-bit native", Harness.Measure(ops, () => Add32(operands, ops), rounds), "int"),
            new("add 8-bit + flags eager", Harness.Measure(ops, () => Add8FlagsEager(operands, ops), rounds), "ZF/SF/CF/OF"),
            new("add 8-bit flags lazy", Harness.Measure(ops, () => Add8FlagsLazy(operands, ops), rounds), "flagi na żądanie"),
            new("mul 16-bit", Harness.Measure(ops, () => Mul16(operands, ops), rounds)),
            new("div 16-bit", Harness.Measure(ops, () => Div16(operands, ops), rounds), "najdroższe"),
            new("rotate 8-bit", Harness.Measure(ops, () => Rotate8(operands, ops), rounds)),
            new("bcd adjust", Harness.Measure(ops, () => BcdAdjust(operands, ops), rounds), "6502/Z80"),
        ];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Add8(byte[] operands, int ops)
    {
        int acc = 0;

        for (int i = 0; i < ops; i++)
        {
            acc = (byte)(acc + operands[i & 4095]);
        }

        return acc;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Add32(byte[] operands, int ops)
    {
        int acc = 0;

        for (int i = 0; i < ops; i++)
        {
            acc += operands[i & 4095];
        }

        return acc;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Add8FlagsEager(byte[] operands, int ops)
    {
        int acc = 0;
        int flags = 0;

        for (int i = 0; i < ops; i++)
        {
            int a = acc & 0xFF;
            int b = operands[i & 4095];
            int sum = a + b;
            int result = sum & 0xFF;

            flags = (sum > 0xFF ? 1 : 0)                       // CF
                | (result == 0 ? 1 << 6 : 0)                    // ZF
                | ((result & 0x80) != 0 ? 1 << 7 : 0)           // SF
                | (((a ^ b ^ 0x80) & (a ^ result) & 0x80) != 0 ? 1 << 11 : 0); // OF

            acc = result;
        }

        return acc + flags;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Add8FlagsLazy(byte[] operands, int ops)
    {
        int acc = 0;

        // Flag nie liczymy w pętli — tylko ostatnią wartość, gdy ktoś o nie zapyta.
        for (int i = 0; i < ops; i++)
        {
            acc = (byte)(acc + operands[i & 4095]);
        }

        return acc + (acc == 0 ? 1 : 0) + (acc & 0x80);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Mul16(byte[] operands, int ops)
    {
        int acc = 1;

        for (int i = 0; i < ops; i++)
        {
            acc = (acc * (operands[i & 4095] | 1)) & 0xFFFF;
        }

        return acc;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Div16(byte[] operands, int ops)
    {
        int acc = 0x7FFF;

        for (int i = 0; i < ops; i++)
        {
            acc = (acc + 1) / (operands[i & 4095] | 1);
        }

        return acc;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Rotate8(byte[] operands, int ops)
    {
        int acc = 0x81;

        for (int i = 0; i < ops; i++)
        {
            int count = operands[i & 4095] & 7;
            acc = ((acc << count) | (acc >> (8 - count))) & 0xFF;
        }

        return acc;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long BcdAdjust(byte[] operands, int ops)
    {
        int acc = 0;

        for (int i = 0; i < ops; i++)
        {
            int a = acc & 0x0F;
            int b = operands[i & 4095] & 0x0F;
            int sum = a + b;

            if (sum > 9)
            {
                sum += 6;
            }

            acc = ((acc & 0xF0) + sum) & 0xFF;
        }

        return acc;
    }
}
