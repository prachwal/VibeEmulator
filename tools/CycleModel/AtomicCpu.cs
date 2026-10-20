namespace CycleModel;

/// <summary>
/// Model A: instrukcja jest atomowa. Wszystkie dostępy wykonują się w jednym momencie,
/// a liczba cykli jest dopisywana po fakcie.
/// </summary>
internal sealed class AtomicCpu(IBus bus)
{
    private readonly IBus _bus = bus;
    private ushort _pc;

    public byte A { get; private set; }

    public int Cycles { get; private set; }

    public long Instructions { get; private set; }

    public bool Halted { get; private set; }

    public void Reset(ushort pc)
    {
        _pc = pc;
        Cycles = 0;
        Halted = false;
    }

    public void Step()
    {
        if (Halted)
        {
            return;
        }

        // Wszystkie dostępy tej instrukcji widzą TEN SAM cykl — stąd batching.
        _bus.Cycle = Cycles;
        Instructions++;
        byte opcode = _bus.Read(_pc++);

        switch (opcode)
        {
            case 0xA9: // LDA #imm
                A = _bus.Read(_pc++);
                Cycles += 2;
                break;

            case 0xA5: // LDA zp
                byte zeroPage = _bus.Read(_pc++);
                A = _bus.Read(zeroPage);
                Cycles += 3;
                break;

            case 0x8D: // STA abs
                _bus.Write(Absolute(), A);
                Cycles += 4;
                break;

            case 0xAD: // LDA abs
                A = _bus.Read(Absolute());
                Cycles += 4;
                break;

            case 0xEE: // INC abs — brak zapisu "starej" wartości, tylko wynik
                ushort target = Absolute();
                byte value = _bus.Read(target);
                _bus.Write(target, (byte)(value + 1));
                Cycles += 6;
                break;

            case 0xEA: // NOP
                Cycles += 2;
                break;

            case 0x4C: // JMP abs
                _pc = Absolute();
                Cycles += 3;
                break;

            default:
                Halted = true;
                break;
        }
    }

    private ushort Absolute()
    {
        byte low = _bus.Read(_pc++);
        byte high = _bus.Read(_pc++);

        return (ushort)(low | (high << 8));
    }
}
