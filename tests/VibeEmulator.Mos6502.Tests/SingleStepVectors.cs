using System.Text.Json;
using System.Text.Json.Serialization;

namespace VibeEmulator.Mos6502.Tests;

/// <summary>
/// Walidacja rdzenia cyklowego wektorami SingleStepTests/65x02 (licencja MIT, patrz <c>Vectors/README.md</c>).
/// Każdy plik to jeden opkod z pierwszymi 100 testami. Dla każdego testu porównywane są, cykl po cyklu,
/// dostępy do magistrali (adres, wartość, odczyt/zapis), stan końcowy i pamięć końcowa.
/// </summary>
/// <remarks>
/// Odwzorowanie stanu: <c>pc</c> → PC; <c>s</c> (8-bit) → SP (<c>ushort</c> 0..0xFF);
/// <c>a</c>/<c>x</c>/<c>y</c> → A/X/Y; <c>p</c> → P bit po bicie. Bity Unused (0x20) i Break (0x10)
/// przenoszone są bez zmian — BRK (00.json, od etapu F) zmienia tylko I (i D w 65C02), nie B w rejestrze P.
/// Pliki są czytane z dysku (kopiowane do katalogu wyjściowego), bez sieci.
/// </remarks>
public static class SingleStepVectors
{
    private const string RootDirectory = "Vectors";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new BusAccessConverter() },
    };

    /// <summary>Uruchamia wszystkie testy wektorowe jednego opkodu jednego wariantu.</summary>
    /// <param name="variant">Wariant rdzenia.</param>
    /// <param name="vectorDirectory">Podkatalog w <c>Vectors/</c>, np. <c>nmos6502</c>.</param>
    /// <param name="opcode">Bajt opkodu; nazwa pliku to jego hex małymi literami.</param>
    /// <remarks>
    /// Etap G (2026-10-21) zdjął jedyny wyjątek od porównania (ADC/SBC z D=1) — od tego etapu
    /// każdy test z próbki jest porównywany w całości, bez pominięć.
    /// </remarks>
    public static void Run(CpuVariant variant, string vectorDirectory, byte opcode)
    {
        string path = Path.Combine(AppContext.BaseDirectory, RootDirectory, vectorDirectory, $"{opcode:x2}.json");
        string json = File.ReadAllText(path);
        List<VectorTest> tests = JsonSerializer.Deserialize<List<VectorTest>>(json, JsonOptions)
            ?? throw new InvalidDataException($"Pusty plik wektorów: {path}");

        if (tests.Count != 100)
        {
            Assert.Fail($"Plik {path} ma {tests.Count} testów, oczekiwano 100 (przycięta próbka).");
        }

        foreach (VectorTest test in tests)
        {
            RunTest(variant, opcode, test);
        }
    }

    private static void RunTest(CpuVariant variant, byte opcode, VectorTest test)
    {
        string context = $"Wariant {variant.Name}, opkod {opcode:X2}, test \"{test.Name}\"";
        var bus = new RecordingBus();

        foreach (int[] entry in test.Initial.Ram)
        {
            bus.Poke((ushort)entry[0], (byte)entry[1]);
        }

        var cpu = new StateMachineCpuCore(bus, variant);
        ref CpuState state = ref cpu.State;
        state.PC = (ushort)test.Initial.Pc;
        state.SP = (ushort)test.Initial.S;
        state.A = (byte)test.Initial.A;
        state.X = (byte)test.Initial.X;
        state.Y = (byte)test.Initial.Y;
        state.P = (StatusFlags)test.Initial.P;

        for (int cycle = 0; cycle < test.Cycles.Count; cycle++)
        {
            BusAccess expected = test.Cycles[cycle];
            bus.Accesses.Clear();
            cpu.Tick();

            if (bus.Accesses.Count != 1 || bus.Accesses[0] != expected)
            {
                string actual = bus.Accesses.Count == 0
                    ? "brak dostępu"
                    : string.Join(", ", bus.Accesses);

                Assert.Fail($"{context}, cykl {cycle + 1}: oczekiwano {expected}, otrzymano {actual}.");
            }
        }

        Expect(0, cpu.Pending, "Pending po ostatnim cyklu (granica instrukcji)", context);
        Expect(test.Final.Pc, cpu.State.PC, "PC", context);
        Expect(test.Final.S, cpu.State.SP, "SP", context);
        Expect(test.Final.A, cpu.State.A, "A", context);
        Expect(test.Final.X, cpu.State.X, "X", context);
        Expect(test.Final.Y, cpu.State.Y, "Y", context);
        Expect(test.Final.P, (int)cpu.State.P, "P", context);

        foreach (int[] entry in test.Final.Ram)
        {
            Expect(entry[1], bus.Peek((ushort)entry[0]), $"pamięć ${entry[0]:X4}", context);
        }
    }

    private static void Expect<T>(T expected, T actual, string what, string context)
        where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            Assert.Fail($"{context}: {what}: oczekiwano {expected}, otrzymano {actual}.");
        }
    }

    /// <summary>Jeden test z pliku wektorów (tylko pola używane przez harness).</summary>
    public sealed class VectorTest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("initial")]
        public VectorState Initial { get; set; } = new();

        [JsonPropertyName("final")]
        public VectorState Final { get; set; } = new();

        /// <summary>Każdy wpis to <c>[adres, wartość, "read"|"write"]</c>.</summary>
        [JsonPropertyName("cycles")]
        public List<BusAccess> Cycles { get; set; } = [];
    }

    /// <summary>Czyta cykl w formacie wektora: <c>[adres, wartość, "read"|"write"]</c>.</summary>
    private sealed class BusAccessConverter : JsonConverter<BusAccess>
    {
        public override BusAccess Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Cykl wektora musi być tablicą [adres, wartość, rodzaj].");
            }

            reader.Read();
            ushort address = (ushort)reader.GetInt32();
            reader.Read();
            byte value = (byte)reader.GetInt32();
            reader.Read();
            string kind = reader.GetString() ?? throw new JsonException("Brak rodzaju dostępu.");
            reader.Read();

            if (reader.TokenType != JsonTokenType.EndArray)
            {
                throw new JsonException("Cykl wektora ma więcej niż trzy pola.");
            }

            bool isWrite = kind switch
            {
                "read" => false,
                "write" => true,
                _ => throw new JsonException($"Nieznany rodzaj dostępu: {kind}."),
            };

            return new BusAccess(address, value, isWrite);
        }

        public override void Write(Utf8JsonWriter writer, BusAccess value, JsonSerializerOptions options) =>
            throw new NotSupportedException("Harness wektorów tylko czyta pliki.");
    }

    /// <summary>Stan procesora i pamięć z wektora.</summary>
    public sealed class VectorState
    {
        [JsonPropertyName("pc")]
        public int Pc { get; set; }

        [JsonPropertyName("s")]
        public int S { get; set; }

        [JsonPropertyName("a")]
        public int A { get; set; }

        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }

        [JsonPropertyName("p")]
        public int P { get; set; }

        /// <summary>Pary <c>[adres, wartość]</c>.</summary>
        [JsonPropertyName("ram")]
        public List<int[]> Ram { get; set; } = [];
    }
}
