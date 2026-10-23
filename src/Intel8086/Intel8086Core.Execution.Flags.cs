namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private const ushort CarryFlag = 0x0001;
    private const ushort InterruptFlag = 0x0200;
    private const ushort DirectionFlag = 0x0400;
    private const ushort LahfSahfMask = 0x00D5;

    private static bool IsFlagControlOpcode(byte opcode) =>
        opcode is 0x9E or 0x9F or 0xF5 or 0xF8 or 0xF9 or 0xFA or 0xFB or 0xFC or 0xFD;

    private void TickFlagControl()
    {
        _instructionTState++;
        if (_instructionTState == 1)
        {
            PopQueue();
            _state.IP++;
            LastBusCycle = IdleCycle('F', Opcode);
            return;
        }

        if (Opcode == 0x9E && _queueCountAtStart <= 5 && _instructionTState == 3)
        {
            LastBusCycle = FetchCycle("T1", '-', 0);
            return;
        }

        if (_instructionTState < _instructionCycles)
        {
            LastBusCycle = IdleCycle('-', 0);
            return;
        }

        switch (Opcode)
        {
            case 0x9E:
                _state.Flags = (ushort)((_state.Flags & ~LahfSahfMask) | (GetByteRegister(4) & LahfSahfMask));
                break;
            case 0x9F:
                SetByteRegister(4, (byte)((_state.Flags & LahfSahfMask) | 0x02));
                break;
            case 0xF5:
                _state.Flags ^= CarryFlag;
                break;
            case 0xF8:
                _state.Flags &= unchecked((ushort)~CarryFlag);
                break;
            case 0xF9:
                _state.Flags |= CarryFlag;
                break;
            case 0xFA:
                _state.Flags &= unchecked((ushort)~InterruptFlag);
                break;
            case 0xFB:
                _state.Flags |= InterruptFlag;
                break;
            case 0xFC:
                _state.Flags &= unchecked((ushort)~DirectionFlag);
                break;
            case 0xFD:
                _state.Flags |= DirectionFlag;
                break;
            default:
                throw new InvalidOperationException($"Unexpected flag-control opcode {Opcode:X2}.");
        }

        LastBusCycle = Opcode == 0x9E && _queueCountAtStart <= 5
            ? FetchContinuationCycle('-', 0)
            : IdleCycle('-', 0);
        if (_state.QueueCount != 0)
        {
            PopQueue();
        }

        _instructionTState = 0;
    }
}
