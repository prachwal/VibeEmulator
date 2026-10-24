namespace VibeEmulator.Intel8086;

public sealed partial class Intel8086Core
{
    private Intel8086BusCycle FetchRepeatedMovsOpcodeCycle()
    {
        PopQueue();
        _state.IP++;
        return FetchCycle("T1", 'F', Opcode);
    }

    private Intel8086BusCycle ConsumeRepeatedMovsPrefixCycle()
    {
        PopQueue();
        _state.IP++;
        return IdleCycle('F', _instructionPrefix);
    }

    private Intel8086BusCycle ConsumeRepeatedMovsOpcodeCycle()
    {
        PopQueue();
        _state.IP++;
        return IdleCycle('F', Opcode);
    }

    private Intel8086BusCycle FetchRepeatedMovsDataCycle()
    {
        Intel8086BusCycle cycle = FetchDataCycle();
        PushQueue(0x90);
        AdvanceStringPrefetchAddress(2);
        return cycle;
    }
}
