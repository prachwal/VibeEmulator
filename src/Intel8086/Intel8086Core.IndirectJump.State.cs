namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private bool _indirectJumpTransfer;
    private bool _indirectJumpFar;
    private byte _indirectJumpModRm;
    private int _indirectJumpPrefixLength;
    private int _indirectJumpDisplacementBytes;
    private int _indirectJumpDisplacementStart;
    private int _indirectJumpFlushStart;
    private int _indirectJumpFetch1;
    private int _indirectJumpFetch2;
    private int _indirectJumpFetch3;
    private int _indirectJumpFetch4;
    private int _indirectJumpRead1;
    private int _indirectJumpRead2;
    private int _indirectJumpRead3;
    private int _indirectJumpRead4;
    private uint _indirectJumpPhysicalAddress;
    private ushort _indirectJumpOffset;
    private ushort _indirectJumpFetchOffset;
    private ushort _indirectJumpTargetOffset;
    private ushort _indirectJumpTargetSegment;

    private void ResetIndirectJumpState()
    {
        _indirectJumpTransfer = false;
        _indirectJumpFar = false;
        _indirectJumpModRm = 0;
        _indirectJumpPrefixLength = 0;
        _indirectJumpDisplacementBytes = 0;
        _indirectJumpDisplacementStart = 0;
        _indirectJumpFlushStart = 0;
        _indirectJumpFetch1 = 0;
        _indirectJumpFetch2 = 0;
        _indirectJumpFetch3 = 0;
        _indirectJumpFetch4 = 0;
        _indirectJumpRead1 = 0;
        _indirectJumpRead2 = 0;
        _indirectJumpRead3 = 0;
        _indirectJumpRead4 = 0;
        _indirectJumpPhysicalAddress = 0;
        _indirectJumpOffset = 0;
        _indirectJumpFetchOffset = 0;
        _indirectJumpTargetOffset = 0;
        _indirectJumpTargetSegment = 0;
    }
}
