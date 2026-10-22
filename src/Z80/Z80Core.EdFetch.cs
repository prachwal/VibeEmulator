namespace VibeEmulator.Z80;

public sealed partial class Z80Core
{
    private int GetEdInstructionTStates(byte opcode)
    {
        if ((opcode & 0xC7) is 0x40 or 0x41)
        {
            return 12;
        }

        if ((opcode & 0xCF) is 0x42 or 0x4A)
        {
            return 15;
        }

        if ((opcode & 0xCF) is 0x43 or 0x4B)
        {
            return 20;
        }

        if ((opcode & 0xC7) == 0x45)
        {
            return 14;
        }

        if (opcode is 0x47 or 0x4F or 0x57 or 0x5F)
        {
            return 9;
        }

        if (opcode is 0x67 or 0x6F)
        {
            return 18;
        }

        if (IsEdBlockInstruction(opcode))
        {
            if ((opcode & 0x10) == 0)
            {
                return 16;
            }

            if ((opcode & 3) <= 1)
            {
                return PairBC() == 1 ? 16 : 21;
            }

            return _state.B == 1 ? 16 : 21;
        }

        return 8;
    }

    private void PrepareEdCycle()
    {
        byte opcode = _edOpcode;
        if ((opcode & 0xC7) == 0x40)
        {
            StartPortRead(PairBC());
        }
        else if ((opcode & 0xC7) == 0x41)
        {
            StartPortWrite(PairBC(), GetRegister((byte)((opcode >> 3) & 7)));
        }
        else if ((opcode & 0xCF) is 0x42 or 0x4A)
        {
            StartInternal(_refreshAddress, 7, InternalPurpose.Finish);
        }
        else if ((opcode & 0xCF) is 0x43 or 0x4B)
        {
            StartRead(_state.PC, ReadPurpose.EdAbsoluteLow);
        }
        else if ((opcode & 0xC7) == 0x45)
        {
            StartRead(_state.SP, ReadPurpose.EdStackLow);
        }
        else if (opcode is 0x47 or 0x4F or 0x57 or 0x5F)
        {
            StartInternal(_refreshAddress, 1, InternalPurpose.Finish);
        }
        else if (opcode is 0x67 or 0x6F)
        {
            StartRead(HlAddress(), ReadPurpose.EdNibble);
        }
        else if (IsEdBlockInstruction(opcode))
        {
            PrepareEdBlockCycle();
        }
    }

    private static bool IsEdBlockInstruction(byte opcode) =>
        opcode is 0xA0 or 0xA1 or 0xA2 or 0xA3 or 0xA8 or 0xA9 or 0xAA or 0xAB
            or 0xB0 or 0xB1 or 0xB2 or 0xB3 or 0xB8 or 0xB9 or 0xBA or 0xBB;
}
