using VibeEmulator.Memory;
using VibeEmulator.Mos6502;
using Xunit.Abstractions;

namespace VibeEmulator.Core.Tests;

public sealed partial class CounterEndToEndTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("nmos6502", 201)]
    [InlineData("wdc65c02", 101)]
    [InlineData("nes2a03", 201)]
    public void Assemble_LoadAndRun_CountsFromZeroToHundred(string name, int expectedWrites)
    {
        CpuVariant variant = name switch
        {
            "nmos6502" => CpuVariants.Nmos6502,
            "wdc65c02" => CpuVariants.Cmos65C02,
            "nes2a03" => CpuVariants.Nes2A03,
            _ => throw new ArgumentOutOfRangeException(nameof(name)),
        };
        string source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Examples", name + ".asm"));
        string directory = Path.Combine(Path.GetTempPath(), "vibe-counter-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string binaryPath = Path.Combine(directory, name + ".bin");
            File.WriteAllBytes(binaryPath, CounterAssembler.Compile(source, variant, 0x0200));
            byte[] binary = File.ReadAllBytes(binaryPath);
            Assert.Equal(Convert.FromHexString("A9008D0003EE0003AD0003C964D0F6"), binary);

            var bus = new DiagnosticMemory();
            bus.Memory.Load(0x0200, binary);
            var cpu = new StateMachineCpuCore(bus, variant);
            cpu.Reset(0x0200);
            int instructions = 0;
            while ((cpu.State.PC != 0x020F || cpu.Pending != 0) && cpu.Cycles < 2000)
            {
                cpu.Tick();
                if (cpu.Pending == 0)
                {
                    instructions++;
                }
            }

            output.WriteLine($"{variant.Name}: {binary.Length} B; PC=${cpu.State.PC:X4}; " +
                $"cykle={cpu.Cycles}; instrukcje={instructions}; zapisy={bus.Writes.Count}; $0300={bus.Memory.Read(0x0300)}");
            output.WriteLine("Zapisy $0300: " + string.Join(", ", bus.Writes));
            Assert.Equal(0x020F, cpu.State.PC);
            Assert.Equal(0, cpu.Pending);
            Assert.Equal(1505, cpu.Cycles);
            Assert.Equal(402, instructions);
            Assert.Equal(1505, bus.Accesses);
            Assert.Equal(100, bus.Memory.Read(0x0300));
            Assert.Equal(expectedWrites, bus.Writes.Count);
            byte[] expected = Enumerable.Range(0, 101).SelectMany(value =>
                variant.Rmw == RmwStyle.DoubleWrite && value < 100
                    ? new[] { (byte)value, (byte)value } : new[] { (byte)value }).ToArray();
            Assert.Equal(expected, bus.Writes);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class DiagnosticMemory : IMemoryBus
    {
        public FlatMemory Memory { get; } = new();
        public List<byte> Writes { get; } = [];
        public int Accesses { get; private set; }

        public byte Read(ushort address)
        {
            Accesses++;
            return Memory.Read(address);
        }

        public void Write(ushort address, byte value)
        {
            Accesses++;
            Memory.Write(address, value);
            if (address == 0x0300)
            {
                Writes.Add(value);
            }
        }
    }
}
