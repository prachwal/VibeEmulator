namespace VibeEmulator.Z80;

public sealed partial class Z80Core
{
    private static byte GetCbRegisterCode(byte opcode) => (byte)(opcode & 7);

    private static bool CbChangesFlags(byte opcode) => (opcode >> 6) is 0 or 1;

    private void CompleteCbInstruction()
    {
        if (_cbPrefixActive || _indexCbPrefixActive)
        {
            byte code = GetCbRegisterCode(_cbOpcode);
            if (_indexCbPrefixActive)
            {
                byte indexedGroup = (byte)(_cbOpcode >> 6);
                if (code != 6 && indexedGroup != 1)
                {
                    SetRegister(code, _cycleWriteValue, indexMemoryPeer: true);
                }

                return;
            }

            if (code == 6)
            {
                return;
            }

            byte value = GetRegister(code);
            byte group = (byte)(_cbOpcode >> 6);
            if (group == 0)
            {
                (value, _state.F) = Z80BitOperations.RotateShift((byte)((_cbOpcode >> 3) & 7), value, _state.F);
            }
            else if (group == 1)
            {
                _state.F = Z80BitOperations.TestBit((byte)((_cbOpcode >> 3) & 7), value, _state.F, value);
            }
            else if (group == 2)
            {
                value = (byte)(value & ~(1 << ((_cbOpcode >> 3) & 7)));
            }
            else
            {
                value = (byte)(value | (1 << ((_cbOpcode >> 3) & 7)));
            }

            if (group != 1)
            {
                SetRegister(code, value);
            }

            return;
        }

        if (_opcode is not (0x07 or 0x0F or 0x17 or 0x1F))
        {
            return;
        }

        byte operation = _opcode switch
        {
            0x07 => 0,
            0x0F => 1,
            0x17 => 2,
            _ => 3,
        };
        (_state.A, _state.F) = Z80BitOperations.RotateAccumulator(operation, _state.A, _state.F);
    }

    private void CompleteCbMemoryRead(byte value)
    {
        byte group = (byte)(_cbOpcode >> 6);
        byte operation = (byte)((_cbOpcode >> 3) & 7);
        if (group == 0)
        {
            (_cycleWriteValue, _state.F) = Z80BitOperations.RotateShift(operation, value, _state.F);
        }
        else if (group == 1)
        {
            _state.F = Z80BitOperations.TestBit(operation, value, _state.F, (byte)(_state.WZ >> 8));
            StartInternal(_cycleAddress, 1, InternalPurpose.Finish);
            return;
        }
        else if (group == 2)
        {
            _cycleWriteValue = (byte)(value & ~(1 << operation));
        }
        else
        {
            _cycleWriteValue = (byte)(value | (1 << operation));
        }

        StartInternal(_cycleAddress, 1, InternalPurpose.CbWrite);
    }
}
