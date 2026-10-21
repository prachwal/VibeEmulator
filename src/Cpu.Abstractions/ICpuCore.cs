namespace VibeEmulator.Cpu.Abstractions;

/// <summary>
/// Rdzeń procesora w modelu cyklowym. Kontrakt jest neutralny rodzinowo: nie zna rejestrów,
/// opkodów ani magistrali. Wymusza dwie rzeczy, których nie ma w atomowym
/// <c>Cpu&lt;TBus&gt;</c> z rodziny 6502: możliwość zatrzymania w środku instrukcji
/// (<see cref="Stall"/>) oraz postęp dokładnie jednego cyklu na <see cref="Tick"/>.
/// </summary>
public interface ICpuCore
{
    /// <summary>Liczba cykli pozostałych do końca bieżącej instrukcji.</summary>
    int Pending { get; }

    /// <summary>CPU zatrzymany przez nielegalny opkod (JAM/KIL) lub błąd.</summary>
    bool Jammed { get; }

    /// <summary>Łączna liczba cykli wykonanych przez rdzeń.</summary>
    long Cycles { get; }

    /// <summary>Adres początku aktualnie wykonywanej lub ostatnio wykonanej instrukcji.</summary>
    ushort InstructionPc { get; }

    /// <summary>Opkod aktualnie wykonywanej lub ostatnio wykonanej instrukcji.</summary>
    byte Opcode { get; }

    void Reset(ushort pc = 0);

    /// <summary>Wykonuje jeden cykl pracy procesora.</summary>
    void Tick();

    /// <summary>Cykl ukradziony przez VIC-II — procesor stoi, ale zegar maszyny biegnie.</summary>
    void Stall();
}

/// <summary>
/// Rdzeń procesora z dostępem do jego stanu rejestrowego. <typeparamref name="TState"/> to struktura
/// rejestrów danej rodziny (np. <c>CpuState</c> dla 6502/6510).
/// </summary>
public interface ICpuCore<TState> : ICpuCore
    where TState : struct
{
    /// <summary>Stan rejestrów. Zwracany przez referencję — manipulacja bez kopiowania.</summary>
    ref TState State { get; }
}
