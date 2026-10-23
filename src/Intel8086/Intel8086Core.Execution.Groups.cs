namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void ExecuteUnaryNotNegGroup()
    {
        byte modRm = _state.Queue[_state.QueueHead];
        PopQueue();
        PopQueue();
        _state.IP++;
        int register = modRm & 7;
        bool negate = ((modRm >> 3) & 7) == 3;
        if (Opcode == 0xF6)
        {
            Intel8086AluResult result = negate
                ? Intel8086Alu.ExecuteNegate(GetByteRegister(register), _state.Flags)
                : Intel8086Alu.ExecuteNot(GetByteRegister(register), _state.Flags);
            SetByteRegister(register, result.Value);
            _state.Flags = result.Flags;
        }
        else
        {
            Intel8086AluWordResult result = negate
                ? Intel8086Alu.ExecuteNegate(GetWordRegister(register), _state.Flags)
                : Intel8086Alu.ExecuteNot(GetWordRegister(register), _state.Flags);
            SetWordRegister(register, result.Value);
            _state.Flags = result.Flags;
        }

        LastBusCycle = IdleCycle('S', modRm);
    }

    private void ExecuteShiftRotateGroup()
    {
        byte modRm = _state.Queue[_state.QueueHead];
        PopQueue();
        PopQueue();
        _state.IP++;
        int register = modRm & 7;
        int extension = (modRm >> 3) & 7;
        if (Opcode == 0xD0)
        {
            Intel8086AluResult result = Intel8086Alu.ExecuteShiftRotate(GetByteRegister(register), _state.Flags, extension);
            SetByteRegister(register, result.Value);
            _state.Flags = result.Flags;
        }
        else
        {
            Intel8086AluWordResult result = Intel8086Alu.ExecuteShiftRotate(GetWordRegister(register), _state.Flags, extension);
            SetWordRegister(register, result.Value);
            _state.Flags = result.Flags;
        }

        LastBusCycle = IdleCycle('S', modRm);
        _instructionTState = 0;
    }
}
