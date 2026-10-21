using VibeEmulator.Memory;

namespace VibeEmulator.Core.C64;

/// <summary>
/// Magistrala C64: dekoder <c>$0000-$FFFF</c> z bankowaniem przez port procesora
/// oraz open bus dla nieobsługiwanych odczytów.
/// </summary>
public sealed class C64Bus : IMemoryBus, IPeekableBus
{
    private readonly byte[] _ram = new byte[0x10000];
    private readonly byte[]? _basicRom;
    private readonly byte[]? _kernalRom;
    private readonly byte[]? _charRom;

    public C64Bus(VicII vic, Cia6526 cia1, Cia6526 cia2, Sid6581 sid)
        : this(vic, cia1, cia2, sid, null, null, null)
    {
    }

    public C64Bus(
        VicII vic,
        Cia6526 cia1,
        Cia6526 cia2,
        Sid6581 sid,
        byte[]? basicRom,
        byte[]? kernalRom,
        byte[]? charRom)
    {
        ArgumentNullException.ThrowIfNull(vic);
        ArgumentNullException.ThrowIfNull(cia1);
        ArgumentNullException.ThrowIfNull(cia2);
        ArgumentNullException.ThrowIfNull(sid);

        Vic = vic;
        Cia1 = cia1;
        Cia2 = cia2;
        Sid = sid;
        _basicRom = basicRom;
        _kernalRom = kernalRom;
        _charRom = charRom;
    }

    public ProcessorPort Port { get; } = new();

    public VicII Vic { get; }

    public Cia6526 Cia1 { get; }

    public Cia6526 Cia2 { get; }

    public Sid6581 Sid { get; }

    /// <summary>Ostatnia wartość, która pojawiła się na magistrali (open bus).</summary>
    public byte OpenBus { get; private set; }

    /// <summary>Opcjonalny obserwator — ustawiany tylko przez narzędzia śledzące.</summary>
    public IBusObserver? Observer { get; set; }

    public Span<byte> Ram => _ram;

    public byte Read(ushort address)
    {
        byte value = ReadDecoded(address);

        // Każdy odczyt zostawia wartość na magistrali — z niej korzystają
        // nieoficjalne opkody i odczyty spod niezmapowanych adresów.
        OpenBus = value;
        Observer?.OnAccess(false, address, value);

        return value;
    }

    /// <summary>
    /// Odczyt diagnostyczny bez skutków ubocznych: nie zmienia <see cref="OpenBus"/>, nie wywołuje
    /// <see cref="Observer"/>, nie zmienia stanu żadnego urządzenia. Respektuje bankowanie
    /// (ROM-y, I/O, CHAR ROM) dokładnie tak jak <see cref="Read"/>.
    /// </summary>
    /// <remarks>
    /// Kontrakt: jeśli któreś urządzenie dostanie odczyt ze skutkiem ubocznym (np. kasowanie flag CIA ICR),
    /// Peek musi go omijać. Obecnie odczyty VIC-II, CIA i SID są czysto odczytowe, więc Peek deleguje
    /// do wspólnego dekodera <c>ReadDecoded</c>.
    /// </remarks>
    public byte Peek(ushort address) => ReadDecoded(address);

    public void Write(ushort address, byte value)
    {
        WriteDecoded(address, value);
        Observer?.OnAccess(true, address, value);
    }

    public void LoadRam(ushort address, ReadOnlySpan<byte> data) => data.CopyTo(_ram.AsSpan(address));

    private byte ReadDecoded(ushort address)
    {
        ProcessorPort port = Port;
        bool loram = port.BasicRomEnabled;
        bool hiram = port.KernalRomEnabled;
        bool io = port.IoVisible;

        if (address < 0xA000)
        {
            // CHAR ROM widoczny pod $1000-$1FFF przy wyłączonych obu ROM-ach i CHAREN=0.
            if (address is >= 0x1000 and <= 0x1FFF && !loram && !hiram && !io && _charRom is not null)
            {
                return _charRom[address - 0x1000];
            }

            return _ram[address];
        }

        if (address < 0xC000)
        {
            if (loram && _basicRom is not null)
            {
                return _basicRom[address - 0xA000];
            }

            return _ram[address];
        }

        if (address < 0xD000)
        {
            return _ram[address];
        }

        if (address < 0xE000)
        {
            if (io)
            {
                return ReadIo(address);
            }

            // CHAR ROM pod $D000-$DFFF, gdy I/O i ROM-y wyłączone.
            if (!loram && !hiram && _charRom is not null)
            {
                return _charRom[address - 0xD000];
            }

            return _ram[address];
        }

        if (hiram && _kernalRom is not null)
        {
            return _kernalRom[address - 0xE000];
        }

        return _ram[address];
    }

    private byte ReadIo(ushort address)
    {
        if (address < 0xD400)
        {
            return Vic.Read(address);
        }

        if (address < 0xD800)
        {
            return Sid.Read(address);
        }

        if (address < 0xDC00)
        {
            return _ram[address]; // $D800-$DBFF: kolor RAM (uproszczenie — bez efektu koloru)
        }

        if (address < 0xDD00)
        {
            return Cia1.Read(address);
        }

        if (address < 0xDE00)
        {
            return Cia2.Read(address);
        }

        return OpenBus;
    }

    private void WriteDecoded(ushort address, byte value)
    {
        ProcessorPort port = Port;
        bool loram = port.BasicRomEnabled;
        bool hiram = port.KernalRomEnabled;
        bool io = port.IoVisible;

        if (address < 0xA000)
        {
            _ram[address] = value;
            return;
        }

        if (address < 0xC000)
        {
            if (loram && _basicRom is not null)
            {
                return; // zapis do ROM ignorowany
            }

            _ram[address] = value;
            return;
        }

        if (address < 0xD000)
        {
            _ram[address] = value;
            return;
        }

        if (address < 0xE000)
        {
            if (io)
            {
                WriteIo(address, value);
                return;
            }

            _ram[address] = value;
            return;
        }

        if (hiram && _kernalRom is not null)
        {
            return;
        }

        _ram[address] = value;
    }

    private void WriteIo(ushort address, byte value)
    {
        if (address < 0xD400)
        {
            Vic.Write(address, value);
            return;
        }

        if (address < 0xD800)
        {
            Sid.Write(address, value);
            return;
        }

        if (address < 0xDD00)
        {
            _ram[address] = value; // $D800-$DBFF to nadal RAM kolorów
            return;
        }

        if (address < 0xDE00)
        {
            Cia1.Write(address, value);
            return;
        }

        if (address < 0xDF00)
        {
            Cia2.Write(address, value);
            return;
        }
    }
}
