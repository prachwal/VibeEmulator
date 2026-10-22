using System.Diagnostics;
using System.Text;
using VibeEmulator.Memory;
using VibeEmulator.Z80;

namespace VibeEmulator.Z80Harness;

public static class ZexHarnessRunner
{
    public const ushort ProgramOrigin = 0x0100;
    public const ushort BdosAddress = 0x0005;
    public const ushort BdosStack = 0xF000;
    private const int MaximumCapturedOutput = 1_048_576;

    public static ZexHarnessResult Run(ReadOnlySpan<byte> image, long limitTStates, TextWriter output)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limitTStates);
        ArgumentNullException.ThrowIfNull(output);
        if (image.IsEmpty || image.Length > ushort.MaxValue + 1 - ProgramOrigin)
        {
            throw new ArgumentException("plik .COM musi mieć od 1 do 65280 bajtów.", nameof(image));
        }

        FlatMemory memory = new();
        memory.Write(0x0000, 0xC3);
        memory.Write(0x0001, 0x00);
        memory.Write(0x0002, 0x00);
        memory.Write(BdosAddress, 0xC3);
        memory.Write(0x0006, (byte)(BdosStack & 0xFF));
        memory.Write(0x0007, (byte)(BdosStack >> 8));
        memory.Load(ProgramOrigin, image);

        Z80Core cpu = new(memory, new FlatPorts());
        cpu.Reset(ProgramOrigin);
        cpu.State.SP = BdosStack;

        BoundedOutputWriter capturedOutput = new(output, MaximumCapturedOutput);
        Stopwatch stopwatch = Stopwatch.StartNew();
        long tStates = 0;

        while (tStates < limitTStates)
        {
            if (cpu.Pending == 0 && !cpu.Halted)
            {
                if (cpu.State.PC == 0)
                {
                    stopwatch.Stop();
                    return Result(capturedOutput, tStates, stopwatch.Elapsed, capturedOutput.HasError
                        ? ZexHarnessOutcome.Fail
                        : ZexHarnessOutcome.Pass, capturedOutput.HasError ? "program zgłosił ERROR" : null);
                }

                if (cpu.State.PC == BdosAddress)
                {
                    string? error = DispatchBdos(cpu, memory, capturedOutput);
                    if (error is not null)
                    {
                        capturedOutput.WriteLine($"error: {error}");
                        stopwatch.Stop();
                        return Result(capturedOutput, tStates, stopwatch.Elapsed, ZexHarnessOutcome.Fail, error);
                    }

                    continue;
                }
            }

            cpu.Tick();
            tStates++;
        }

        stopwatch.Stop();
        return Result(capturedOutput, tStates, stopwatch.Elapsed, ZexHarnessOutcome.Limit, "osiągnięto limit T-state");
    }

    private static string? DispatchBdos(Z80Core cpu, FlatMemory memory, BoundedOutputWriter output)
    {
        Z80State state = cpu.State;
        switch (state.C)
        {
            case 2:
                output.Write((char)state.E);
                break;
            case 9:
                ushort address = (ushort)((state.D << 8) | state.E);
                bool terminated = false;
                for (int count = 0; count < 0x10000; count++)
                {
                    byte value = memory.Read(address);
                    address = (ushort)(address + 1);
                    if (value == '$')
                    {
                        terminated = true;
                        break;
                    }

                    output.Write((char)value);
                }

                if (!terminated)
                {
                    return "BDOS 9: łańcuch nie zawiera terminatora '$' w przestrzeni 64 KB";
                }

                break;
            default:
                return $"nieobsługiwana funkcja BDOS {state.C}";
        }

        ushort stackPointer = state.SP;
        byte returnLow = memory.Read(stackPointer);
        byte returnHigh = memory.Read((ushort)(stackPointer + 1));
        cpu.State.SP = (ushort)(stackPointer + 2);
        cpu.State.PC = (ushort)(returnLow | (returnHigh << 8));
        return null;
    }

    private static ZexHarnessResult Result(
        BoundedOutputWriter output,
        long tStates,
        TimeSpan elapsed,
        ZexHarnessOutcome outcome,
        string? error) =>
        new(outcome, tStates, elapsed, output.ToString(), output.Truncated, error);

    private sealed class BoundedOutputWriter(TextWriter destination, int maximumCharacters) : TextWriter
    {
        private readonly StringBuilder _buffer = new(Math.Min(maximumCharacters, 4096));
        private string _tail = string.Empty;

        public override System.Text.Encoding Encoding => destination.Encoding;

        public bool HasError { get; private set; }

        public bool Truncated { get; private set; }

        public override void Write(char value)
        {
            destination.Write(value);
            string candidate = _tail + value;
            HasError |= candidate.Contains("ERROR", StringComparison.OrdinalIgnoreCase);
            _tail = candidate.Length <= 4 ? candidate : candidate[^4..];
            if (_buffer.Length < maximumCharacters)
            {
                _buffer.Append(value);
            }
            else
            {
                Truncated = true;
            }
        }

        public override void WriteLine(string? value)
        {
            Write(value);
            Write(NewLine);
        }

        public override void Write(string? value)
        {
            if (value is null)
            {
                return;
            }

            foreach (char character in value)
            {
                Write(character);
            }
        }

        public override string ToString() => _buffer.ToString();
    }
}
