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
            case MachineCycle.PortRead:
                TickPortCycle(tState, input: true);
                break;
            case MachineCycle.PortWrite:
                TickPortCycle(tState, input: false);
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
                LastPins = "----";
                if (_indexedImmediateRead)
                {
                    LastData = tState == 2 ? _cycleReadValue : null;
                    if (tState == 2)
                    {
                        return;
                    }

                    _indexedImmediateRead = false;
                    CompleteMemoryRead(_cycleReadValue);
                    return;
                }

                LastData = _cycleReadValue;
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

    private void TickPortCycle(int tState, bool input)
    {
        switch (tState)
        {
            case 0:
                LastAddress = _portAddress;
                LastData = null;
                LastPins = "----";
                break;
            case 1:
                LastAddress = _portAddress;
                LastData = null;
                LastPins = "----";
                break;
            case 2:
                LastAddress = _portAddress;
                if (input)
                {
                    _cycleReadValue = _ports.Input(_portAddress);
                    LastData = null;
                    LastPins = "r--i";
                }
                else
                {
                    _ports.Output(_portAddress, _cycleWriteValue);
                    LastData = _cycleWriteValue;
                    LastPins = "-w-i";
                }

                break;
            default:
                LastAddress = _portAddress;
                LastData = input ? _cycleReadValue : null;
                LastPins = "----";
                CompletePortCycle(input);
                break;
        }
    }

    private void StartPortRead(ushort address)
    {
        _memoryCycleStart = _elapsedTStates + 1;
        _machineCycle = MachineCycle.PortRead;
        _portAddress = address;
    }

    private void StartPortWrite(ushort address, byte value)
    {
        _memoryCycleStart = _elapsedTStates + 1;
        _machineCycle = MachineCycle.PortWrite;
        _portAddress = address;
        _cycleWriteValue = value;
    }

    private void CompletePortCycle(bool input)
    {
        if (input)
        {
            if (_edPrefixActive)
            {
                CompleteEdPortRead(_cycleReadValue);
            }
            else
            {
                _state.A = _cycleReadValue;
                _state.WZ = (ushort)(_portAddress + 1);
            }
        }
        else if (_edPrefixActive)
        {
            CompleteEdPortWrite();
        }
        else
        {
            _state.WZ = _opcode == 0xD3
                ? (ushort)((_portAddress & 0xFF00) | ((_portAddress + 1) & 0x00FF))
                : (ushort)(_portAddress + 1);
        }
    }

    private void CompleteMemoryRead(byte value)
    {
        if (_readPurpose == ReadPurpose.IndexDisplacement)
        {
            CompleteIndexDisplacement(value);
            return;
        }

        if (_readPurpose == ReadPurpose.IndexCbOpcode)
        {
            CompleteIndexCbOpcode(value);
            return;
        }

        if (_edPrefixActive && _readPurpose is ReadPurpose.EdAbsoluteLow or ReadPurpose.EdAbsoluteHigh
            or ReadPurpose.EdPairLow or ReadPurpose.EdPairHigh or ReadPurpose.EdStackLow or ReadPurpose.EdStackHigh
            or ReadPurpose.EdNibble or ReadPurpose.EdBlockRead)
        {
            CompleteEdMemoryRead(value);
            return;
        }

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
                if (_opcode is 0xDB or 0xD3)
                {
                    _portAddress = (ushort)((_state.A << 8) | value);
                    if (_opcode == 0xDB)
                    {
                        StartPortRead(_portAddress);
                    }
                    else
                    {
                        StartPortWrite(_portAddress, _state.A);
                    }
                }
                else if (_opcode == 0x36)
                {
                    _cycleWriteValue = value;
                    if (_indexPrefixActive)
                    {
                        StartInternal((ushort)(_indexDisplacementAddress + 1), 1, InternalPurpose.IndexedImmediateWrite);
                    }
                    else
                    {
                        StartWrite(HlAddress(), value, WritePurpose.ImmediateToHl);
                    }
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
                SetRegister(_registerCode, value, indexMemoryPeer: _indexPrefixActive);
                break;
            case ReadPurpose.AccumulatorFromPair:
                _state.A = value;
                _state.WZ = (ushort)(_pairAddress + 1);
                break;
            case ReadPurpose.AccumulatorFromAbsolute:
                _state.A = value;
                _state.WZ = (ushort)(_absoluteAddress + 1);
                break;
            case ReadPurpose.CbOperand:
                CompleteCbMemoryRead(value);
                break;
            default:
                CompleteWordRead(value);
                break;
        }
    }

    private void CompleteMemoryWrite()
    {
        if (_edPrefixActive && _writePurpose is WritePurpose.EdAbsoluteLow or WritePurpose.EdAbsoluteHigh
            or WritePurpose.EdNibble or WritePurpose.EdBlockWrite)
        {
            CompleteEdMemoryWrite();
            return;
        }

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
