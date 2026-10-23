namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private static bool IsAccumulatorImmediateAluOpcode(byte opcode) =>
        opcode is 0x04 or 0x0C or 0x14 or 0x1C or 0x24 or 0x2C or 0x34 or 0x3C or 0xA8;

    private static bool IsRegisterDirectAluOpcode(byte opcode) =>
        opcode is 0x00 or 0x02 or 0x08 or 0x0A or 0x10 or 0x12 or 0x18 or 0x1A or
            0x20 or 0x22 or 0x28 or 0x2A or 0x30 or 0x32 or 0x38 or 0x3A or 0x84;

    private static bool IsRegisterDirectWordAluOpcode(byte opcode) =>
        opcode is 0x01 or 0x03 or 0x09 or 0x0B or 0x11 or 0x13 or 0x19 or 0x1B or
            0x21 or 0x23 or 0x29 or 0x2B or 0x31 or 0x33 or 0x39 or 0x3B or 0x85;

    private static bool IsWordIncDecGroupOpcode(byte opcode) => opcode == 0xFF;

    private static bool IsByteIncDecGroupOpcode(byte opcode) => opcode == 0xFE;

    private static bool IsUnaryNotNegGroupOpcode(byte opcode) => opcode is 0xF6 or 0xF7;

    private static bool IsSupportedUnaryNotNeg(byte opcode, byte modRm) =>
        IsUnaryNotNegGroupOpcode(opcode) && (modRm >> 6) == 3 && ((modRm >> 3) & 7) is 2 or 3;

    private static bool IsShiftRotateGroupOpcode(byte opcode) => opcode is >= 0xD0 and <= 0xD3;

    private static bool IsShiftRotateClGroupOpcode(byte opcode) => opcode is 0xD2 or 0xD3;

    private static bool IsSupportedShiftRotate(byte opcode, byte modRm) =>
        IsShiftRotateGroupOpcode(opcode) && (modRm >> 6) == 3 && ((modRm >> 3) & 7) is not 6;

    private static Intel8086AluOperation GetRegisterDirectAluOperation(byte opcode) => opcode switch
    {
        0x00 or 0x02 => Intel8086AluOperation.Add,
        0x08 or 0x0A => Intel8086AluOperation.Or,
        0x10 or 0x12 => Intel8086AluOperation.Adc,
        0x18 or 0x1A => Intel8086AluOperation.Sbb,
        0x20 or 0x22 => Intel8086AluOperation.And,
        0x28 or 0x2A => Intel8086AluOperation.Sub,
        0x30 or 0x32 => Intel8086AluOperation.Xor,
        0x38 or 0x3A => Intel8086AluOperation.Compare,
        0x84 => Intel8086AluOperation.Test,
        _ => throw new ArgumentOutOfRangeException(nameof(opcode)),
    };

    private static Intel8086AluOperation GetRegisterDirectWordAluOperation(byte opcode) => opcode switch
    {
        0x01 or 0x03 => Intel8086AluOperation.Add,
        0x09 or 0x0B => Intel8086AluOperation.Or,
        0x11 or 0x13 => Intel8086AluOperation.Adc,
        0x19 or 0x1B => Intel8086AluOperation.Sbb,
        0x21 or 0x23 => Intel8086AluOperation.And,
        0x29 or 0x2B => Intel8086AluOperation.Sub,
        0x31 or 0x33 => Intel8086AluOperation.Xor,
        0x39 or 0x3B => Intel8086AluOperation.Compare,
        0x85 => Intel8086AluOperation.Test,
        _ => throw new ArgumentOutOfRangeException(nameof(opcode)),
    };

    private static Intel8086AluOperation GetAccumulatorImmediateOperation(byte opcode) => opcode switch
    {
        0x04 => Intel8086AluOperation.Add,
        0x0C => Intel8086AluOperation.Or,
        0x14 => Intel8086AluOperation.Adc,
        0x1C => Intel8086AluOperation.Sbb,
        0x24 => Intel8086AluOperation.And,
        0x2C => Intel8086AluOperation.Sub,
        0x34 => Intel8086AluOperation.Xor,
        0x3C => Intel8086AluOperation.Compare,
        0xA8 => Intel8086AluOperation.Test,
        _ => throw new ArgumentOutOfRangeException(nameof(opcode)),
    };

    private static Intel8086AluOperation GetGroupImmediateAluOperation(byte extension) => extension switch
    {
        0 => Intel8086AluOperation.Add,
        1 => Intel8086AluOperation.Or,
        2 => Intel8086AluOperation.Adc,
        3 => Intel8086AluOperation.Sbb,
        4 => Intel8086AluOperation.And,
        5 => Intel8086AluOperation.Sub,
        6 => Intel8086AluOperation.Xor,
        7 => Intel8086AluOperation.Compare,
        _ => throw new ArgumentOutOfRangeException(nameof(extension)),
    };


    private static bool IsAccumulatorImmediateWordAluOpcode(byte opcode) =>
        opcode is 0x05 or 0x0D or 0x15 or 0x1D or 0x25 or 0x2D or 0x35 or 0x3D;

    private static Intel8086AluOperation GetAccumulatorImmediateWordOperation(byte opcode) => opcode switch
    {
        0x05 => Intel8086AluOperation.Add,
        0x0D => Intel8086AluOperation.Or,
        0x15 => Intel8086AluOperation.Adc,
        0x1D => Intel8086AluOperation.Sbb,
        0x25 => Intel8086AluOperation.And,
        0x2D => Intel8086AluOperation.Sub,
        0x35 => Intel8086AluOperation.Xor,
        0x3D => Intel8086AluOperation.Compare,
        _ => throw new ArgumentOutOfRangeException(nameof(opcode)),
    };

}
