using System.Runtime.CompilerServices;

namespace EmuBench;

/// <summary>
/// Rdzeń interpretera: koszt pobrania i wywołania handlera instrukcji.
/// Porównuje cztery techniki dispatchu przy tej samej pracy na opkod.
/// </summary>
internal static unsafe class Dispatch
{
    private const int ProgramSize = 4096;
    private const int OpsInTable = 16;

    private static readonly byte[] Program = MakeProgram();
    private static readonly Action<int>[] Delegates = BuildDelegates();
    private static readonly delegate*<int, void>[] Pointers = BuildPointers();

    private static int s_accumulator;

    public static IReadOnlyList<Harness.Result> Run(int ops, int rounds)
    {
        return
        [
            new("loop only (baseline)", Harness.Measure(ops, () => LoopOnly(ops), rounds), "brak dispatchu"),
            new("switch 16 opcodes", Harness.Measure(ops, () => Switch16(Program, ops), rounds), "jump table"),
            new("delegate table 16", Harness.Measure(ops, () => DelegateTable(Program, ops), rounds), "Action<int>[]"),
            new("fnptr table 16", Harness.Measure(ops, () => PointerTable(Program, ops), rounds), "delegate*<int,void>[]"),
            new("direct call 16", Harness.Measure(ops, () => DirectCall(Program, ops), rounds), "bez tabeli"),
        ];
    }

    private static byte[] MakeProgram()
    {
        var program = new byte[ProgramSize];
        var rng = new Random(12345);

        for (int i = 0; i < program.Length; i++)
        {
            program[i] = (byte)rng.Next(OpsInTable);
        }

        return program;
    }

    private static Action<int>[] BuildDelegates()
    {
        // UWAGA: nie wolno budować tabeli jednym wyrażeniem lambda w pętli — wszystkie
        // wpisy wskazywałyby wtedy na tę samą metodę i predyktor gałęzi zawsze trafiał,
        // zaniżając koszt dispatchu do poziomu wywołania bezpośredniego.
        return
        [
            Op0, Op1, Op2, Op3, Op4, Op5, Op6, Op7,
            Op8, Op9, Op10, Op11, Op12, Op13, Op14, Op15,
        ];
    }

    private static delegate*<int, void>[] BuildPointers()
    {
        var table = new delegate*<int, void>[OpsInTable];

        for (int i = 0; i < OpsInTable; i++)
        {
            table[i] = i switch
            {
                0 => &Op0,
                1 => &Op1,
                2 => &Op2,
                3 => &Op3,
                4 => &Op4,
                5 => &Op5,
                6 => &Op6,
                7 => &Op7,
                8 => &Op8,
                9 => &Op9,
                10 => &Op10,
                11 => &Op11,
                12 => &Op12,
                13 => &Op13,
                14 => &Op14,
                _ => &Op15,
            };
        }

        return table;
    }

    private static void Apply(int v) => s_accumulator = (s_accumulator * 3 + v) & 0x7FFF;

    private static void Op0(int v) => s_accumulator = (s_accumulator * 3 + v) & 0x7FFF;
    private static void Op1(int v) => s_accumulator = (s_accumulator * 3 + v + 1) & 0x7FFF;
    private static void Op2(int v) => s_accumulator = (s_accumulator * 3 + v + 2) & 0x7FFF;
    private static void Op3(int v) => s_accumulator = (s_accumulator * 3 + v + 3) & 0x7FFF;
    private static void Op4(int v) => s_accumulator = (s_accumulator * 3 + v + 4) & 0x7FFF;
    private static void Op5(int v) => s_accumulator = (s_accumulator * 3 + v + 5) & 0x7FFF;
    private static void Op6(int v) => s_accumulator = (s_accumulator * 3 + v + 6) & 0x7FFF;
    private static void Op7(int v) => s_accumulator = (s_accumulator * 3 + v + 7) & 0x7FFF;
    private static void Op8(int v) => s_accumulator = (s_accumulator * 3 + v + 8) & 0x7FFF;
    private static void Op9(int v) => s_accumulator = (s_accumulator * 3 + v + 9) & 0x7FFF;
    private static void Op10(int v) => s_accumulator = (s_accumulator * 3 + v + 10) & 0x7FFF;
    private static void Op11(int v) => s_accumulator = (s_accumulator * 3 + v + 11) & 0x7FFF;
    private static void Op12(int v) => s_accumulator = (s_accumulator * 3 + v + 12) & 0x7FFF;
    private static void Op13(int v) => s_accumulator = (s_accumulator * 3 + v + 13) & 0x7FFF;
    private static void Op14(int v) => s_accumulator = (s_accumulator * 3 + v + 14) & 0x7FFF;
    private static void Op15(int v) => s_accumulator = (s_accumulator * 3 + v + 15) & 0x7FFF;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Switch16(byte[] program, int ops)
    {
        int acc = 0;

        for (int i = 0; i < ops; i++)
        {
            int op = program[i & (ProgramSize - 1)];

            switch (op)
            {
                case 0: acc = (acc * 3 + 0) & 0x7FFF; break;
                case 1: acc = (acc * 3 + 1) & 0x7FFF; break;
                case 2: acc = (acc * 3 + 2) & 0x7FFF; break;
                case 3: acc = (acc * 3 + 3) & 0x7FFF; break;
                case 4: acc = (acc * 3 + 4) & 0x7FFF; break;
                case 5: acc = (acc * 3 + 5) & 0x7FFF; break;
                case 6: acc = (acc * 3 + 6) & 0x7FFF; break;
                case 7: acc = (acc * 3 + 7) & 0x7FFF; break;
                case 8: acc = (acc * 3 + 8) & 0x7FFF; break;
                case 9: acc = (acc * 3 + 9) & 0x7FFF; break;
                case 10: acc = (acc * 3 + 10) & 0x7FFF; break;
                case 11: acc = (acc * 3 + 11) & 0x7FFF; break;
                case 12: acc = (acc * 3 + 12) & 0x7FFF; break;
                case 13: acc = (acc * 3 + 13) & 0x7FFF; break;
                case 14: acc = (acc * 3 + 14) & 0x7FFF; break;
                default: acc = (acc * 3 + 15) & 0x7FFF; break;
            }
        }

        return acc;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long DelegateTable(byte[] program, int ops)
    {
        s_accumulator = 0;

        for (int i = 0; i < ops; i++)
        {
            Delegates[program[i & (ProgramSize - 1)]](i);
        }

        return s_accumulator;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long PointerTable(byte[] program, int ops)
    {
        s_accumulator = 0;

        for (int i = 0; i < ops; i++)
        {
            Pointers[program[i & (ProgramSize - 1)]](i);
        }

        return s_accumulator;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long DirectCall(byte[] program, int ops)
    {
        s_accumulator = 0;

        for (int i = 0; i < ops; i++)
        {
            Apply(i);
        }

        return s_accumulator;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long LoopOnly(int ops)
    {
        int acc = 0;

        for (int i = 0; i < ops; i++)
        {
            acc += i;
        }

        return acc;
    }
}
