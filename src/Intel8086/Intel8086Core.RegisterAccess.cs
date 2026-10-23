namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private void SetByteRegister(int register, byte value)
    {
        switch (register)
        {
            case 0:
                _state.AX = (ushort)((_state.AX & 0xFF00) | value);
                break;
            case 1:
                _state.CX = (ushort)((_state.CX & 0xFF00) | value);
                break;
            case 2:
                _state.DX = (ushort)((_state.DX & 0xFF00) | value);
                break;
            case 3:
                _state.BX = (ushort)((_state.BX & 0xFF00) | value);
                break;
            case 4:
                _state.AX = (ushort)((_state.AX & 0x00FF) | (value << 8));
                break;
            case 5:
                _state.CX = (ushort)((_state.CX & 0x00FF) | (value << 8));
                break;
            case 6:
                _state.DX = (ushort)((_state.DX & 0x00FF) | (value << 8));
                break;
            case 7:
                _state.BX = (ushort)((_state.BX & 0x00FF) | (value << 8));
                break;
        }
    }

    private byte GetByteRegister(int register) => register switch
    {
        0 => (byte)_state.AX,
        1 => (byte)_state.CX,
        2 => (byte)_state.DX,
        3 => (byte)_state.BX,
        4 => (byte)(_state.AX >> 8),
        5 => (byte)(_state.CX >> 8),
        6 => (byte)(_state.DX >> 8),
        7 => (byte)(_state.BX >> 8),
        _ => throw new ArgumentOutOfRangeException(nameof(register)),
    };

}
