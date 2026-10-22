namespace VibeEmulator.Mos6502;

public static partial class OpcodeCatalog
{
    /// <summary>
    /// Etap A: logika, porównania i pozostałe tryby ADC/SBC (54 opkody).
    /// Opkody bazowe są w <c>OpcodeCatalog.Nmos.cs</c>; tablica <c>Opcodes</c> obejmuje oba pliki.
    /// </summary>
    public static partial class Nmos
    {
        [Opcode(0x01, CpuSet.Nmos, "ORA", AddressingMode.IndexedIndirect, OpKind.Read, CpuAction.Ora)]
        public static readonly CpuOpcode OraIndexedIndirect = new(0x01, "ORA", AddressingMode.IndexedIndirect, OpKind.Read, CpuAction.Ora);
        [Opcode(0x05, CpuSet.Nmos, "ORA", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Ora)]
        public static readonly CpuOpcode OraZeroPage = new(0x05, "ORA", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Ora);
        [Opcode(0x09, CpuSet.Nmos, "ORA", AddressingMode.Immediate, OpKind.Read, CpuAction.Ora)]
        public static readonly CpuOpcode OraImmediate = new(0x09, "ORA", AddressingMode.Immediate, OpKind.Read, CpuAction.Ora);
        [Opcode(0x0D, CpuSet.Nmos, "ORA", AddressingMode.Absolute, OpKind.Read, CpuAction.Ora)]
        public static readonly CpuOpcode OraAbsolute = new(0x0D, "ORA", AddressingMode.Absolute, OpKind.Read, CpuAction.Ora);
        [Opcode(0x11, CpuSet.Nmos, "ORA", AddressingMode.IndirectIndexed, OpKind.Read, CpuAction.Ora)]
        public static readonly CpuOpcode OraIndirectIndexed = new(0x11, "ORA", AddressingMode.IndirectIndexed, OpKind.Read, CpuAction.Ora);
        [Opcode(0x15, CpuSet.Nmos, "ORA", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Ora)]
        public static readonly CpuOpcode OraZeroPageX = new(0x15, "ORA", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Ora);
        [Opcode(0x19, CpuSet.Nmos, "ORA", AddressingMode.AbsoluteY, OpKind.Read, CpuAction.Ora)]
        public static readonly CpuOpcode OraAbsoluteY = new(0x19, "ORA", AddressingMode.AbsoluteY, OpKind.Read, CpuAction.Ora);
        [Opcode(0x1D, CpuSet.Nmos, "ORA", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Ora)]
        public static readonly CpuOpcode OraAbsoluteX = new(0x1D, "ORA", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Ora);
        [Opcode(0x21, CpuSet.Nmos, "AND", AddressingMode.IndexedIndirect, OpKind.Read, CpuAction.And)]
        public static readonly CpuOpcode AndIndexedIndirect = new(0x21, "AND", AddressingMode.IndexedIndirect, OpKind.Read, CpuAction.And);
        [Opcode(0x24, CpuSet.Nmos, "BIT", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Bit)]
        public static readonly CpuOpcode BitZeroPage = new(0x24, "BIT", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Bit);
        [Opcode(0x25, CpuSet.Nmos, "AND", AddressingMode.ZeroPage, OpKind.Read, CpuAction.And)]
        public static readonly CpuOpcode AndZeroPage = new(0x25, "AND", AddressingMode.ZeroPage, OpKind.Read, CpuAction.And);
        [Opcode(0x29, CpuSet.Nmos, "AND", AddressingMode.Immediate, OpKind.Read, CpuAction.And)]
        public static readonly CpuOpcode AndImmediate = new(0x29, "AND", AddressingMode.Immediate, OpKind.Read, CpuAction.And);
        [Opcode(0x2C, CpuSet.Nmos, "BIT", AddressingMode.Absolute, OpKind.Read, CpuAction.Bit)]
        public static readonly CpuOpcode BitAbsolute = new(0x2C, "BIT", AddressingMode.Absolute, OpKind.Read, CpuAction.Bit);
        [Opcode(0x2D, CpuSet.Nmos, "AND", AddressingMode.Absolute, OpKind.Read, CpuAction.And)]
        public static readonly CpuOpcode AndAbsolute = new(0x2D, "AND", AddressingMode.Absolute, OpKind.Read, CpuAction.And);
        [Opcode(0x31, CpuSet.Nmos, "AND", AddressingMode.IndirectIndexed, OpKind.Read, CpuAction.And)]
        public static readonly CpuOpcode AndIndirectIndexed = new(0x31, "AND", AddressingMode.IndirectIndexed, OpKind.Read, CpuAction.And);
        [Opcode(0x35, CpuSet.Nmos, "AND", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.And)]
        public static readonly CpuOpcode AndZeroPageX = new(0x35, "AND", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.And);
        [Opcode(0x39, CpuSet.Nmos, "AND", AddressingMode.AbsoluteY, OpKind.Read, CpuAction.And)]
        public static readonly CpuOpcode AndAbsoluteY = new(0x39, "AND", AddressingMode.AbsoluteY, OpKind.Read, CpuAction.And);
        [Opcode(0x3D, CpuSet.Nmos, "AND", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.And)]
        public static readonly CpuOpcode AndAbsoluteX = new(0x3D, "AND", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.And);
        [Opcode(0x41, CpuSet.Nmos, "EOR", AddressingMode.IndexedIndirect, OpKind.Read, CpuAction.Eor)]
        public static readonly CpuOpcode EorIndexedIndirect = new(0x41, "EOR", AddressingMode.IndexedIndirect, OpKind.Read, CpuAction.Eor);
        [Opcode(0x45, CpuSet.Nmos, "EOR", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Eor)]
        public static readonly CpuOpcode EorZeroPage = new(0x45, "EOR", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Eor);
        [Opcode(0x49, CpuSet.Nmos, "EOR", AddressingMode.Immediate, OpKind.Read, CpuAction.Eor)]
        public static readonly CpuOpcode EorImmediate = new(0x49, "EOR", AddressingMode.Immediate, OpKind.Read, CpuAction.Eor);
        [Opcode(0x4D, CpuSet.Nmos, "EOR", AddressingMode.Absolute, OpKind.Read, CpuAction.Eor)]
        public static readonly CpuOpcode EorAbsolute = new(0x4D, "EOR", AddressingMode.Absolute, OpKind.Read, CpuAction.Eor);
        [Opcode(0x51, CpuSet.Nmos, "EOR", AddressingMode.IndirectIndexed, OpKind.Read, CpuAction.Eor)]
        public static readonly CpuOpcode EorIndirectIndexed = new(0x51, "EOR", AddressingMode.IndirectIndexed, OpKind.Read, CpuAction.Eor);
        [Opcode(0x55, CpuSet.Nmos, "EOR", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Eor)]
        public static readonly CpuOpcode EorZeroPageX = new(0x55, "EOR", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Eor);
        [Opcode(0x59, CpuSet.Nmos, "EOR", AddressingMode.AbsoluteY, OpKind.Read, CpuAction.Eor)]
        public static readonly CpuOpcode EorAbsoluteY = new(0x59, "EOR", AddressingMode.AbsoluteY, OpKind.Read, CpuAction.Eor);
        [Opcode(0x5D, CpuSet.Nmos, "EOR", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Eor)]
        public static readonly CpuOpcode EorAbsoluteX = new(0x5D, "EOR", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Eor);
        [Opcode(0x61, CpuSet.Nmos, "ADC", AddressingMode.IndexedIndirect, OpKind.Read, CpuAction.Adc)]
        public static readonly CpuOpcode AdcIndexedIndirect = new(0x61, "ADC", AddressingMode.IndexedIndirect, OpKind.Read, CpuAction.Adc);
        [Opcode(0x65, CpuSet.Nmos, "ADC", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Adc)]
        public static readonly CpuOpcode AdcZeroPage = new(0x65, "ADC", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Adc);
        [Opcode(0x6D, CpuSet.Nmos, "ADC", AddressingMode.Absolute, OpKind.Read, CpuAction.Adc)]
        public static readonly CpuOpcode AdcAbsolute = new(0x6D, "ADC", AddressingMode.Absolute, OpKind.Read, CpuAction.Adc);
        [Opcode(0x71, CpuSet.Nmos, "ADC", AddressingMode.IndirectIndexed, OpKind.Read, CpuAction.Adc)]
        public static readonly CpuOpcode AdcIndirectIndexed = new(0x71, "ADC", AddressingMode.IndirectIndexed, OpKind.Read, CpuAction.Adc);
        [Opcode(0x75, CpuSet.Nmos, "ADC", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Adc)]
        public static readonly CpuOpcode AdcZeroPageX = new(0x75, "ADC", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Adc);
        [Opcode(0x79, CpuSet.Nmos, "ADC", AddressingMode.AbsoluteY, OpKind.Read, CpuAction.Adc)]
        public static readonly CpuOpcode AdcAbsoluteY = new(0x79, "ADC", AddressingMode.AbsoluteY, OpKind.Read, CpuAction.Adc);
        [Opcode(0x7D, CpuSet.Nmos, "ADC", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Adc)]
        public static readonly CpuOpcode AdcAbsoluteX = new(0x7D, "ADC", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Adc);
        [Opcode(0xC0, CpuSet.Nmos, "CPY", AddressingMode.Immediate, OpKind.Read, CpuAction.Cpy)]
        public static readonly CpuOpcode CpyImmediate = new(0xC0, "CPY", AddressingMode.Immediate, OpKind.Read, CpuAction.Cpy);
        [Opcode(0xC1, CpuSet.Nmos, "CMP", AddressingMode.IndexedIndirect, OpKind.Read, CpuAction.Cmp)]
        public static readonly CpuOpcode CmpIndexedIndirect = new(0xC1, "CMP", AddressingMode.IndexedIndirect, OpKind.Read, CpuAction.Cmp);
        [Opcode(0xC4, CpuSet.Nmos, "CPY", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Cpy)]
        public static readonly CpuOpcode CpyZeroPage = new(0xC4, "CPY", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Cpy);
        [Opcode(0xC5, CpuSet.Nmos, "CMP", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Cmp)]
        public static readonly CpuOpcode CmpZeroPage = new(0xC5, "CMP", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Cmp);
        [Opcode(0xC9, CpuSet.Nmos, "CMP", AddressingMode.Immediate, OpKind.Read, CpuAction.Cmp)]
        public static readonly CpuOpcode CmpImmediate = new(0xC9, "CMP", AddressingMode.Immediate, OpKind.Read, CpuAction.Cmp);
        [Opcode(0xCC, CpuSet.Nmos, "CPY", AddressingMode.Absolute, OpKind.Read, CpuAction.Cpy)]
        public static readonly CpuOpcode CpyAbsolute = new(0xCC, "CPY", AddressingMode.Absolute, OpKind.Read, CpuAction.Cpy);
        [Opcode(0xCD, CpuSet.Nmos, "CMP", AddressingMode.Absolute, OpKind.Read, CpuAction.Cmp)]
        public static readonly CpuOpcode CmpAbsolute = new(0xCD, "CMP", AddressingMode.Absolute, OpKind.Read, CpuAction.Cmp);
        [Opcode(0xD1, CpuSet.Nmos, "CMP", AddressingMode.IndirectIndexed, OpKind.Read, CpuAction.Cmp)]
        public static readonly CpuOpcode CmpIndirectIndexed = new(0xD1, "CMP", AddressingMode.IndirectIndexed, OpKind.Read, CpuAction.Cmp);
        [Opcode(0xD5, CpuSet.Nmos, "CMP", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Cmp)]
        public static readonly CpuOpcode CmpZeroPageX = new(0xD5, "CMP", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Cmp);
        [Opcode(0xD9, CpuSet.Nmos, "CMP", AddressingMode.AbsoluteY, OpKind.Read, CpuAction.Cmp)]
        public static readonly CpuOpcode CmpAbsoluteY = new(0xD9, "CMP", AddressingMode.AbsoluteY, OpKind.Read, CpuAction.Cmp);
        [Opcode(0xDD, CpuSet.Nmos, "CMP", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Cmp)]
        public static readonly CpuOpcode CmpAbsoluteX = new(0xDD, "CMP", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Cmp);
        [Opcode(0xE0, CpuSet.Nmos, "CPX", AddressingMode.Immediate, OpKind.Read, CpuAction.Cpx)]
        public static readonly CpuOpcode CpxImmediate = new(0xE0, "CPX", AddressingMode.Immediate, OpKind.Read, CpuAction.Cpx);
        [Opcode(0xE1, CpuSet.Nmos, "SBC", AddressingMode.IndexedIndirect, OpKind.Read, CpuAction.Sbc)]
        public static readonly CpuOpcode SbcIndexedIndirect = new(0xE1, "SBC", AddressingMode.IndexedIndirect, OpKind.Read, CpuAction.Sbc);
        [Opcode(0xE4, CpuSet.Nmos, "CPX", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Cpx)]
        public static readonly CpuOpcode CpxZeroPage = new(0xE4, "CPX", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Cpx);
        [Opcode(0xE5, CpuSet.Nmos, "SBC", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Sbc)]
        public static readonly CpuOpcode SbcZeroPage = new(0xE5, "SBC", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Sbc);
        [Opcode(0xEC, CpuSet.Nmos, "CPX", AddressingMode.Absolute, OpKind.Read, CpuAction.Cpx)]
        public static readonly CpuOpcode CpxAbsolute = new(0xEC, "CPX", AddressingMode.Absolute, OpKind.Read, CpuAction.Cpx);
        [Opcode(0xED, CpuSet.Nmos, "SBC", AddressingMode.Absolute, OpKind.Read, CpuAction.Sbc)]
        public static readonly CpuOpcode SbcAbsolute = new(0xED, "SBC", AddressingMode.Absolute, OpKind.Read, CpuAction.Sbc);
        [Opcode(0xF1, CpuSet.Nmos, "SBC", AddressingMode.IndirectIndexed, OpKind.Read, CpuAction.Sbc)]
        public static readonly CpuOpcode SbcIndirectIndexed = new(0xF1, "SBC", AddressingMode.IndirectIndexed, OpKind.Read, CpuAction.Sbc);
        [Opcode(0xF5, CpuSet.Nmos, "SBC", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Sbc)]
        public static readonly CpuOpcode SbcZeroPageX = new(0xF5, "SBC", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Sbc);
        [Opcode(0xF9, CpuSet.Nmos, "SBC", AddressingMode.AbsoluteY, OpKind.Read, CpuAction.Sbc)]
        public static readonly CpuOpcode SbcAbsoluteY = new(0xF9, "SBC", AddressingMode.AbsoluteY, OpKind.Read, CpuAction.Sbc);
        [Opcode(0xFD, CpuSet.Nmos, "SBC", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Sbc)]
        public static readonly CpuOpcode SbcAbsoluteX = new(0xFD, "SBC", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Sbc);
    }
}
