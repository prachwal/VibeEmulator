namespace CycleModel;

/// <summary>
/// Model B: jeden <see cref="Tick"/> = jeden cykl = jeden dostęp do magistrali.
/// Instrukcja rozkłada się na sekwencję mikrooperacji; maszyna widzi każdą z nich.
/// </summary>
internal sealed class CycleCpu(IBus bus)
{
    private readonly IBus _bus = bus;
    private ushort _pc;
    private int _step;
    private byte _opcode;
    private byte _low;
    private byte _high;
    private byte _temp;

    public byte A { get; private set; }

    public int Cycles { get; private set; }

    public long Instructions { get; private set; }

    public bool Halted { get; private set; }

    /// <summary>Liczba cykli jeszcze pozostałych do końca bieżącej instrukcji.</summary>
    public int Pending => _step == 0 ? 0 : 1;

    public void Reset(ushort pc)
    {
        _pc = pc;
        _step = 0;
        Cycles = 0;
        Halted = false;
    }

    public void Tick()
    {
        if (Halted)
        {
            return;
        }

        _bus.Cycle = Cycles;

        switch (_step)
        {
            case 0:
                FetchOpcode();
                break;
            case 1:
                Step1();
                break;
            case 2:
                Step2();
                break;
            case 3:
                Step3();
                break;
            case 4:
                WriteOldValue();
                break;
            case 5:
                WriteNewValue();
                break;
            default:
                Halted = true;
                break;
        }

        Cycles++;
    }

    private void FetchOpcode()
    {
        _opcode = _bus.Read(_pc++);
        Instructions++;
        _step = 1;
    }

    private void Step1()
    {
        switch (_opcode)
        {
            case 0xA9: // LDA #imm
                A = _bus.Read(_pc++);
                _step = 0;
                break;

            case 0xA5: // LDA zp — najpierw adres, potem odczyt
                _low = _bus.Read(_pc++);
                _step = 2;
                break;

            case 0x8D: // STA abs
            case 0xAD: // LDA abs
            case 0xEE: // INC abs
                _low = _bus.Read(_pc++);
                _step = 2;
                break;

            case 0xEA: // NOP ma cykl wewnętrzny, bez dostępu
                _step = 0;
                break;

            case 0x4C: // JMP abs — adres czytany w dwóch cyklach
                _low = _bus.Read(_pc++);
                _step = 2;
                break;

            default:
                Halted = true;
                _step = 0;
                break;
        }
    }

    private void Step2()
    {
        if (_opcode == 0xA5)
        {
            A = _bus.Read(_low);
            _step = 0;
            return;
        }

        _high = _bus.Read(_pc++);

        if (_opcode == 0x4C)
        {
            _pc = (ushort)(_low | (_high << 8));
            _step = 0;
            return;
        }

        _step = 3;
    }

    private void Step3()
    {
        ushort address = (ushort)(_low | (_high << 8));

        switch (_opcode)
        {
            case 0x8D: // STA abs — zapis w 4. cyklu
                _bus.Write(address, A);
                _step = 0;
                break;

            case 0xAD: // LDA abs — odczyt w 4. cyklu
                A = _bus.Read(address);
                _step = 0;
                break;

            case 0xEE: // INC abs — read, write(old), write(new)
                _temp = _bus.Read(address);
                _step = 4;
                break;

            default:
                Halted = true;
                _step = 0;
                break;
        }
    }

    /// <summary>RMW: prawdziwy 6502 zapisuje z powrotem starą wartość, zanim zapisze nową.</summary>
    private void WriteOldValue()
    {
        ushort address = (ushort)(_low | (_high << 8));
        _bus.Write(address, _temp);
        _step = 5;
    }

    private void WriteNewValue()
    {
        ushort address = (ushort)(_low | (_high << 8));
        _bus.Write(address, (byte)(_temp + 1));
        _step = 0;
    }
}
