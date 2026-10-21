using VibeEmulator.Memory;

namespace VibeEmulator.Mos6502;

/// <summary>
/// Fazy maszyny stanów: fetch, operand, indeksowanie, dummy read, dostęp i RMW.
/// Część <c>partial</c> rdzenia — operacje i ALU są w <c>StateMachineCpuCore.Operations.cs</c>.
/// </summary>
public sealed partial class StateMachineCpuCore
{
    private void Execute()
    {
        switch (_phase)
        {
            case Phase.Fetch:
                Fetch();
                break;

            case Phase.OperandLow:
                FetchOperandLow();
                break;

            case Phase.IndexInternal:
                // Dummy read spod adresu zerowej strony przed dodaniem indeksu — obie warianty (wektory).
                _ = _bus.Read(_lo);
                _index = SelectIndex();
                _address = (byte)(_lo + _index);
                _phase = _entry.Mode == AddressingMode.IndexedIndirect ? Phase.PointerLow : Phase.Access;
                break;

            case Phase.OperandHigh:
                FetchOperandHigh();
                break;

            case Phase.DummyRead:
                // Adres dummy read zależy od wariantu (IndexedDummyRead); odczyt jest tylko w cyklu dodatkowym.
                // Etap B: RMW w abs,X/abs,Y w 65C02 czyta spod PC-1 (jak odczyt), nie spod adresu bez korekty.
                // Wektory: FE/DE bez page cross pokazują odczyt spod PC-1 (cykl 4 = ostatni bajt operandu),
                // a 5E/7E z page cross — też spod PC-1, nie spod bazy+indeks.
                _ = _bus.Read(_ghostIndexedDummy
                    ? (ushort)((_baseAddress & 0xFF00) | ((_baseAddress + _index) & 0xFF))
                    : (ushort)(_state.PC - 1));
                _phase = Phase.Access;
                break;

            case Phase.Access:
                Access();
                break;

            case Phase.BranchOperand:
                ExecuteBranchOperand();
                break;

            case Phase.ImpliedRead:
                // Drugi cykl instrukcji jednobajtowej: dummy read spod PC; operacja wykonuje się w tym cyklu.
                _ = _bus.Read(_state.PC);
                ApplyImplied(_entry.Action);
                _internal--;
                _phase = _internal > 0 ? Phase.Internal : Phase.Fetch;
                break;

            case Phase.Internal:
                _internal--;
                if (_internal <= 0)
                {
                    _phase = Phase.Fetch;
                }

                break;

            case Phase.AccumulatorRead:
                // 65C02 INC A / DEC A: dummy read spod PC i zmiana akumulatora w tym samym cyklu.
                _ = _bus.Read(_state.PC);
                ApplyImplied(_entry.Action);
                _phase = Phase.Fetch;
                break;

            case Phase.PointerLow:
                _pointer = _bus.Read(_address);
                _address = (byte)(_address + 1); // wskaźnik zawija się w stronie zerowej
                _phase = Phase.PointerHigh;
                break;

            case Phase.PointerHigh:
                FetchPointerHigh();
                break;

            case Phase.RmwOld:
                if (_doubleReadRmw)
                {
                    _ = _bus.Read(_address);
                }
                else
                {
                    _bus.Write(_address, _value);
                }

                _phase = Phase.RmwNew;
                break;

            case Phase.RmwNew:
                // Etap B: ApplyRead liczy wartość bez dostępu do _state (metoda statyczna).
                // Dla przesunięć/rotacji Carry liczymy tutaj z _value i bieżącego C (wejście ROL/ROR).
                byte result = ApplyRead(_entry.Action, _value);
                if (_entry.Action is CpuAction.Asl or CpuAction.Lsr or CpuAction.Rol or CpuAction.Ror)
                {
                    bool carryIn = _state.P.Has(StatusFlags.Carry);
                    bool carry;
                    result = _entry.Action switch
                    {
                        CpuAction.Asl => ShiftLeft(_value, out carry),
                        CpuAction.Lsr => ShiftRight(_value, out carry),
                        CpuAction.Rol => RotateLeft(_value, carryIn, out carry),
                        _ => RotateRight(_value, carryIn, out carry),
                    };
                    _state.P = _state.P.With(StatusFlags.Carry, carry);
                }

                _bus.Write(_address, result);
                SetZeroNegative(result);
                _phase = Phase.Fetch;
                break;

            case Phase.BranchTaken:
                ExecuteBranchTaken();
                break;

            case Phase.BranchCrossed:
                ExecuteBranchCrossed();
                break;

            case Phase.StackDummy:
                ExecuteStackDummy();
                break;

            case Phase.Push:
                ExecutePush();
                break;

            case Phase.PullDummy:
                ExecutePullDummy();
                break;

            case Phase.Pull:
                ExecutePull();
                break;

            case Phase.JsrDummy:
                ExecuteJsrDummy();
                break;

            case Phase.JsrPushHigh:
                ExecuteJsrPushHigh();
                break;

            case Phase.JsrPushLow:
                ExecuteJsrPushLow();
                break;

            case Phase.JsrFetch:
                ExecuteJsrFetch();
                break;

            case Phase.RtsDummy:
                ExecuteRtsDummy();
                break;

            case Phase.RtsPullDummy:
                ExecuteRtsPullDummy();
                break;

            case Phase.RtsPullLow:
                ExecuteRtsPullLow();
                break;

            case Phase.RtsPullHigh:
                ExecuteRtsPullHigh();
                break;

            case Phase.RtsFetch:
                ExecuteRtsFetch();
                break;

            case Phase.RtiDummy:
                ExecuteRtiDummy();
                break;

            case Phase.RtiPullDummy:
                ExecuteRtiPullDummy();
                break;

            case Phase.RtiPullStatus:
                ExecuteRtiPullStatus();
                break;

            case Phase.RtiPullLow:
                ExecuteRtiPullLow();
                break;

            case Phase.RtiPullHigh:
                ExecuteRtiPullHigh();
                break;

            case Phase.JmpIndirectLow:
                ExecuteJmpIndirectLow();
                break;

            case Phase.JmpIndirectHigh:
                ExecuteJmpIndirectHigh();
                break;

            case Phase.JmpIndirectFixup:
                ExecuteJmpIndirectFixup();
                break;

            case Phase.InterruptSignature:
                ExecuteInterruptSignature();
                break;

            case Phase.InterruptPushPch:
                ExecuteInterruptPushPch();
                break;

            case Phase.InterruptPushPcl:
                ExecuteInterruptPushPcl();
                break;

            case Phase.InterruptPushP:
                ExecuteInterruptPushP();
                break;

            case Phase.InterruptVectorLow:
                ExecuteInterruptVectorLow();
                break;

            case Phase.InterruptVectorHigh:
                ExecuteInterruptVectorHigh();
                break;

            case Phase.DecimalExtra:
                ExecuteDecimalExtra();
                break;
        }
    }

    private void Fetch()
    {
        // Etap F: przerwania próbkowane na granicy instrukcji, przed pobraniem opkodu.
        // NMI (zalatentowane zbocze) ma priorytet i nie zależy od I. IRQ respektuje I i próbkuje
        // poziom w chwili Fetch; po CLI/SEI/PLP _deferIrq odsuwa obsługę o jedną instrukcję
        // (decyzja zapadła przy próbkowaniu przed zmianą I — docs/cpu/interrupts-and-reset.md).
        if (_nmiPending)
        {
            _nmiPending = false;
            StartInterrupt(InterruptSource.Nmi);
            return;
        }

        if (_deferIrq)
        {
            _deferIrq = false;
        }
        else if (IrqLevel && !_state.P.Has(StatusFlags.InterruptDisable))
        {
            StartInterrupt(InterruptSource.Irq);
            return;
        }

        InstructionPc = _state.PC;
        byte opcode = _bus.Read(_state.PC);
        Opcode = opcode;
        _state.PC++;

        CpuOpcode? found = _table[opcode];

        if (found is null)
        {
            _jammed = true;
            return;
        }

        _entry = found.Value;

        switch (_entry.Kind)
        {
            case OpKind.Implied:
                _internal = Math.Max(1, _entry.InternalCycles);
                _phase = Phase.ImpliedRead;
                break;

            case OpKind.Accumulator:
                _phase = Phase.AccumulatorRead;
                break;

            case OpKind.Jump:
                _phase = Phase.OperandLow;
                break;

            case OpKind.Branch:
                _phase = Phase.BranchOperand;
                break;

            case OpKind.Push:
            case OpKind.Pull:
                _phase = Phase.StackDummy;
                break;

            case OpKind.Subroutine:
                // Etap E: JSR czyta low operandu, potem dummy/pushe; high-byte dopiero w JsrFetch.
                // RTS/RTI zaczynają od dummy read spod PC.
                _phase = _entry.Action == CpuAction.Jsr ? Phase.OperandLow
                    : _entry.Action == CpuAction.Rts ? Phase.RtsDummy
                    : Phase.RtiDummy;
                break;

            case OpKind.Interrupt:
                // Etap F: BRK — opkod już pobrany, PC wskazuje bajt sygnatury. Sekwencja
                // siedmiu cykli kończy się skokiem przez wektor $FFFE/$FFFF.
                StartInterrupt(InterruptSource.Brk);
                break;

            default:
                _phase = _entry.Mode switch
                {
                    AddressingMode.Immediate => Phase.Access,
                    AddressingMode.ZeroPage
                        or AddressingMode.ZeroPageX
                        or AddressingMode.ZeroPageY
                        or AddressingMode.Absolute
                        or AddressingMode.AbsoluteX
                        or AddressingMode.AbsoluteY
                        or AddressingMode.IndexedIndirect
                        or AddressingMode.IndirectIndexed => Phase.OperandLow,
                    _ => Jam(),
                };
                break;
        }
    }

    private Phase Jam()
    {
        _jammed = true;
        return Phase.Fetch;
    }
}
