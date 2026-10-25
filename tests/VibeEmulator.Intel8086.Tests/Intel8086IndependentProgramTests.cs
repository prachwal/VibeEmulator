using VibeEmulator.Intel8086;
using VibeEmulator.Memory;

namespace VibeEmulator.Intel8086.Tests;

/// <summary>Uruchamia oryginalny mini-program integracyjny niezależny od korpusu SST.</summary>
public sealed class Intel8086IndependentProgramTests
{
    [Fact]
    public void ArithmeticAndConditionalBranchProgram_ReachesSuccessHalt()
    {
        // Listing: MOV AX,2; ADD AX,3; CMP AX,5; JNE failure; HLT;
        // failure: MOV AX,0xDEAD; HLT.
        byte[] program = [
            0xB8, 0x02, 0x00,
            0x05, 0x03, 0x00,
            0x3D, 0x05, 0x00,
            0x75, 0x01,
            0xF4,
            0xB8, 0xAD, 0xDE,
            0xF4,
        ];
        const ushort codeSegment = 0;
        const ushort codeOffset = 0;
        uint physicalStart = (uint)((codeSegment << 4) + codeOffset);
        var memory = new FlatAddressMemory(0x100000);
        for (int index = 0; index < program.Length; index++)
        {
            memory.Write(physicalStart + (uint)index, program[index]);
        }

        var cpu = new Intel8086Core(memory)
        {
            State = Intel8086State.Initial(codeSegment, codeOffset)
        };
        int ticks = 0;
        List<string> trace = [];
        ushort lastIp = codeOffset;
        while (!cpu.Halted && ticks < 256)
        {
            if (cpu.Pending == 0)
            {
                // Host supplies the next 8086 prefetch window at instruction boundaries.
                // Autonomous queue refill is not part of this core-level program smoke test.
                byte[] queue = [.. Enumerable.Repeat((byte)0x90, 6)];
                int available = Math.Min(queue.Length, program.Length - cpu.State.IP);
                if (available > 0)
                {
                    Array.Copy(program, cpu.State.IP, queue, 0, available);
                }

                cpu.State.Queue = queue;
                cpu.State.QueueHead = 0;
                cpu.State.QueueCount = queue.Length;
            }

            cpu.Tick();
            ticks++;
            if (cpu.State.IP != lastIp)
            {
                trace.Add($"{lastIp:X4}:{cpu.Opcode:X2}->IP={cpu.State.IP:X4},AX={cpu.State.AX:X4}");
                lastIp = cpu.State.IP;
            }
        }

        Assert.True(cpu.Halted, $"Program nie osiągnął HLT w 256 taktach; IP={cpu.State.IP:X4}.");
        Assert.InRange(ticks, 1, 255);
        Assert.True(cpu.State.AX == 0x0005 && cpu.State.IP == codeOffset + 12,
            $"Nieoczekiwany wynik programu: AX={cpu.State.AX:X4}, IP={cpu.State.IP:X4}, ticks={ticks}, cycles={cpu.Cycles}; {string.Join("; ", trace)}.");
    }
}
