namespace VibeEmulator.Mos6502;

/// <summary>
/// Zestaw opkodów, którego dotyczy adnotacja. Wartości odpowiadają kolumnie <c>set</c>
/// w tabelach <c>docs/cpu/opcodes/*.csv</c>.
/// </summary>
public enum CpuSet
{
    /// <summary>NMOS 6502/6510 — `set=nmos`, plik `nmos6502.csv`.</summary>
    Nmos,

    /// <summary>CMOS 65C02 — `set=c02`, plik `wdc65c02.csv`.</summary>
    Cmos65C02,
}

/// <summary>
/// Adnotacja pojedynczego opkodu: procesor (zestaw), bajt opkodu i pełny wpis tabeli wariantu.
/// Nakładana na pola <see cref="OpcodeCatalog"/>, skąd czyta je narzędzie
/// <c>tools/OpcodeInfo --verify-code</c> i porównuje z tabelami CSV.
/// </summary>
/// <remarks>
/// Wartość pola i argumenty adnotacji muszą być zgodne — pilnuje tego test
/// <c>OpcodeCatalogTests.Katalog_PoleMaAdnotacjeZgodneZWartoscia</c>.
/// Adnotacja jest metadanymi: ścieżka wykonania (<c>Tick</c>) czyta wyłącznie tabelę wariantu.
/// </remarks>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class OpcodeAttribute : Attribute
{
    /// <param name="opcode">Bajt opkodu (0x00–0xFF).</param>
    /// <param name="set">Procesor/zestaw, w którym opkod jest zarejestrowany.</param>
    /// <param name="mnemonic">Mnemonic zgodny z tabelą CSV.</param>
    /// <param name="mode">Tryb adresowania.</param>
    /// <param name="kind">Rodzaj dostępu do magistrali.</param>
    /// <param name="action">Akcja wykonywana przez rdzeń cyklowy.</param>
    /// <param name="internalCycles">Cykle wewnętrzne bez dostępu do magistrali (np. 6 dla BRK).</param>
    public OpcodeAttribute(
        int opcode,
        CpuSet set,
        string mnemonic,
        AddressingMode mode,
        OpKind kind,
        CpuAction action,
        int internalCycles = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(opcode);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(opcode, 0xFF);
        ArgumentException.ThrowIfNullOrWhiteSpace(mnemonic);
        ArgumentOutOfRangeException.ThrowIfNegative(internalCycles);

        Opcode = (byte)opcode;
        Set = set;
        Mnemonic = mnemonic;
        Mode = mode;
        Kind = kind;
        Action = action;
        InternalCycles = internalCycles;
    }

    public byte Opcode { get; }

    public CpuSet Set { get; }

    public string Mnemonic { get; }

    public AddressingMode Mode { get; }

    public OpKind Kind { get; }

    public CpuAction Action { get; }

    public int InternalCycles { get; }

    /// <summary>Nazwa zestawu w tabelach CSV: `nmos` albo `c02`.</summary>
    public string CsvSet => Set switch
    {
        CpuSet.Nmos => "nmos",
        CpuSet.Cmos65C02 => "c02",
        _ => throw new ArgumentOutOfRangeException(nameof(Set), Set, "Nieznany zestaw opkodów."),
    };

    /// <summary>Wpis tabeli wariantu zbudowany z adnotacji.</summary>
    public CpuOpcode ToOpcode() => new(Opcode, Mnemonic, Mode, Kind, Action, InternalCycles);
}
