using VibeEmulator.Core.C64;
using VibeEmulator.Mos6502;

namespace C64Trace;

/// <summary>Scenariusz własny (--hex / --load) oraz nakładanie --reg i --poke na dowolną maszynę.</summary>
internal static class CustomProgram
{
    /// <summary>Buduje maszynę z programem; przy błędzie zwraca null i opis w <paramref name="error"/>.</summary>
    public static C64Machine? Create(TraceOptions options, out string? error)
    {
        error = null;
        ushort origin = options.Org ?? TraceOptions.DefaultOrigin;
        byte[] data;

        if (options.Hex is not null)
        {
            data = options.Hex;
        }
        else
        {
            string path = options.LoadPath ?? string.Empty;
            byte[] file;

            try
            {
                file = File.ReadAllBytes(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                error = $"--load: nie można odczytać pliku '{path}' ({ex.Message}).";
                return null;
            }

            if (string.Equals(Path.GetExtension(path), ".prg", StringComparison.OrdinalIgnoreCase))
            {
                if (file.Length < 2)
                {
                    error = "plik .prg musi zawierać co najmniej 2 bajty adresu ładowania.";
                    return null;
                }

                // Nagłówek .prg: adres ładowania little-endian; --org go nadpisuje.
                ushort fileOrigin = (ushort)(file[0] | (file[1] << 8));
                origin = options.Org ?? fileOrigin;
                data = file[2..];
            }
            else
            {
                data = file;
            }
        }

        if (data.Length == 0)
        {
            error = "program jest pusty.";
            return null;
        }

        if (origin + data.Length > 0x10000)
        {
            error = $"program ({data.Length} B) nie mieści się w pamięci od adresu ${origin:X4}.";
            return null;
        }

        C64Machine machine = C64Machine.CreateCycleExact();
        machine.Reset(origin);
        machine.Bus.LoadRam(origin, data);

        return machine;
    }

    /// <summary>Ustawia rejestry (po Reset) i wykonuje zapisy --poke. Wywoływać przed podpięciem obserwatora.</summary>
    public static void ApplyOverrides(C64Machine machine, TraceOptions options)
    {
        ref CpuState state = ref machine.Cpu.State;

        foreach (RegisterAssignment register in options.Registers)
        {
            switch (register.Name)
            {
                case "A":
                    state.A = register.Value;
                    break;

                case "X":
                    state.X = register.Value;
                    break;

                case "Y":
                    state.Y = register.Value;
                    break;

                case "SP":
                    state.SP = register.Value;
                    break;
            }
        }

        foreach (MemoryWrite poke in options.Pokes)
        {
            machine.Bus.Write(poke.Address, poke.Value);
        }
    }
}
