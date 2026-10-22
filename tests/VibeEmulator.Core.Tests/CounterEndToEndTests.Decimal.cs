using VibeEmulator.Mos6502;

namespace VibeEmulator.Core.Tests;

public sealed partial class CounterEndToEndTests
{
    // Źródła oczekiwań: docs/cpu/decimal-mode.md oraz opcodes/{nmos6502,wdc65c02}.csv (69).
    [Theory]
    [InlineData("nmos6502", 0x10, 2)]
    [InlineData("wdc65c02", 0x10, 3)]
    [InlineData("nes2a03", 0x0A, 2)]
    public void Assemble_DecimalAddition_DistinguishesCpuVariants(string name, byte expectedResult, int expectedAdcCycles)
    {
        CpuVariant variant = name switch
        {
            "nmos6502" => CpuVariants.Nmos6502,
            "wdc65c02" => CpuVariants.Cmos65C02,
            "nes2a03" => CpuVariants.Nes2A03,
            _ => throw new ArgumentOutOfRangeException(nameof(name)),
        };
        string source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Examples", "decimal-addition.asm"));
        string directory = Path.Combine(Path.GetTempPath(), "vibe-decimal-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, name + ".bin");
            File.WriteAllBytes(path, CounterAssembler.Compile(source, variant, 0x0200));
            byte[] binary = File.ReadAllBytes(path);
            Assert.Equal(Convert.FromHexString("F818A90969018D0003"), binary);
            var bus = new DiagnosticMemory();
            bus.Memory.Load(0x0200, binary);
            var cpu = new StateMachineCpuCore(bus, variant);
            cpu.Reset(0x0200);
            var instructionCycles = new List<int>();

            while ((cpu.State.PC != 0x0209 || cpu.Pending != 0) && cpu.Cycles < 64)
            {
                int cycles = 0;
                do
                {
                    cpu.Tick();
                    cycles++;
                }
                while (cpu.Pending != 0 && cycles < 64);
                instructionCycles.Add(cycles);
            }

            output.WriteLine($"{variant.Name}: 09 + 01 przy D=1 → ${bus.Memory.Read(0x0300):X2}; " +
                $"ADC={instructionCycles[3]} cykle; program={cpu.Cycles} cykli; PC=${cpu.State.PC:X4}");
            Assert.Equal(0x0209, cpu.State.PC);
            Assert.Equal(0, cpu.Pending);
            Assert.Equal(new[] { 2, 2, 2, expectedAdcCycles, 4 }, instructionCycles);
            Assert.Equal(10 + expectedAdcCycles, cpu.Cycles);
            Assert.Equal(10 + expectedAdcCycles, bus.Accesses);
            Assert.Equal(expectedResult, cpu.State.A);
            Assert.Equal(expectedResult, bus.Memory.Read(0x0300));
            Assert.Equal(new[] { expectedResult }, bus.Writes);
            Assert.Equal(StatusFlags.Decimal, cpu.State.P & StatusFlags.Decimal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
