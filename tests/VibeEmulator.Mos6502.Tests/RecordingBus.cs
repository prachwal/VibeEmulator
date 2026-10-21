using VibeEmulator.Memory;

namespace VibeEmulator.Mos6502.Tests;

/// <summary>Pojedynczy dostęp do magistrali: adres, wartość i kierunek.</summary>
public readonly record struct BusAccess(ushort Address, byte Value, bool IsWrite)
{
    public override string ToString() => $"{(IsWrite ? "write" : "read")} ${Address:X4}={Value:X2}";
}

/// <summary>
/// Płaska pamięć 64 KB, która rejestruje każdy dostęp wykonany przez rdzeń (odczyt i zapis).
/// <see cref="Poke"/> i <see cref="Peek"/> nie są rejestrowane — służą do przygotowania i sprawdzenia stanu.
/// </summary>
public sealed class RecordingBus : IMemoryBus
{
    private readonly byte[] _memory = new byte[0x10000];

    public List<BusAccess> Accesses { get; } = [];

    public byte Read(ushort address)
    {
        byte value = _memory[address];
        Accesses.Add(new BusAccess(address, value, IsWrite: false));
        return value;
    }

    public void Write(ushort address, byte value)
    {
        Accesses.Add(new BusAccess(address, value, IsWrite: true));
        _memory[address] = value;
    }

    public void Poke(ushort address, byte value) => _memory[address] = value;

    public byte Peek(ushort address) => _memory[address];
}
