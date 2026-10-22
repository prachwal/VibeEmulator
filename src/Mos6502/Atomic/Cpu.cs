using VibeEmulator.Memory;

namespace VibeEmulator.Mos6502;

/// <summary>
/// Generyczny procesor sparametryzowany magistralą. Nie wie, czym jest pamięć:
/// dostaje <typeparamref name="TBus"/>, więc ta sama logika działa na płaskiej
/// pamięci, mapie regionów i urządzeniu peryferyjnym.
/// </summary>
/// <typeparam name="TBus">Magistrala pamięci.</typeparam>
public sealed class Cpu<TBus> where TBus : IMemoryBus
{
    private readonly TBus _bus;
    private CpuState _state;

    public Cpu(TBus bus, OpcodeTable table)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(table);

        _bus = bus;
        Table = table;
        _state = CpuState.Reset();
    }

    public TBus Bus => _bus;

    public OpcodeTable Table { get; }

    /// <summary>Zwraca referencję — pozwala ustawiać i czytać rejestry bez kopiowania.</summary>
    public ref CpuState State => ref _state;

    public void Reset(ushort pc = 0)
    {
        _state = CpuState.Reset();
        _state.PC = pc;
    }

    /// <summary>Pobiera opkod, dekoduje operand, wykonuje instrukcję. Zwraca wykonaną instrukcję.</summary>
    public Instruction Step()
    {
        byte opcode = _bus.Read(_state.PC);
        _state.PC++;

        Instruction instruction = Table.Get(opcode);
        Operand operand = Addressing.Resolve(instruction.Mode, ref _state, _bus);
        instruction.Operation(ref _state, in operand, _bus);

        return instruction;
    }

    /// <summary>
    /// Wykonuje instrukcje aż do BRK (flaga Break), nieznanego opkodu lub limitu kroków.
    /// Zwraca liczbę wykonanych instrukcji.
    /// </summary>
    public int Run(int maxSteps = 10_000)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxSteps);

        int executed = 0;

        while (executed < maxSteps)
        {
            if (_state.P.Has(StatusFlags.Break))
            {
                break;
            }

            if (!Table.IsDefined(_bus.Read(_state.PC)))
            {
                break;
            }

            Step();
            executed++;
        }

        return executed;
    }
}

/// <summary>Składa procesor z części: magistrali + grup opkodów + pojedynczych instrukcji.</summary>
public sealed class CpuBuilder<TBus> where TBus : IMemoryBus
{
    private readonly TBus _bus;
    private readonly OpcodeTableBuilder _opcodes = new();

    public CpuBuilder(TBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);
        _bus = bus;
    }

    public CpuBuilder<TBus> WithGroup(Action<OpcodeTableBuilder> group)
    {
        _opcodes.Compose(group);
        return this;
    }

    public CpuBuilder<TBus> With(byte opcode, string mnemonic, AddressingMode mode, Operation operation)
    {
        _opcodes.Define(opcode, mnemonic, mode, operation);
        return this;
    }

    public Cpu<TBus> Build() => new(_bus, _opcodes.Build());
}
