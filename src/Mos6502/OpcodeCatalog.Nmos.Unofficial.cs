namespace VibeEmulator.Mos6502;

public static partial class OpcodeCatalog
{
    /// <summary>
    /// Etap J: nieoficjalne opkody NMOS (93, klasa <c>unofficial</c>). Rodziny łączą istniejące akcje
    /// (SLO = ASL+ORA, RLA = ROL+AND, SRE = LSR+EOR, RRA = ROR+ADC, DCP = DEC+CMP, ISC = INC+SBC);
    /// rdzeń składa akcje, nie kopiuje kodu.
    /// <para>
    /// <b>Niestabilne</b> (wynik zależy od układu): <c>LAX #imm</c> ($AB — maska <c>0xEE | (A &amp; 0x11)</c>),
    /// <c>XAA</c> ($8B — <c>(A | 0xEE) &amp; X &amp; imm</c>), <c>SBC $EB</c> (zwykłe SBC),
    /// <c>AHX/SHX/SHY/TAS</c> (adres i wartość zapisu maskowane przez <c>high+1</c>).
    /// Zachowanie z wektorów SingleStepTests (commit <c>2f6980a2</c>), nie z „No More Secrets” (nieotwarte);
    /// opis decyzji: <c>docs/cpu/opcodes.md</c>.
    /// </para>
    /// <para>KIL (<c>02 12 22 …</c>) NIE należą tutaj — etap K.</para>
    /// </summary>
    public static partial class Nmos
    {
        [Opcode(0x03, CpuSet.Nmos, "SLO", AddressingMode.IndexedIndirect, OpKind.ReadModifyWrite, CpuAction.Slo)]
        public static readonly CpuOpcode SloIndexedIndirect03 = new(0x03, "SLO", AddressingMode.IndexedIndirect, OpKind.ReadModifyWrite, CpuAction.Slo);

        [Opcode(0x04, CpuSet.Nmos, "NOP", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode NopZeroPage04 = new(0x04, "NOP", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Nop);

        [Opcode(0x07, CpuSet.Nmos, "SLO", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Slo)]
        public static readonly CpuOpcode SloZeroPage07 = new(0x07, "SLO", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Slo);

        [Opcode(0x0B, CpuSet.Nmos, "ANC", AddressingMode.Immediate, OpKind.Read, CpuAction.Anc)]
        public static readonly CpuOpcode AncImmediate0B = new(0x0B, "ANC", AddressingMode.Immediate, OpKind.Read, CpuAction.Anc);

        [Opcode(0x0C, CpuSet.Nmos, "NOP", AddressingMode.Absolute, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode NopAbsolute0C = new(0x0C, "NOP", AddressingMode.Absolute, OpKind.Read, CpuAction.Nop);

        [Opcode(0x0F, CpuSet.Nmos, "SLO", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Slo)]
        public static readonly CpuOpcode SloAbsolute0F = new(0x0F, "SLO", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Slo);

        [Opcode(0x13, CpuSet.Nmos, "SLO", AddressingMode.IndirectIndexed, OpKind.ReadModifyWrite, CpuAction.Slo)]
        public static readonly CpuOpcode SloIndirectIndexed13 = new(0x13, "SLO", AddressingMode.IndirectIndexed, OpKind.ReadModifyWrite, CpuAction.Slo);

        [Opcode(0x14, CpuSet.Nmos, "NOP", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode NopZeroPageX14 = new(0x14, "NOP", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Nop);

        [Opcode(0x17, CpuSet.Nmos, "SLO", AddressingMode.ZeroPageX, OpKind.ReadModifyWrite, CpuAction.Slo)]
        public static readonly CpuOpcode SloZeroPageX17 = new(0x17, "SLO", AddressingMode.ZeroPageX, OpKind.ReadModifyWrite, CpuAction.Slo);

        [Opcode(0x1A, CpuSet.Nmos, "NOP", AddressingMode.Implied, OpKind.Implied, CpuAction.Nop, 1)]
        public static readonly CpuOpcode NopImplied1A = new(0x1A, "NOP", AddressingMode.Implied, OpKind.Implied, CpuAction.Nop, 1);

        [Opcode(0x1B, CpuSet.Nmos, "SLO", AddressingMode.AbsoluteY, OpKind.ReadModifyWrite, CpuAction.Slo)]
        public static readonly CpuOpcode SloAbsoluteY1B = new(0x1B, "SLO", AddressingMode.AbsoluteY, OpKind.ReadModifyWrite, CpuAction.Slo);

        [Opcode(0x1C, CpuSet.Nmos, "NOP", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode NopAbsoluteX1C = new(0x1C, "NOP", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Nop);

        [Opcode(0x1F, CpuSet.Nmos, "SLO", AddressingMode.AbsoluteX, OpKind.ReadModifyWrite, CpuAction.Slo)]
        public static readonly CpuOpcode SloAbsoluteX1F = new(0x1F, "SLO", AddressingMode.AbsoluteX, OpKind.ReadModifyWrite, CpuAction.Slo);

        [Opcode(0x23, CpuSet.Nmos, "RLA", AddressingMode.IndexedIndirect, OpKind.ReadModifyWrite, CpuAction.Rla)]
        public static readonly CpuOpcode RlaIndexedIndirect23 = new(0x23, "RLA", AddressingMode.IndexedIndirect, OpKind.ReadModifyWrite, CpuAction.Rla);

        [Opcode(0x27, CpuSet.Nmos, "RLA", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Rla)]
        public static readonly CpuOpcode RlaZeroPage27 = new(0x27, "RLA", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Rla);

        [Opcode(0x2B, CpuSet.Nmos, "ANC", AddressingMode.Immediate, OpKind.Read, CpuAction.Anc)]
        public static readonly CpuOpcode AncImmediate2B = new(0x2B, "ANC", AddressingMode.Immediate, OpKind.Read, CpuAction.Anc);

        [Opcode(0x2F, CpuSet.Nmos, "RLA", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Rla)]
        public static readonly CpuOpcode RlaAbsolute2F = new(0x2F, "RLA", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Rla);

        [Opcode(0x33, CpuSet.Nmos, "RLA", AddressingMode.IndirectIndexed, OpKind.ReadModifyWrite, CpuAction.Rla)]
        public static readonly CpuOpcode RlaIndirectIndexed33 = new(0x33, "RLA", AddressingMode.IndirectIndexed, OpKind.ReadModifyWrite, CpuAction.Rla);

        [Opcode(0x34, CpuSet.Nmos, "NOP", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode NopZeroPageX34 = new(0x34, "NOP", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Nop);

        [Opcode(0x37, CpuSet.Nmos, "RLA", AddressingMode.ZeroPageX, OpKind.ReadModifyWrite, CpuAction.Rla)]
        public static readonly CpuOpcode RlaZeroPageX37 = new(0x37, "RLA", AddressingMode.ZeroPageX, OpKind.ReadModifyWrite, CpuAction.Rla);

        [Opcode(0x3A, CpuSet.Nmos, "NOP", AddressingMode.Implied, OpKind.Implied, CpuAction.Nop, 1)]
        public static readonly CpuOpcode NopImplied3A = new(0x3A, "NOP", AddressingMode.Implied, OpKind.Implied, CpuAction.Nop, 1);

        [Opcode(0x3B, CpuSet.Nmos, "RLA", AddressingMode.AbsoluteY, OpKind.ReadModifyWrite, CpuAction.Rla)]
        public static readonly CpuOpcode RlaAbsoluteY3B = new(0x3B, "RLA", AddressingMode.AbsoluteY, OpKind.ReadModifyWrite, CpuAction.Rla);

        [Opcode(0x3C, CpuSet.Nmos, "NOP", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode NopAbsoluteX3C = new(0x3C, "NOP", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Nop);

        [Opcode(0x3F, CpuSet.Nmos, "RLA", AddressingMode.AbsoluteX, OpKind.ReadModifyWrite, CpuAction.Rla)]
        public static readonly CpuOpcode RlaAbsoluteX3F = new(0x3F, "RLA", AddressingMode.AbsoluteX, OpKind.ReadModifyWrite, CpuAction.Rla);

        [Opcode(0x43, CpuSet.Nmos, "SRE", AddressingMode.IndexedIndirect, OpKind.ReadModifyWrite, CpuAction.Sre)]
        public static readonly CpuOpcode SreIndexedIndirect43 = new(0x43, "SRE", AddressingMode.IndexedIndirect, OpKind.ReadModifyWrite, CpuAction.Sre);

        [Opcode(0x44, CpuSet.Nmos, "NOP", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode NopZeroPage44 = new(0x44, "NOP", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Nop);

        [Opcode(0x47, CpuSet.Nmos, "SRE", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Sre)]
        public static readonly CpuOpcode SreZeroPage47 = new(0x47, "SRE", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Sre);

        [Opcode(0x4B, CpuSet.Nmos, "ALR", AddressingMode.Immediate, OpKind.Read, CpuAction.Alr)]
        public static readonly CpuOpcode AlrImmediate4B = new(0x4B, "ALR", AddressingMode.Immediate, OpKind.Read, CpuAction.Alr);

        [Opcode(0x4F, CpuSet.Nmos, "SRE", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Sre)]
        public static readonly CpuOpcode SreAbsolute4F = new(0x4F, "SRE", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Sre);

        [Opcode(0x53, CpuSet.Nmos, "SRE", AddressingMode.IndirectIndexed, OpKind.ReadModifyWrite, CpuAction.Sre)]
        public static readonly CpuOpcode SreIndirectIndexed53 = new(0x53, "SRE", AddressingMode.IndirectIndexed, OpKind.ReadModifyWrite, CpuAction.Sre);

        [Opcode(0x54, CpuSet.Nmos, "NOP", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode NopZeroPageX54 = new(0x54, "NOP", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Nop);

        [Opcode(0x57, CpuSet.Nmos, "SRE", AddressingMode.ZeroPageX, OpKind.ReadModifyWrite, CpuAction.Sre)]
        public static readonly CpuOpcode SreZeroPageX57 = new(0x57, "SRE", AddressingMode.ZeroPageX, OpKind.ReadModifyWrite, CpuAction.Sre);

        [Opcode(0x5A, CpuSet.Nmos, "NOP", AddressingMode.Implied, OpKind.Implied, CpuAction.Nop, 1)]
        public static readonly CpuOpcode NopImplied5A = new(0x5A, "NOP", AddressingMode.Implied, OpKind.Implied, CpuAction.Nop, 1);

        [Opcode(0x5B, CpuSet.Nmos, "SRE", AddressingMode.AbsoluteY, OpKind.ReadModifyWrite, CpuAction.Sre)]
        public static readonly CpuOpcode SreAbsoluteY5B = new(0x5B, "SRE", AddressingMode.AbsoluteY, OpKind.ReadModifyWrite, CpuAction.Sre);

        [Opcode(0x5C, CpuSet.Nmos, "NOP", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode NopAbsoluteX5C = new(0x5C, "NOP", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Nop);

        [Opcode(0x5F, CpuSet.Nmos, "SRE", AddressingMode.AbsoluteX, OpKind.ReadModifyWrite, CpuAction.Sre)]
        public static readonly CpuOpcode SreAbsoluteX5F = new(0x5F, "SRE", AddressingMode.AbsoluteX, OpKind.ReadModifyWrite, CpuAction.Sre);

        [Opcode(0x63, CpuSet.Nmos, "RRA", AddressingMode.IndexedIndirect, OpKind.ReadModifyWrite, CpuAction.Rra)]
        public static readonly CpuOpcode RraIndexedIndirect63 = new(0x63, "RRA", AddressingMode.IndexedIndirect, OpKind.ReadModifyWrite, CpuAction.Rra);

        [Opcode(0x64, CpuSet.Nmos, "NOP", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode NopZeroPage64 = new(0x64, "NOP", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Nop);

        [Opcode(0x67, CpuSet.Nmos, "RRA", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Rra)]
        public static readonly CpuOpcode RraZeroPage67 = new(0x67, "RRA", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Rra);

        [Opcode(0x6B, CpuSet.Nmos, "ARR", AddressingMode.Immediate, OpKind.Read, CpuAction.Arr)]
        public static readonly CpuOpcode ArrImmediate6B = new(0x6B, "ARR", AddressingMode.Immediate, OpKind.Read, CpuAction.Arr);

        [Opcode(0x6F, CpuSet.Nmos, "RRA", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Rra)]
        public static readonly CpuOpcode RraAbsolute6F = new(0x6F, "RRA", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Rra);

        [Opcode(0x73, CpuSet.Nmos, "RRA", AddressingMode.IndirectIndexed, OpKind.ReadModifyWrite, CpuAction.Rra)]
        public static readonly CpuOpcode RraIndirectIndexed73 = new(0x73, "RRA", AddressingMode.IndirectIndexed, OpKind.ReadModifyWrite, CpuAction.Rra);

        [Opcode(0x74, CpuSet.Nmos, "NOP", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode NopZeroPageX74 = new(0x74, "NOP", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Nop);

        [Opcode(0x77, CpuSet.Nmos, "RRA", AddressingMode.ZeroPageX, OpKind.ReadModifyWrite, CpuAction.Rra)]
        public static readonly CpuOpcode RraZeroPageX77 = new(0x77, "RRA", AddressingMode.ZeroPageX, OpKind.ReadModifyWrite, CpuAction.Rra);

        [Opcode(0x7A, CpuSet.Nmos, "NOP", AddressingMode.Implied, OpKind.Implied, CpuAction.Nop, 1)]
        public static readonly CpuOpcode NopImplied7A = new(0x7A, "NOP", AddressingMode.Implied, OpKind.Implied, CpuAction.Nop, 1);

        [Opcode(0x7B, CpuSet.Nmos, "RRA", AddressingMode.AbsoluteY, OpKind.ReadModifyWrite, CpuAction.Rra)]
        public static readonly CpuOpcode RraAbsoluteY7B = new(0x7B, "RRA", AddressingMode.AbsoluteY, OpKind.ReadModifyWrite, CpuAction.Rra);

        [Opcode(0x7C, CpuSet.Nmos, "NOP", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode NopAbsoluteX7C = new(0x7C, "NOP", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Nop);

        [Opcode(0x7F, CpuSet.Nmos, "RRA", AddressingMode.AbsoluteX, OpKind.ReadModifyWrite, CpuAction.Rra)]
        public static readonly CpuOpcode RraAbsoluteX7F = new(0x7F, "RRA", AddressingMode.AbsoluteX, OpKind.ReadModifyWrite, CpuAction.Rra);

        [Opcode(0x80, CpuSet.Nmos, "NOP", AddressingMode.Immediate, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode NopImmediate80 = new(0x80, "NOP", AddressingMode.Immediate, OpKind.Read, CpuAction.Nop);

        [Opcode(0x82, CpuSet.Nmos, "NOP", AddressingMode.Immediate, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode NopImmediate82 = new(0x82, "NOP", AddressingMode.Immediate, OpKind.Read, CpuAction.Nop);

        [Opcode(0x83, CpuSet.Nmos, "SAX", AddressingMode.IndexedIndirect, OpKind.Write, CpuAction.Sax)]
        public static readonly CpuOpcode SaxIndexedIndirect83 = new(0x83, "SAX", AddressingMode.IndexedIndirect, OpKind.Write, CpuAction.Sax);

        [Opcode(0x87, CpuSet.Nmos, "SAX", AddressingMode.ZeroPage, OpKind.Write, CpuAction.Sax)]
        public static readonly CpuOpcode SaxZeroPage87 = new(0x87, "SAX", AddressingMode.ZeroPage, OpKind.Write, CpuAction.Sax);

        [Opcode(0x89, CpuSet.Nmos, "NOP", AddressingMode.Immediate, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode NopImmediate89 = new(0x89, "NOP", AddressingMode.Immediate, OpKind.Read, CpuAction.Nop);

        [Opcode(0x8B, CpuSet.Nmos, "XAA", AddressingMode.Immediate, OpKind.Read, CpuAction.Xaa)]
        public static readonly CpuOpcode XaaImmediate8B = new(0x8B, "XAA", AddressingMode.Immediate, OpKind.Read, CpuAction.Xaa);

        [Opcode(0x8F, CpuSet.Nmos, "SAX", AddressingMode.Absolute, OpKind.Write, CpuAction.Sax)]
        public static readonly CpuOpcode SaxAbsolute8F = new(0x8F, "SAX", AddressingMode.Absolute, OpKind.Write, CpuAction.Sax);

        [Opcode(0x93, CpuSet.Nmos, "AHX", AddressingMode.IndirectIndexed, OpKind.Write, CpuAction.Ahx)]
        public static readonly CpuOpcode AhxIndirectIndexed93 = new(0x93, "AHX", AddressingMode.IndirectIndexed, OpKind.Write, CpuAction.Ahx);

        [Opcode(0x97, CpuSet.Nmos, "SAX", AddressingMode.ZeroPageY, OpKind.Write, CpuAction.Sax)]
        public static readonly CpuOpcode SaxZeroPageY97 = new(0x97, "SAX", AddressingMode.ZeroPageY, OpKind.Write, CpuAction.Sax);

        [Opcode(0x9B, CpuSet.Nmos, "TAS", AddressingMode.AbsoluteY, OpKind.Write, CpuAction.Tas)]
        public static readonly CpuOpcode TasAbsoluteY9B = new(0x9B, "TAS", AddressingMode.AbsoluteY, OpKind.Write, CpuAction.Tas);

        [Opcode(0x9C, CpuSet.Nmos, "SHY", AddressingMode.AbsoluteX, OpKind.Write, CpuAction.Shy)]
        public static readonly CpuOpcode ShyAbsoluteX9C = new(0x9C, "SHY", AddressingMode.AbsoluteX, OpKind.Write, CpuAction.Shy);

        [Opcode(0x9E, CpuSet.Nmos, "SHX", AddressingMode.AbsoluteY, OpKind.Write, CpuAction.Shx)]
        public static readonly CpuOpcode ShxAbsoluteY9E = new(0x9E, "SHX", AddressingMode.AbsoluteY, OpKind.Write, CpuAction.Shx);

        [Opcode(0x9F, CpuSet.Nmos, "AHX", AddressingMode.AbsoluteY, OpKind.Write, CpuAction.Ahx)]
        public static readonly CpuOpcode AhxAbsoluteY9F = new(0x9F, "AHX", AddressingMode.AbsoluteY, OpKind.Write, CpuAction.Ahx);

        [Opcode(0xA3, CpuSet.Nmos, "LAX", AddressingMode.IndexedIndirect, OpKind.Read, CpuAction.Lax)]
        public static readonly CpuOpcode LaxIndexedIndirectA3 = new(0xA3, "LAX", AddressingMode.IndexedIndirect, OpKind.Read, CpuAction.Lax);

        [Opcode(0xA7, CpuSet.Nmos, "LAX", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Lax)]
        public static readonly CpuOpcode LaxZeroPageA7 = new(0xA7, "LAX", AddressingMode.ZeroPage, OpKind.Read, CpuAction.Lax);

        [Opcode(0xAB, CpuSet.Nmos, "LAX", AddressingMode.Immediate, OpKind.Read, CpuAction.Lax)]
        public static readonly CpuOpcode LaxImmediateAB = new(0xAB, "LAX", AddressingMode.Immediate, OpKind.Read, CpuAction.Lax);

        [Opcode(0xAF, CpuSet.Nmos, "LAX", AddressingMode.Absolute, OpKind.Read, CpuAction.Lax)]
        public static readonly CpuOpcode LaxAbsoluteAF = new(0xAF, "LAX", AddressingMode.Absolute, OpKind.Read, CpuAction.Lax);

        [Opcode(0xB3, CpuSet.Nmos, "LAX", AddressingMode.IndirectIndexed, OpKind.Read, CpuAction.Lax)]
        public static readonly CpuOpcode LaxIndirectIndexedB3 = new(0xB3, "LAX", AddressingMode.IndirectIndexed, OpKind.Read, CpuAction.Lax);

        [Opcode(0xB7, CpuSet.Nmos, "LAX", AddressingMode.ZeroPageY, OpKind.Read, CpuAction.Lax)]
        public static readonly CpuOpcode LaxZeroPageYB7 = new(0xB7, "LAX", AddressingMode.ZeroPageY, OpKind.Read, CpuAction.Lax);

        [Opcode(0xBB, CpuSet.Nmos, "LAS", AddressingMode.AbsoluteY, OpKind.Read, CpuAction.Las)]
        public static readonly CpuOpcode LasAbsoluteYBB = new(0xBB, "LAS", AddressingMode.AbsoluteY, OpKind.Read, CpuAction.Las);

        [Opcode(0xBF, CpuSet.Nmos, "LAX", AddressingMode.AbsoluteY, OpKind.Read, CpuAction.Lax)]
        public static readonly CpuOpcode LaxAbsoluteYBF = new(0xBF, "LAX", AddressingMode.AbsoluteY, OpKind.Read, CpuAction.Lax);

        [Opcode(0xC2, CpuSet.Nmos, "NOP", AddressingMode.Immediate, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode NopImmediateC2 = new(0xC2, "NOP", AddressingMode.Immediate, OpKind.Read, CpuAction.Nop);

        [Opcode(0xC3, CpuSet.Nmos, "DCP", AddressingMode.IndexedIndirect, OpKind.ReadModifyWrite, CpuAction.Dcp)]
        public static readonly CpuOpcode DcpIndexedIndirectC3 = new(0xC3, "DCP", AddressingMode.IndexedIndirect, OpKind.ReadModifyWrite, CpuAction.Dcp);

        [Opcode(0xC7, CpuSet.Nmos, "DCP", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Dcp)]
        public static readonly CpuOpcode DcpZeroPageC7 = new(0xC7, "DCP", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Dcp);

        [Opcode(0xCB, CpuSet.Nmos, "AXS", AddressingMode.Immediate, OpKind.Read, CpuAction.Axs)]
        public static readonly CpuOpcode AxsImmediateCB = new(0xCB, "AXS", AddressingMode.Immediate, OpKind.Read, CpuAction.Axs);

        [Opcode(0xCF, CpuSet.Nmos, "DCP", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Dcp)]
        public static readonly CpuOpcode DcpAbsoluteCF = new(0xCF, "DCP", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Dcp);

        [Opcode(0xD3, CpuSet.Nmos, "DCP", AddressingMode.IndirectIndexed, OpKind.ReadModifyWrite, CpuAction.Dcp)]
        public static readonly CpuOpcode DcpIndirectIndexedD3 = new(0xD3, "DCP", AddressingMode.IndirectIndexed, OpKind.ReadModifyWrite, CpuAction.Dcp);

        [Opcode(0xD4, CpuSet.Nmos, "NOP", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode NopZeroPageXD4 = new(0xD4, "NOP", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Nop);

        [Opcode(0xD7, CpuSet.Nmos, "DCP", AddressingMode.ZeroPageX, OpKind.ReadModifyWrite, CpuAction.Dcp)]
        public static readonly CpuOpcode DcpZeroPageXD7 = new(0xD7, "DCP", AddressingMode.ZeroPageX, OpKind.ReadModifyWrite, CpuAction.Dcp);

        [Opcode(0xDA, CpuSet.Nmos, "NOP", AddressingMode.Implied, OpKind.Implied, CpuAction.Nop, 1)]
        public static readonly CpuOpcode NopImpliedDA = new(0xDA, "NOP", AddressingMode.Implied, OpKind.Implied, CpuAction.Nop, 1);

        [Opcode(0xDB, CpuSet.Nmos, "DCP", AddressingMode.AbsoluteY, OpKind.ReadModifyWrite, CpuAction.Dcp)]
        public static readonly CpuOpcode DcpAbsoluteYDB = new(0xDB, "DCP", AddressingMode.AbsoluteY, OpKind.ReadModifyWrite, CpuAction.Dcp);

        [Opcode(0xDC, CpuSet.Nmos, "NOP", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode NopAbsoluteXDC = new(0xDC, "NOP", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Nop);

        [Opcode(0xDF, CpuSet.Nmos, "DCP", AddressingMode.AbsoluteX, OpKind.ReadModifyWrite, CpuAction.Dcp)]
        public static readonly CpuOpcode DcpAbsoluteXDF = new(0xDF, "DCP", AddressingMode.AbsoluteX, OpKind.ReadModifyWrite, CpuAction.Dcp);

        [Opcode(0xE2, CpuSet.Nmos, "NOP", AddressingMode.Immediate, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode NopImmediateE2 = new(0xE2, "NOP", AddressingMode.Immediate, OpKind.Read, CpuAction.Nop);

        [Opcode(0xE3, CpuSet.Nmos, "ISC", AddressingMode.IndexedIndirect, OpKind.ReadModifyWrite, CpuAction.Isc)]
        public static readonly CpuOpcode IscIndexedIndirectE3 = new(0xE3, "ISC", AddressingMode.IndexedIndirect, OpKind.ReadModifyWrite, CpuAction.Isc);

        [Opcode(0xE7, CpuSet.Nmos, "ISC", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Isc)]
        public static readonly CpuOpcode IscZeroPageE7 = new(0xE7, "ISC", AddressingMode.ZeroPage, OpKind.ReadModifyWrite, CpuAction.Isc);

        [Opcode(0xEB, CpuSet.Nmos, "SBC", AddressingMode.Immediate, OpKind.Read, CpuAction.Sbc)]
        public static readonly CpuOpcode SbcImmediateEB = new(0xEB, "SBC", AddressingMode.Immediate, OpKind.Read, CpuAction.Sbc);

        [Opcode(0xEF, CpuSet.Nmos, "ISC", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Isc)]
        public static readonly CpuOpcode IscAbsoluteEF = new(0xEF, "ISC", AddressingMode.Absolute, OpKind.ReadModifyWrite, CpuAction.Isc);

        [Opcode(0xF3, CpuSet.Nmos, "ISC", AddressingMode.IndirectIndexed, OpKind.ReadModifyWrite, CpuAction.Isc)]
        public static readonly CpuOpcode IscIndirectIndexedF3 = new(0xF3, "ISC", AddressingMode.IndirectIndexed, OpKind.ReadModifyWrite, CpuAction.Isc);

        [Opcode(0xF4, CpuSet.Nmos, "NOP", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode NopZeroPageXF4 = new(0xF4, "NOP", AddressingMode.ZeroPageX, OpKind.Read, CpuAction.Nop);

        [Opcode(0xF7, CpuSet.Nmos, "ISC", AddressingMode.ZeroPageX, OpKind.ReadModifyWrite, CpuAction.Isc)]
        public static readonly CpuOpcode IscZeroPageXF7 = new(0xF7, "ISC", AddressingMode.ZeroPageX, OpKind.ReadModifyWrite, CpuAction.Isc);

        [Opcode(0xFA, CpuSet.Nmos, "NOP", AddressingMode.Implied, OpKind.Implied, CpuAction.Nop, 1)]
        public static readonly CpuOpcode NopImpliedFA = new(0xFA, "NOP", AddressingMode.Implied, OpKind.Implied, CpuAction.Nop, 1);

        [Opcode(0xFB, CpuSet.Nmos, "ISC", AddressingMode.AbsoluteY, OpKind.ReadModifyWrite, CpuAction.Isc)]
        public static readonly CpuOpcode IscAbsoluteYFB = new(0xFB, "ISC", AddressingMode.AbsoluteY, OpKind.ReadModifyWrite, CpuAction.Isc);

        [Opcode(0xFC, CpuSet.Nmos, "NOP", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Nop)]
        public static readonly CpuOpcode NopAbsoluteXFC = new(0xFC, "NOP", AddressingMode.AbsoluteX, OpKind.Read, CpuAction.Nop);

        [Opcode(0xFF, CpuSet.Nmos, "ISC", AddressingMode.AbsoluteX, OpKind.ReadModifyWrite, CpuAction.Isc)]
        public static readonly CpuOpcode IscAbsoluteXFF = new(0xFF, "ISC", AddressingMode.AbsoluteX, OpKind.ReadModifyWrite, CpuAction.Isc);

    }
}
