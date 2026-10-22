using VibeEmulator.Mos6502;

namespace VibeEmulator.CpuHarness;

/// <summary>
/// Protokół wyniku testów Blargga (`instr_test-v5`): status pod <c>$6000</c> — <c>$80</c> = test trwa,
/// <c>$00</c>–<c>$7F</c> = zakończony z tym kodem (0 = sukces); tekst od <c>$6004</c>, zakończony bajtem 0.
/// </summary>
public static class BlarggRunner
{
    /// <summary>Adres statusu w protokole Blargga.</summary>
    public const ushort StatusAddress = 0x6000;

    /// <summary>Początek tekstu wyniku.</summary>
    public const ushort TextAddress = 0x6004;

    private const byte Running = 0x80;

    /// <summary>Uruchamia ROM aż status opuści <c>$80</c> albo minie limit cykli.</summary>
    public static (HarnessResult Result, string Text, NesBus Bus) Run(InesImage image, ushort entryPc, long limitCycles)
    {
        var bus = new NesBus(image);
        var cpu = new StateMachineCpuCore(bus, CpuVariants.Nes2A03);
        cpu.Reset(entryPc);

        long instructions = 0;
        bool started = false;

        while (cpu.Cycles < limitCycles)
        {
            do
            {
                cpu.Tick();
            }
            while (cpu.Pending != 0 && cpu.Cycles < limitCycles);

            instructions++;

            if (cpu.Jammed)
            {
                return (new HarnessResult(HarnessOutcome.Jam, cpu.State.PC, cpu.Cycles, instructions, null, bus.IoWrites), ReadText(bus), bus);
            }

            byte status = bus.Peek(StatusAddress);

            // Pamięć startuje wyzerowana, więc $00 przed zapisem $80 nie znaczy „sukces”. Najpierw czekamy,
            // aż ROM zgłosi stan „trwa”, i dopiero potem na kod końcowy.
            if (!started)
            {
                started = status == Running;
                continue;
            }

            if (status != Running)
            {
                HarnessOutcome outcome = status == 0 ? HarnessOutcome.Pass : HarnessOutcome.Fail;
                return (new HarnessResult(outcome, cpu.State.PC, cpu.Cycles, instructions, status, bus.IoWrites), ReadText(bus), bus);
            }
        }

        string text = ReadText(bus);
        if (!started)
        {
            text = "protokół nie wystartował: status $6000 nigdy nie był $80\n" + text;
        }

        return (new HarnessResult(HarnessOutcome.Limit, cpu.State.PC, cpu.Cycles, instructions, null, bus.IoWrites), text, bus);
    }

    private static string ReadText(NesBus bus)
    {
        var chars = new System.Text.StringBuilder();

        for (ushort address = TextAddress; address < 0x8000; address++)
        {
            byte value = bus.Peek(address);
            if (value == 0)
            {
                break;
            }

            chars.Append((char)value);
        }

        return chars.ToString();
    }
}
