using System.Text.Json;
using VibeEmulator.Intel8086;
using VibeEmulator.Memory;

namespace VibeEmulator.Intel8086VectorRunner;

public static class Intel8086VectorRunner
{
    public static VectorRunResult Run(JsonElement vector, int maxTStates = 256)
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
        var cpu = new Intel8086Core(memory)
        {
            State = state
        };
        var actualCycles = new List<Intel8086BusCycle>();
        int tStates = 0;
        do
        {
            if (tStates >= maxTStates)
            {
                return Fail(tStates, $"test_num={testNumber}, hash={hash}: exceeded {maxTStates} T-states.");
            }

            cpu.Tick();
            actualCycles.Add(cpu.LastBusCycle);
            tStates++;
        }
        while (cpu.Pending != 0);

        JsonElement final = vector.GetProperty("final");
        JsonElement finalRegisters = final.GetProperty("regs");
        foreach (JsonProperty register in initialRegisters.EnumerateObject())
        {
            ushort actual = GetRegister(cpu.State, register.Name);
            ushort expected = finalRegisters.TryGetProperty(register.Name, out JsonElement finalValue)
                ? finalValue.GetUInt16()
                : register.Value.GetUInt16();
            if (actual != expected)
            {
                return Fail(tStates, $"test_num={testNumber}, hash={hash}: register {register.Name}, expected {expected:X4}, actual {actual:X4}.");
            }
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
            if (actual != expected)
            {
                return Fail(tStates, $"test_num={testNumber}, hash={hash}: RAM[{address:X5}] expected {expected:X2}, actual {actual:X2}.");
            }
        }

        foreach (JsonElement entry in final.GetProperty("ram").EnumerateArray())
        {
            byte expected = entry[1].GetByte();
            uint address = entry[0].GetUInt32();
            byte actual = memory.Read(address);
            if (actual != expected)
            {
                return Fail(tStates, $"test_num={testNumber}, hash={hash}: RAM[{address:X5}] expected {expected:X2}, actual {actual:X2}.");
            }
        }

        JsonElement expectedCycles = vector.GetProperty("cycles");
        if (expectedCycles.GetArrayLength() != actualCycles.Count)
        {
            return Fail(tStates, $"test_num={testNumber}, hash={hash}: cycle count expected {expectedCycles.GetArrayLength()}, actual {actualCycles.Count}.");
        }

        for (int index = 0; index < actualCycles.Count; index++)
        {
            string? difference = GetCycleDifference(expectedCycles[index], actualCycles[index]);
            if (difference is not null)
            {
                return Fail(tStates, $"test_num={testNumber}, hash={hash}, cycle={index}: {difference}");
            }
        }

        byte[] expectedQueue = [.. final.GetProperty("queue").EnumerateArray().Select(item => item.GetByte())];
        byte[] actualQueue = cpu.State.PendingQueue.ToArray();
        if (!expectedQueue.SequenceEqual(actualQueue))
        {
            return Fail(tStates, $"test_num={testNumber}, hash={hash}: queue expected [{Convert.ToHexString(expectedQueue)}], actual [{Convert.ToHexString(actualQueue)}].");
        }

        return new VectorRunResult(true, tStates, null);
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

    private static string? GetCycleDifference(JsonElement expected, Intel8086BusCycle actual)
    {
        if (expected[0].GetByte() != actual.PinBits)
        {
            return $"pin bits expected {expected[0].GetByte()}, actual {actual.PinBits}.";
        }

        if (actual.PinBits != 0 && expected[1].GetUInt32() != actual.MultiplexedBus)
        {
            return $"bus expected {expected[1].GetUInt32():X5}, actual {actual.MultiplexedBus:X5}.";
        }

        if (expected[2].GetString() != actual.SegmentStatus || expected[3].GetString() != actual.MemoryStatus || expected[4].GetString() != actual.IoStatus)
        {
            return $"segment/memory/I/O status expected {expected[2].GetString()}/{expected[3].GetString()}/{expected[4].GetString()}, " +
                $"actual {actual.SegmentStatus}/{actual.MemoryStatus}/{actual.IoStatus}.";
        }

        if (actual.BusStatus != "PASV" && expected[5].GetByte() != actual.Bhe)
        {
            return $"BHE expected {expected[5].GetByte()}, actual {actual.Bhe}.";
        }

        if (expected[6].GetUInt16() != actual.Data || expected[7].GetString() != actual.BusStatus || expected[8].GetString() != actual.TState)
        {
            return "data bus, bus status or T-state differs.";
        }

        if (expected[9].GetString()![0] != actual.QueueStatus || expected[10].GetByte() != actual.QueueData)
        {
            return $"queue status/data expected '{expected[9].GetString()![0]}'/{expected[10].GetByte():X2}, actual '{actual.QueueStatus}'/{actual.QueueData:X2}.";
        }

        return null;
    }

    private static VectorRunResult Fail(int tStates, string difference) => new(false, tStates, difference);
}
