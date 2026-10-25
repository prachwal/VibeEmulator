namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private bool _indirectCallTransfer;
    private bool _indirectCallFar;
    private byte _indirectCallModRm;
    private int _indirectCallPrefixLength;
    private int _indirectCallDisplacementBytes;
    private int _indirectCallDisplacementStart;
    private int _indirectCallFlushStart;
    private int _indirectCallFetch1;
    private int _indirectCallFetch2;
    private int _indirectCallFetch3;
    private int _indirectCallFetch4;
    private int _indirectCallRead1;
    private int _indirectCallRead2;
    private int _indirectCallRead3;
    private int _indirectCallRead4;
    private int _indirectCallWrite1;
    private int _indirectCallWrite2;
    private int _indirectCallWrite3;
    private int _indirectCallWrite4;
    private uint _indirectCallPhysicalAddress;
    private ushort _indirectCallOffset;
    private ushort _indirectCallFetchOffset;
    private ushort _indirectCallTargetOffset;
    private ushort _indirectCallTargetSegment;

    private void ResetIndirectCallState()
    {
        _indirectCallTransfer = false;
        _indirectCallFar = false;
        _indirectCallModRm = 0;
        _indirectCallPrefixLength = 0;
        _indirectCallDisplacementBytes = 0;
        _indirectCallDisplacementStart = 0;
        _indirectCallFlushStart = 0;
        _indirectCallFetch1 = 0;
        _indirectCallFetch2 = 0;
        _indirectCallFetch3 = 0;
        _indirectCallFetch4 = 0;
        _indirectCallRead1 = 0;
        _indirectCallRead2 = 0;
        _indirectCallRead3 = 0;
        _indirectCallRead4 = 0;
        _indirectCallWrite1 = 0;
        _indirectCallWrite2 = 0;
        _indirectCallWrite3 = 0;
        _indirectCallWrite4 = 0;
        _indirectCallPhysicalAddress = 0;
        _indirectCallOffset = 0;
        _indirectCallFetchOffset = 0;
        _indirectCallTargetOffset = 0;
        _indirectCallTargetSegment = 0;
    }
}
