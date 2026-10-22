namespace VibeEmulator.Z80;

public sealed partial class Z80Core
{
    private void TickCbOpcodeFetch(int tState)
    {
        _machineCycle = MachineCycle.OpcodeFetch;
        switch (tState)
        {
            case 0:
                LastAddress = _state.PC;
                LastData = null;
                LastPins = "----";
                break;
            case 1:
                _cbOpcode = _bus.Read(_state.PC);
                _state.PC++;
                _instructionTStates = GetCbInstructionTStates(_cbOpcode);
                LastAddress = (ushort)(_state.PC - 1);
                LastData = null;
                LastPins = "r-m-";
                break;
            case 2:
                LastAddress = _refreshAddress;
                LastData = _cbOpcode;
                LastPins = "----";
                break;
            default:
                LastAddress = _refreshAddress;
                LastData = null;
                LastPins = "----";
                _state.R = (byte)((_state.R + 1) & 0x7F);
                PrepareCbCycle();
                break;
        }
    }

    private int GetCbInstructionTStates(byte opcode)
    {
        if ((opcode & 7) != 6)
        {
            return 8;
        }

        return (opcode >> 6) == 1 ? 12 : 15;
    }

    private void PrepareCbCycle()
    {
        if ((_cbOpcode & 7) == 6)
        {
            StartRead(HlAddress(), ReadPurpose.CbOperand);
        }
    }

    private void TickEdOpcodeFetch(int tState)
    {
        _machineCycle = MachineCycle.OpcodeFetch;
        switch (tState)
        {
            case 0:
                LastAddress = _state.PC;
                LastData = null;
                LastPins = "----";
                break;
            case 1:
                _edOpcode = _bus.Read(_state.PC);
                _state.PC++;
                _instructionTStates = GetEdInstructionTStates(_edOpcode);
                LastAddress = (ushort)(_state.PC - 1);
                LastData = null;
                LastPins = "r-m-";
                break;
            case 2:
                LastAddress = _refreshAddress;
                LastData = _edOpcode;
                LastPins = "----";
                break;
            default:
                LastAddress = _refreshAddress;
                LastData = null;
                LastPins = "----";
                _state.R = (byte)((_state.R + 1) & 0x7F);
                PrepareEdCycle();
                break;
        }
    }
}
