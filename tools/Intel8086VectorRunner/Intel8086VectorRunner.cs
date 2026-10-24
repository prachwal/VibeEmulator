using System.Text.Json;
using VibeEmulator.Intel8086;
using VibeEmulator.Memory;

namespace VibeEmulator.Intel8086VectorRunner;

public static class Intel8086VectorRunner
{
    public static VectorRunResult Run(
        JsonElement vector,
        int maxTStates = 1024,
        ushort flagsMask = ushort.MaxValue,
        IReadOnlyDictionary<uint, byte>? ramMasks = null,
        IReadOnlyDictionary<int, ushort>? cycleDataMasks = null,
        IPortBus? portBus = null)
    {
        int testNumber = vector.GetProperty("test_num").GetInt32();
        string hash = vector.GetProperty("test_hash").GetString()!;
        JsonElement initial = vector.GetProperty("initial");
        JsonElement initialRegisters = initial.GetProperty("regs");
        var memory = new FlatAddressMemory(0x100000);
        foreach (JsonElement entry in initial.GetProperty("ram").EnumerateArray())
        {
            memory.Write(entry[0].GetUInt32(), entry[1].GetByte());
        }

        Intel8086State state = CreateState(initialRegisters, initial.GetProperty("queue"));
        var cpu = new Intel8086Core(memory, portBus)
        {
            State = state
        };
        var actualCycles = new List<Intel8086BusCycle>();
        int tStates = 0;
        do
        {
            if (tStates >= maxTStates)
            {
                return new VectorRunResult(
                    VectorRunStatus.TimedOut,
                    tStates,
                    $"test_num={testNumber}, hash={hash}: exceeded {maxTStates} T-states.");
            }

            cpu.Tick();
            actualCycles.Add(cpu.LastBusCycle);
            tStates++;
        }
        while (cpu.Pending != 0);

        JsonElement expectedCycles = vector.GetProperty("cycles");
        int comparableCycles = Math.Min(expectedCycles.GetArrayLength(), actualCycles.Count);
        for (int index = 0; index < comparableCycles; index++)
        {
            ushort dataMask = cycleDataMasks is not null && cycleDataMasks.TryGetValue(index, out ushort configuredMask)
                ? configuredMask
                : ushort.MaxValue;
            string? difference = GetCycleDifference(expectedCycles[index], actualCycles[index], dataMask);
            if (difference is not null)
            {
                int firstContextCycle = Math.Max(0, index - 2);
                int contextLength = Math.Min(comparableCycles - firstContextCycle, 11);
                string context = string.Join(" | ", Enumerable.Range(firstContextCycle, contextLength)
                    .Select(cycle => $"{cycle}: expected {expectedCycles[cycle].GetRawText()}, actual {actualCycles[cycle]}"));
                int expectedVectorRead = FindVectorReadCycle(expectedCycles);
                int actualVectorRead = actualCycles.FindIndex(cycle =>
                    cycle.BusStatus == "MEMR" && cycle.TState == "T1" && cycle.MultiplexedBus == 0);
                return Fail(tStates,
                    $"test_num={testNumber}, hash={hash}, cycle={index}: {difference} actualTStates={tStates}, " +
                    $"expectedTStates={expectedCycles.GetArrayLength()}, QueueCount={cpu.State.QueueCount}; " +
                    $"final CS:IP={cpu.State.CS:X4}:{cpu.State.IP:X4}, SP={cpu.State.SP:X4}, AX={cpu.State.AX:X4}; " +
                    $"vector[0] T1 expected={expectedVectorRead}, actual={actualVectorRead}; context: {context}.");
            }
        }

        if (expectedCycles.GetArrayLength() != actualCycles.Count)
        {
            int expectedTailStart = Math.Max(0, expectedCycles.GetArrayLength() - 3);
            int actualTailStart = Math.Max(0, actualCycles.Count - 3);
            string expectedTail = string.Join(" | ", Enumerable.Range(expectedTailStart, expectedCycles.GetArrayLength() - expectedTailStart)
                .Select(index => $"{index}: {expectedCycles[index].GetRawText()}"));
            string actualTail = string.Join(" | ", Enumerable.Range(actualTailStart, actualCycles.Count - actualTailStart)
                .Select(index => $"{index}: {actualCycles[index]}"));
            return Fail(tStates,
                $"test_num={testNumber}, hash={hash}: cycle count expected {expectedCycles.GetArrayLength()}, actual {actualCycles.Count}; " +
                $"expected tail: {expectedTail}; actual tail: {actualTail}.");
        }

        JsonElement final = vector.GetProperty("final");
        JsonElement finalRegisters = final.GetProperty("regs");
        foreach (JsonProperty register in initialRegisters.EnumerateObject())
        {
            ushort actual = GetRegister(cpu.State, register.Name);
            ushort expected = finalRegisters.TryGetProperty(register.Name, out JsonElement finalValue)
                ? finalValue.GetUInt16()
                : register.Value.GetUInt16();
            if (register.Name == "flags" ? ((actual ^ expected) & flagsMask) != 0 : actual != expected)
            {
                return Fail(tStates, $"test_num={testNumber}, hash={hash}: register {register.Name}, expected {expected:X4}, actual {actual:X4}.");
            }
        }

        byte[] expectedQueue = [.. final.GetProperty("queue").EnumerateArray().Select(item => item.GetByte())];
        byte[] actualQueue = cpu.State.PendingQueue.ToArray();
        if (!expectedQueue.SequenceEqual(actualQueue))
        {
            return Fail(tStates, $"test_num={testNumber}, hash={hash}: queue expected [{Convert.ToHexString(expectedQueue)}], actual [{Convert.ToHexString(actualQueue)}].");
        }

        foreach (JsonElement entry in initial.GetProperty("ram").EnumerateArray())
        {
            uint address = entry[0].GetUInt32();
            byte expected = entry[1].GetByte();
            foreach (JsonElement finalEntry in final.GetProperty("ram").EnumerateArray())
            {
                if (finalEntry[0].GetUInt32() == address)
                {
                    expected = finalEntry[1].GetByte();
                    break;
                }
            }

            byte actual = memory.Read(address);
            byte mask = ramMasks is not null && ramMasks.TryGetValue(address, out byte configuredMask)
                ? configuredMask
                : byte.MaxValue;
            if (((actual ^ expected) & mask) != 0)
            {
                return Fail(tStates, $"test_num={testNumber}, hash={hash}: RAM[{address:X5}] expected {expected:X2}, actual {actual:X2}.");
            }
        }

        foreach (JsonElement entry in final.GetProperty("ram").EnumerateArray())
        {
            byte expected = entry[1].GetByte();
            uint address = entry[0].GetUInt32();
            byte actual = memory.Read(address);
            byte mask = ramMasks is not null && ramMasks.TryGetValue(address, out byte configuredMask)
                ? configuredMask
                : byte.MaxValue;
            if (((actual ^ expected) & mask) != 0)
            {
                return Fail(tStates, $"test_num={testNumber}, hash={hash}: RAM[{address:X5}] expected {expected:X2}, actual {actual:X2}.");
            }
        }

        return new VectorRunResult(VectorRunStatus.Passed, tStates, null);
    }

    private static Intel8086State CreateState(JsonElement registers, JsonElement queue)
    {
        var state = Intel8086State.Initial(registers.GetProperty("cs").GetUInt16(), registers.GetProperty("ip").GetUInt16());
        state.AX = registers.GetProperty("ax").GetUInt16();
        state.BX = registers.GetProperty("bx").GetUInt16();
        state.CX = registers.GetProperty("cx").GetUInt16();
        state.DX = registers.GetProperty("dx").GetUInt16();
        state.SS = registers.GetProperty("ss").GetUInt16();
        state.DS = registers.GetProperty("ds").GetUInt16();
        state.ES = registers.GetProperty("es").GetUInt16();
        state.SP = registers.GetProperty("sp").GetUInt16();
        state.BP = registers.GetProperty("bp").GetUInt16();
        state.SI = registers.GetProperty("si").GetUInt16();
        state.DI = registers.GetProperty("di").GetUInt16();
        state.Flags = registers.GetProperty("flags").GetUInt16();
        state.QueueCount = queue.GetArrayLength();
        for (int index = 0; index < state.QueueCount; index++)
        {
            state.Queue[index] = queue[index].GetByte();
        }

        return state;
    }

    private static int FindVectorReadCycle(JsonElement cycles)
    {
        for (int index = 0; index < cycles.GetArrayLength(); index++)
        {
            JsonElement cycle = cycles[index];
            if (cycle[1].GetUInt32() == 0 && cycle[7].GetString() == "MEMR" && cycle[8].GetString() == "T1")
            {
                return index;
            }
        }

        return -1;
    }

    private static ushort GetRegister(Intel8086State state, string name) => name switch
    {
        "ax" => state.AX,
        "bx" => state.BX,
        "cx" => state.CX,
        "dx" => state.DX,
        "cs" => state.CS,
        "ss" => state.SS,
        "ds" => state.DS,
        "es" => state.ES,
        "sp" => state.SP,
        "bp" => state.BP,
        "si" => state.SI,
        "di" => state.DI,
        "ip" => state.IP,
        "flags" => state.Flags,
        _ => throw new InvalidOperationException($"Unknown register '{name}'."),
    };

    private static string? GetCycleDifference(JsonElement expected, Intel8086BusCycle actual, ushort dataMask)
    {
        if (expected[0].GetByte() != actual.PinBits)
        {
            return $"pin bits expected {expected[0].GetByte()}, actual {actual.PinBits} ({actual.MemoryStatus}/{actual.BusStatus}/{actual.TState}, queue {actual.QueueStatus}:{actual.QueueData:X2}).";
        }

        if (actual.PinBits != 0 && expected[1].GetUInt32() != actual.MultiplexedBus)
        {
            return $"bus expected {expected[1].GetUInt32():X5}, actual {actual.MultiplexedBus:X5}.";
        }

        if (expected[2].GetString() != actual.SegmentStatus || expected[3].GetString() != actual.MemoryStatus || expected[4].GetString() != actual.IoStatus)
        {
            return $"segment/memory/I/O status expected {expected[2].GetString()}/{expected[3].GetString()}/{expected[4].GetString()}, actual {actual.SegmentStatus}/{actual.MemoryStatus}/{actual.IoStatus} (bus {expected[7].GetString()}/{actual.BusStatus}, T-state {expected[8].GetString()}/{actual.TState}).";
        }

        if (actual.BusStatus != "PASV" && expected[5].GetByte() != actual.Bhe)
        {
            return $"BHE expected {expected[5].GetByte()}, actual {actual.Bhe}.";
        }

        if (((expected[6].GetUInt16() ^ actual.Data) & dataMask) != 0 || expected[7].GetString() != actual.BusStatus || expected[8].GetString() != actual.TState)
        {
            return $"data bus, bus status or T-state differs (data {expected[6].GetUInt16():X4}/{actual.Data:X4} mask {dataMask:X4}, bus {expected[7].GetString()}/{actual.BusStatus}, T-state {expected[8].GetString()}/{actual.TState}).";
        }

        if (expected[9].GetString()![0] != actual.QueueStatus || expected[10].GetByte() != actual.QueueData)
        {
            return $"queue status expected {expected[9].GetString()}, actual {actual.QueueStatus}; queue byte expected {expected[10].GetByte():X2}, actual {actual.QueueData:X2}.";
        }

        return null;
    }

    private static VectorRunResult Fail(int tStates, string difference) => new(false, tStates, difference);
}
