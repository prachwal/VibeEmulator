namespace VibeEmulator.Core.Cpu;

/// <summary>Niezmiennicza tablica 256 opkodów.</summary>
public sealed class OpcodeTable
{
    public const int OpcodeCount = 256;

    private readonly Instruction?[] _instructions;

    internal OpcodeTable(Instruction?[] instructions) => _instructions = instructions;

    public bool IsDefined(byte opcode) => _instructions[opcode] is not null;

    public Instruction Get(byte opcode) => _instructions[opcode]
        ?? throw new InvalidOperationException($"Undefined opcode 0x{opcode:X2}.");

    public Instruction this[byte opcode] => Get(opcode);

    public int DefinedCount => _instructions.Count(static instruction => instruction is not null);
}

/// <summary>
/// Budowniczy tablicy opkodów. Wspiera kompozycję: tablicę można złożyć z grup
/// (np. LoadStore + Arithmetic + ControlFlow) albo pojedynczych instrukcji.
/// </summary>
public sealed class OpcodeTableBuilder
{
    private readonly Instruction?[] _instructions = new Instruction?[OpcodeTable.OpcodeCount];

    public OpcodeTableBuilder Define(byte opcode, string mnemonic, AddressingMode mode, Operation operation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mnemonic);
        ArgumentNullException.ThrowIfNull(operation);

        if (_instructions[opcode] is not null)
        {
            throw new InvalidOperationException(
                $"Opcode 0x{opcode:X2} is already defined as {_instructions[opcode]!.Value.Mnemonic}.");
        }

        _instructions[opcode] = new Instruction(mnemonic, mode, operation);
        return this;
    }

    /// <summary>Dokłada grupę instrukcji — jednostkę kompozycji.</summary>
    public OpcodeTableBuilder Compose(Action<OpcodeTableBuilder> group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group(this);
        return this;
    }

    /// <summary>Buduje niemutowalną migawkę — późniejsze zmiany buildera jej nie dotyczą.</summary>
    public OpcodeTable Build() => new((Instruction?[])_instructions.Clone());
}
