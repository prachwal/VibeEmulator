namespace VibeEmulator.Z80;

public sealed partial class Z80Core
{
    private void TickMemoryCycle()
    {
        int tState = _elapsedTStates - _memoryCycleStart;
        switch (_machineCycle)
        {
            case MachineCycle.MemoryRead:
                TickMemoryRead(tState);
                break;
            case MachineCycle.MemoryWrite:
                TickMemoryWrite(tState);
                break;
            case MachineCycle.Internal:
                TickInternalCycle();
                break;
            default:
                LastAddress = InstructionPc;
                LastData = null;
                LastPins = "----";
                break;
        }
    }

    private void TickMemoryRead(int tState)
    {
        switch (tState)
        {
            case 0:
                LastAddress = _cycleAddress;
                LastData = null;
                LastPins = "----";
                break;
            case 1:
                _cycleReadValue = _bus.Read(_cycleAddress);
                LastAddress = _cycleAddress;
                LastData = null;
                LastPins = "r-m-";
                break;
            default:
                LastAddress = _cycleAddress;
                LastData = _cycleReadValue;
                LastPins = "----";
                CompleteMemoryRead(_cycleReadValue);
                break;
        }
    }

    private void TickMemoryWrite(int tState)
    {
        switch (tState)
        {
            case 0:
                LastAddress = _cycleAddress;
                LastData = null;
                LastPins = "----";
                break;
            case 1:
                _bus.Write(_cycleAddress, _cycleWriteValue);
                LastAddress = _cycleAddress;
                LastData = _cycleWriteValue;
                LastPins = "-wm-";
                break;
            default:
                LastAddress = _cycleAddress;
                LastData = null;
                LastPins = "----";
                CompleteMemoryWrite();
                break;
        }
    }

    private void CompleteMemoryRead(byte value)
    {
        switch (_readPurpose)
        {
            case ReadPurpose.AluImmediate:
                _state.PC++;
                ApplyAlu(value);
                break;
            case ReadPurpose.AluOperand:
                ApplyAlu(value);
                break;
            case ReadPurpose.IncrementHl:
                (_cycleWriteValue, _state.F) = Z80Alu.Increment(value, _state.F, (_opcode & 1) != 0);
                StartInternal(_cycleAddress, 1, InternalPurpose.IncrementWrite);
                break;
            case ReadPurpose.Immediate:
                _state.PC++;
                if (_opcode == 0x36)
                {
                    _cycleWriteValue = value;
                    StartWrite(HlAddress(), value, WritePurpose.ImmediateToHl);
                }
                else
                {
                    SetRegister(_registerCode, value);
                }

                break;
            case ReadPurpose.AbsoluteLow:
                _absoluteLow = value;
                _state.PC++;
                StartRead(_state.PC, ReadPurpose.AbsoluteHigh);
                break;
            case ReadPurpose.AbsoluteHigh:
                _absoluteAddress = (ushort)(_absoluteLow | (value << 8));
                _state.PC++;
                if (_opcode == 0x3A)
                {
                    StartRead(_absoluteAddress, ReadPurpose.AccumulatorFromAbsolute);
                }
                else
                {
                    _cycleWriteValue = _state.A;
                    StartWrite(_absoluteAddress, _state.A, WritePurpose.AccumulatorToAbsolute);
                }

                break;
            case ReadPurpose.RegisterFromHl:
                SetRegister(_registerCode, value);
                break;
            case ReadPurpose.AccumulatorFromPair:
                _state.A = value;
                _state.WZ = (ushort)(_pairAddress + 1);
                break;
            case ReadPurpose.AccumulatorFromAbsolute:
                _state.A = value;
                _state.WZ = (ushort)(_absoluteAddress + 1);
                break;
            default:
                CompleteWordRead(value);
                break;
        }
    }

    private void CompleteMemoryWrite()
    {
        switch (_writePurpose)
        {
            case WritePurpose.AccumulatorToPair:
                _state.WZ = (ushort)((_state.A << 8) | ((_pairAddress + 1) & 0x00FF));
                break;
            case WritePurpose.AccumulatorToAbsolute:
                _state.WZ = (ushort)((_state.A << 8) | ((_absoluteAddress + 1) & 0x00FF));
                break;
            default:
                CompleteWordWrite();
                break;
        }
    }

    private void StartRead(ushort address, ReadPurpose purpose)
    {
        _memoryCycleStart = _elapsedTStates + 1;
        _machineCycle = MachineCycle.MemoryRead;
        _cycleAddress = address;
        _readPurpose = purpose;
    }

    private void StartWrite(ushort address, byte value, WritePurpose purpose)
    {
        _memoryCycleStart = _elapsedTStates + 1;
        _machineCycle = MachineCycle.MemoryWrite;
        _cycleAddress = address;
        _cycleWriteValue = value;
        _writePurpose = purpose;
    }
}
