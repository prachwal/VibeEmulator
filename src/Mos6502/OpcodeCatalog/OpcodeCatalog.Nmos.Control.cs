namespace VibeEmulator.Mos6502;

public static partial class OpcodeCatalog
{
    /// <summary>
    /// Etap E: sterowanie (4 opkody, domyka poziom A — 151/151 oficjalnych NMOS).
    /// JSR/RTS/RTI to nowy <c>OpKind.Subroutine</c> (6 cykli); JMP (ind.) to <c>OpKind.Jump</c>
    /// z trybem <c>Indirect</c> (5 cykli NMOS z błędem strony, 6 cykli 65C02 bez błędu + dummy).
    /// </summary>
    public static partial class Nmos
    {
        [Opcode(0x20, CpuSet.Nmos, "JSR", AddressingMode.Absolute, OpKind.Subroutine, CpuAction.Jsr)]
        public static readonly CpuOpcode JsrAbsolute = new(0x20, "JSR", AddressingMode.Absolute, OpKind.Subroutine, CpuAction.Jsr);

        [Opcode(0x60, CpuSet.Nmos, "RTS", AddressingMode.Implied, OpKind.Subroutine, CpuAction.Rts)]
        public static readonly CpuOpcode RtsImplied = new(0x60, "RTS", AddressingMode.Implied, OpKind.Subroutine, CpuAction.Rts);

        [Opcode(0x40, CpuSet.Nmos, "RTI", AddressingMode.Implied, OpKind.Subroutine, CpuAction.Rti)]
        public static readonly CpuOpcode RtiImplied = new(0x40, "RTI", AddressingMode.Implied, OpKind.Subroutine, CpuAction.Rti);

        [Opcode(0x6C, CpuSet.Nmos, "JMP", AddressingMode.Indirect, OpKind.Jump, CpuAction.Jmp)]
        public static readonly CpuOpcode JmpIndirect = new(0x6C, "JMP", AddressingMode.Indirect, OpKind.Jump, CpuAction.Jmp);
    }
}
