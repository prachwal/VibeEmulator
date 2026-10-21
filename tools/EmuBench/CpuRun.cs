using System.Runtime.CompilerServices;
using VibeEmulator.Memory;
using VibeEmulator.Mos6502;

namespace EmuBench;

/// <summary>
/// Pomiar prawdziwego modelu CPU z <c>src/Mos6502</c> — nie atrapy.
/// Mierzy Step() (fetch + decode + execute), Run() oraz pętlę programu 6502.
/// </summary>
internal static class CpuRun
{
    private const ushort Origin = 0x0200;

    // Pętla: LDA $0300; ADC #$01; STA $0300; INX; BNE -4 (JMP $0200)
    private static readonly byte[] LoopProgram =
    [
        0xAD, 0x00, 0x03, // LDA $0300
        0x69, 0x01,       // ADC #$01
        0x8D, 0x00, 0x03, // STA $0300
        0xE8,             // INX
        0x4C, 0x00, 0x02, // JMP $0200
    ];

    public static IReadOnlyList<Harness.Result> Run(int ops, int rounds)
    {
        var memory = new FlatMemory();
        var cpu = Cpu6502.Create(memory);

        return
        [
            new("Step LDA #imm", Harness.Measure(ops, () => StepImmediate(cpu, memory, ops), rounds), "fetch+decode+exec"),
            new("Step STX zp (write)", Harness.Measure(ops, () => StepStore(cpu, memory, ops), rounds), "zapis do pamięci"),
            new("Step JMP abs", Harness.Measure(ops, () => StepJump(cpu, memory, ops), rounds), "kontrola przepływu"),
            new("Run loop 6502 (5 instr.)", Harness.Measure(ops, () => RunLoop(cpu, memory, ops), rounds), "cały cykl maszyny"),
        ];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long StepImmediate(Cpu<FlatMemory> cpu, FlatMemory memory, int ops)
    {
        memory.Load(Origin, [0xA9, 0x7F]); // LDA #$7F
        long executed = 0;

        for (int i = 0; i < ops; i++)
        {
            cpu.State.PC = Origin;
            cpu.Step();
            executed++;
        }

        return executed + cpu.State.A;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long StepStore(Cpu<FlatMemory> cpu, FlatMemory memory, int ops)
    {
        memory.Load(Origin, [0x86, 0x10]); // STX $10
        long executed = 0;

        for (int i = 0; i < ops; i++)
        {
            cpu.State.PC = Origin;
            cpu.State.X = (byte)i;
            cpu.Step();
            executed++;
        }

        return executed + memory.Read(0x10);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long StepJump(Cpu<FlatMemory> cpu, FlatMemory memory, int ops)
    {
        memory.Load(Origin, [0x4C, 0x00, 0x02]); // JMP $0200
        long executed = 0;

        for (int i = 0; i < ops; i++)
        {
            cpu.State.PC = Origin;
            cpu.Step();
            executed++;
        }

        return executed + cpu.State.PC;
    }

    /// <summary>Pętla 5 instrukcji: LDA/ADC/STA/INX/JMP. Zwraca liczbę wykonanych instrukcji.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long RunLoop(Cpu<FlatMemory> cpu, FlatMemory memory, int ops)
    {
        memory.Load(Origin, LoopProgram);
        cpu.Reset(Origin);

        // ops = liczba iteracji pętli, każda to 5 instrukcji.
        long executed = 0;

        for (int i = 0; i < ops; i++)
        {
            if (cpu.State.PC != Origin)
            {
                cpu.State.PC = Origin;
            }

            executed += cpu.Run(maxSteps: 5);
        }

        return executed;
    }
}
