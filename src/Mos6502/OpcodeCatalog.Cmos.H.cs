namespace VibeEmulator.Mos6502;

public static partial class OpcodeCatalog
{
    /// <summary>
    /// Etap H: nowe opkody 65C02 (55). Tryby: <c>IndirectZeroPage</c> ((zp), 5 cykli, bez dummy),
    /// <c>ZeroPageRelative</c> (zp,rel dla BBR/BBS, 5/6/7), <c>IndirectAbsoluteX</c> ((abs,X) dla JMP, 6 cykli).
    /// Numer bitu RMB/SMB/BBR/BBS jest zaszyty w opkodzie — trzyma go <c>CpuOpcode.BitNumber</c>.
    /// Sekwencje dostępów potwierdzone wektorami wdc65c02.
    /// </summary>
    public static partial class Cmos
    {
        // --- TSB/TRB (zp, abs) — RMW z akumulatorem, Z z (A & M) ---

        [Opcode(0x04, CpuSet.Cmos65C02, "TSB", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Tsb)]
        public static readonly CpuOpcode TSBZeroPage = new(0x04, "TSB", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Tsb);

        [Opcode(0x14, CpuSet.Cmos65C02, "TRB", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Trb)]
        public static readonly CpuOpcode TRBZeroPage = new(0x14, "TRB", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Trb);

        [Opcode(0x0C, CpuSet.Cmos65C02, "TSB", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Tsb)]
        public static readonly CpuOpcode TSBAbsolute = new(0x0C, "TSB", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Tsb);

        [Opcode(0x1C, CpuSet.Cmos65C02, "TRB", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Trb)]
        public static readonly CpuOpcode TRBAbsolute = new(0x1C, "TRB", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Trb);

        // --- RMB0–7 / SMB0–7 (zp) ---

        [Opcode(0x07, CpuSet.Cmos65C02, "RMB0", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Rmb, 0, 0)]
        public static readonly CpuOpcode Rmb0ZeroPage = new(0x07, "RMB0", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Rmb, 0, 0);

        [Opcode(0x17, CpuSet.Cmos65C02, "RMB1", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Rmb, 0, 1)]
        public static readonly CpuOpcode Rmb1ZeroPage = new(0x17, "RMB1", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Rmb, 0, 1);

        [Opcode(0x27, CpuSet.Cmos65C02, "RMB2", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Rmb, 0, 2)]
        public static readonly CpuOpcode Rmb2ZeroPage = new(0x27, "RMB2", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Rmb, 0, 2);

        [Opcode(0x37, CpuSet.Cmos65C02, "RMB3", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Rmb, 0, 3)]
        public static readonly CpuOpcode Rmb3ZeroPage = new(0x37, "RMB3", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Rmb, 0, 3);

        [Opcode(0x47, CpuSet.Cmos65C02, "RMB4", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Rmb, 0, 4)]
        public static readonly CpuOpcode Rmb4ZeroPage = new(0x47, "RMB4", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Rmb, 0, 4);

        [Opcode(0x57, CpuSet.Cmos65C02, "RMB5", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Rmb, 0, 5)]
        public static readonly CpuOpcode Rmb5ZeroPage = new(0x57, "RMB5", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Rmb, 0, 5);

        [Opcode(0x67, CpuSet.Cmos65C02, "RMB6", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Rmb, 0, 6)]
        public static readonly CpuOpcode Rmb6ZeroPage = new(0x67, "RMB6", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Rmb, 0, 6);

        [Opcode(0x77, CpuSet.Cmos65C02, "RMB7", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Rmb, 0, 7)]
        public static readonly CpuOpcode Rmb7ZeroPage = new(0x77, "RMB7", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Rmb, 0, 7);

        [Opcode(0x87, CpuSet.Cmos65C02, "SMB0", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Smb, 0, 0)]
        public static readonly CpuOpcode Smb0ZeroPage = new(0x87, "SMB0", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Smb, 0, 0);

        [Opcode(0x97, CpuSet.Cmos65C02, "SMB1", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Smb, 0, 1)]
        public static readonly CpuOpcode Smb1ZeroPage = new(0x97, "SMB1", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Smb, 0, 1);

        [Opcode(0xA7, CpuSet.Cmos65C02, "SMB2", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Smb, 0, 2)]
        public static readonly CpuOpcode Smb2ZeroPage = new(0xA7, "SMB2", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Smb, 0, 2);

        [Opcode(0xB7, CpuSet.Cmos65C02, "SMB3", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Smb, 0, 3)]
        public static readonly CpuOpcode Smb3ZeroPage = new(0xB7, "SMB3", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Smb, 0, 3);

        [Opcode(0xC7, CpuSet.Cmos65C02, "SMB4", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Smb, 0, 4)]
        public static readonly CpuOpcode Smb4ZeroPage = new(0xC7, "SMB4", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Smb, 0, 4);

        [Opcode(0xD7, CpuSet.Cmos65C02, "SMB5", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Smb, 0, 5)]
        public static readonly CpuOpcode Smb5ZeroPage = new(0xD7, "SMB5", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Smb, 0, 5);

        [Opcode(0xE7, CpuSet.Cmos65C02, "SMB6", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Smb, 0, 6)]
        public static readonly CpuOpcode Smb6ZeroPage = new(0xE7, "SMB6", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Smb, 0, 6);

        [Opcode(0xF7, CpuSet.Cmos65C02, "SMB7", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Smb, 0, 7)]
        public static readonly CpuOpcode Smb7ZeroPage = new(0xF7, "SMB7", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Smb, 0, 7);

        // --- (zp) bez indeksu ---

        [Opcode(0x12, CpuSet.Cmos65C02, "ORA", AddressingMode.IndirectZeroPage, OpKind.Read, CpuAction.Ora)]
        public static readonly CpuOpcode OraIndirectZeroPage = new(0x12, "ORA", AddressingMode.IndirectZeroPage, OpKind.Read, CpuAction.Ora);

        [Opcode(0x32, CpuSet.Cmos65C02, "AND", AddressingMode.IndirectZeroPage, OpKind.Read, CpuAction.And)]
        public static readonly CpuOpcode AndIndirectZeroPage = new(0x32, "AND", AddressingMode.IndirectZeroPage, OpKind.Read, CpuAction.And);

        [Opcode(0x52, CpuSet.Cmos65C02, "EOR", AddressingMode.IndirectZeroPage, OpKind.Read, CpuAction.Eor)]
        public static readonly CpuOpcode EorIndirectZeroPage = new(0x52, "EOR", AddressingMode.IndirectZeroPage, OpKind.Read, CpuAction.Eor);

        [Opcode(0x72, CpuSet.Cmos65C02, "ADC", AddressingMode.IndirectZeroPage, OpKind.Read, CpuAction.Adc)]
        public static readonly CpuOpcode AdcIndirectZeroPage = new(0x72, "ADC", AddressingMode.IndirectZeroPage, OpKind.Read, CpuAction.Adc);

        [Opcode(0x92, CpuSet.Cmos65C02, "STA", AddressingMode.IndirectZeroPage, OpKind.Write, CpuAction.Sta)]
        public static readonly CpuOpcode StaIndirectZeroPage = new(0x92, "STA", AddressingMode.IndirectZeroPage, OpKind.Write, CpuAction.Sta);

        [Opcode(0xB2, CpuSet.Cmos65C02, "LDA", AddressingMode.IndirectZeroPage, OpKind.Read, CpuAction.Lda)]
        public static readonly CpuOpcode LdaIndirectZeroPage = new(0xB2, "LDA", AddressingMode.IndirectZeroPage, OpKind.Read, CpuAction.Lda);

        [Opcode(0xD2, CpuSet.Cmos65C02, "CMP", AddressingMode.IndirectZeroPage, OpKind.Read, CpuAction.Cmp)]
        public static readonly CpuOpcode CmpIndirectZeroPage = new(0xD2, "CMP", AddressingMode.IndirectZeroPage, OpKind.Read, CpuAction.Cmp);

        [Opcode(0xF2, CpuSet.Cmos65C02, "SBC", AddressingMode.IndirectZeroPage, OpKind.Read, CpuAction.Sbc)]
        public static readonly CpuOpcode SbcIndirectZeroPage = new(0xF2, "SBC", AddressingMode.IndirectZeroPage, OpKind.Read, CpuAction.Sbc);

        // --- BIT w zpx/abx/imm ---

        [Opcode(0x34, CpuSet.Cmos65C02, "BIT", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Bit)]
        public static readonly CpuOpcode BitZeroPageX = new(0x34, "BIT", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Bit);

        [Opcode(0x3C, CpuSet.Cmos65C02, "BIT", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Bit)]
        public static readonly CpuOpcode BitAbsoluteX = new(0x3C, "BIT", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Bit);

        [Opcode(0x89, CpuSet.Cmos65C02, "BIT", AddressingMode.Immediate, OpKind.Read, CpuAction.Bit)]
        public static readonly CpuOpcode BitImmediate = new(0x89, "BIT", AddressingMode.Immediate, OpKind.Read, CpuAction.Bit);

        // --- JMP (abs,X) ---

        [Opcode(0x7C, CpuSet.Cmos65C02, "JMP", AddressingMode.IndirectAbsoluteX, OpKind.Jump, CpuAction.Jmp)]
        public static readonly CpuOpcode JmpIndirectAbsoluteX = new(0x7C, "JMP", AddressingMode.IndirectAbsoluteX, OpKind.Jump, CpuAction.Jmp);

        // --- BRA ---

        [Opcode(0x80, CpuSet.Cmos65C02, "BRA", AddressingMode.Relative, OpKind.Branch, CpuAction.Bra)]
        public static readonly CpuOpcode BraRelative = new(0x80, "BRA", AddressingMode.Relative, OpKind.Branch, CpuAction.Bra);

        // --- BBR0–7 / BBS0–7 (zp,rel) ---

        [Opcode(0x0F, CpuSet.Cmos65C02, "BBR0", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbr, 0, 0)]
        public static readonly CpuOpcode Bbr0ZeroPageRelative = new(0x0F, "BBR0", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbr, 0, 0);

        [Opcode(0x1F, CpuSet.Cmos65C02, "BBR1", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbr, 0, 1)]
        public static readonly CpuOpcode Bbr1ZeroPageRelative = new(0x1F, "BBR1", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbr, 0, 1);

        [Opcode(0x2F, CpuSet.Cmos65C02, "BBR2", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbr, 0, 2)]
        public static readonly CpuOpcode Bbr2ZeroPageRelative = new(0x2F, "BBR2", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbr, 0, 2);

        [Opcode(0x3F, CpuSet.Cmos65C02, "BBR3", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbr, 0, 3)]
        public static readonly CpuOpcode Bbr3ZeroPageRelative = new(0x3F, "BBR3", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbr, 0, 3);

        [Opcode(0x4F, CpuSet.Cmos65C02, "BBR4", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbr, 0, 4)]
        public static readonly CpuOpcode Bbr4ZeroPageRelative = new(0x4F, "BBR4", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbr, 0, 4);

        [Opcode(0x5F, CpuSet.Cmos65C02, "BBR5", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbr, 0, 5)]
        public static readonly CpuOpcode Bbr5ZeroPageRelative = new(0x5F, "BBR5", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbr, 0, 5);

        [Opcode(0x6F, CpuSet.Cmos65C02, "BBR6", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbr, 0, 6)]
        public static readonly CpuOpcode Bbr6ZeroPageRelative = new(0x6F, "BBR6", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbr, 0, 6);

        [Opcode(0x7F, CpuSet.Cmos65C02, "BBR7", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbr, 0, 7)]
        public static readonly CpuOpcode Bbr7ZeroPageRelative = new(0x7F, "BBR7", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbr, 0, 7);

        [Opcode(0x8F, CpuSet.Cmos65C02, "BBS0", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbs, 0, 0)]
        public static readonly CpuOpcode Bbs0ZeroPageRelative = new(0x8F, "BBS0", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbs, 0, 0);

        [Opcode(0x9F, CpuSet.Cmos65C02, "BBS1", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbs, 0, 1)]
        public static readonly CpuOpcode Bbs1ZeroPageRelative = new(0x9F, "BBS1", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbs, 0, 1);

        [Opcode(0xAF, CpuSet.Cmos65C02, "BBS2", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbs, 0, 2)]
        public static readonly CpuOpcode Bbs2ZeroPageRelative = new(0xAF, "BBS2", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbs, 0, 2);

        [Opcode(0xBF, CpuSet.Cmos65C02, "BBS3", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbs, 0, 3)]
        public static readonly CpuOpcode Bbs3ZeroPageRelative = new(0xBF, "BBS3", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbs, 0, 3);

        [Opcode(0xCF, CpuSet.Cmos65C02, "BBS4", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbs, 0, 4)]
        public static readonly CpuOpcode Bbs4ZeroPageRelative = new(0xCF, "BBS4", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbs, 0, 4);

        [Opcode(0xDF, CpuSet.Cmos65C02, "BBS5", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbs, 0, 5)]
        public static readonly CpuOpcode Bbs5ZeroPageRelative = new(0xDF, "BBS5", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbs, 0, 5);

        [Opcode(0xEF, CpuSet.Cmos65C02, "BBS6", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbs, 0, 6)]
        public static readonly CpuOpcode Bbs6ZeroPageRelative = new(0xEF, "BBS6", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbs, 0, 6);

        [Opcode(0xFF, CpuSet.Cmos65C02, "BBS7", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbs, 0, 7)]
        public static readonly CpuOpcode Bbs7ZeroPageRelative = new(0xFF, "BBS7", AddressingMode.ZeroPageRelative, OpKind.BitBranch, CpuAction.Bbs, 0, 7);

        // --- PHX/PHY/PLX/PLY (fazy stosu z etapu D) ---

        [Opcode(0xDA, CpuSet.Cmos65C02, "PHX", AddressingMode.Implied, OpKind.Push, CpuAction.Phx)]
        public static readonly CpuOpcode PhxImplied = new(0xDA, "PHX", AddressingMode.Implied, OpKind.Push, CpuAction.Phx);

        [Opcode(0x5A, CpuSet.Cmos65C02, "PHY", AddressingMode.Implied, OpKind.Push, CpuAction.Phy)]
        public static readonly CpuOpcode PhyImplied = new(0x5A, "PHY", AddressingMode.Implied, OpKind.Push, CpuAction.Phy);

        [Opcode(0xFA, CpuSet.Cmos65C02, "PLX", AddressingMode.Implied, OpKind.Pull, CpuAction.Plx)]
        public static readonly CpuOpcode PlxImplied = new(0xFA, "PLX", AddressingMode.Implied, OpKind.Pull, CpuAction.Plx);

        [Opcode(0x7A, CpuSet.Cmos65C02, "PLY", AddressingMode.Implied, OpKind.Pull, CpuAction.Ply)]
        public static readonly CpuOpcode PlyImplied = new(0x7A, "PLY", AddressingMode.Implied, OpKind.Pull, CpuAction.Ply);

        // --- WAI / STP (bez wektorów — pliki cb.json/db.json puste w źródle; CSV: 3 cykle) ---

        [Opcode(0xCB, CpuSet.Cmos65C02, "WAI", AddressingMode.Implied, OpKind.Implied, CpuAction.Wai, 2)]
        public static readonly CpuOpcode WaiImplied = new(0xCB, "WAI", AddressingMode.Implied, OpKind.Implied, CpuAction.Wai, 2);

        [Opcode(0xDB, CpuSet.Cmos65C02, "STP", AddressingMode.Implied, OpKind.Implied, CpuAction.Stp, 2)]
        public static readonly CpuOpcode StpImplied = new(0xDB, "STP", AddressingMode.Implied, OpKind.Implied, CpuAction.Stp, 2);
    }
}
