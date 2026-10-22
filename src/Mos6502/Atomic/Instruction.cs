using VibeEmulator.Memory;

namespace VibeEmulator.Mos6502;

/// <summary>
/// Wykonanie instrukcji. Dostaje stan przez <c>ref</c>, więc może zmieniać rejestry
/// bez kopiowania; magistrala jest wstrzykiwana, więc operacja nie zna pamięci.
/// </summary>
public delegate void Operation(ref CpuState state, in Operand operand, IMemoryBus bus);

/// <summary>Pojedynczy wpis tablicy opkodów: nazwa, tryb adresowania, akcja.</summary>
public readonly record struct Instruction(string Mnemonic, AddressingMode Mode, Operation Operation);
