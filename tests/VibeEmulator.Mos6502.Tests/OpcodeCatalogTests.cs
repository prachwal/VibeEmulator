using System.Reflection;
using VibeEmulator.Mos6502;

namespace VibeEmulator.Mos6502.Tests;

/// <summary>
/// Spójność katalogu adnotacji z tabelami wariantów. Adnotacja jest metadanymi weryfikowanymi
/// z CSV przez <c>tools/OpcodeInfo --verify-code</c>; te testy pilnują, żeby nie rozjechała się
/// z wartością pola ani z tabelą wariantu.
/// </summary>
public class OpcodeCatalogTests
{
    [Theory]
    [InlineData(typeof(OpcodeCatalog.Nmos))]
    [InlineData(typeof(OpcodeCatalog.Cmos))]
    public void Katalog_PoleMaAdnotacjeZgodneZWartoscia(Type catalog)
    {
        int checkedFields = 0;

        foreach ((FieldInfo field, OpcodeAttribute attribute) in AnnotatedFields(catalog))
        {
            var opcode = (CpuOpcode)field.GetValue(null)!;

            Assert.Equal(attribute.Opcode, opcode.Opcode);
            Assert.Equal(attribute.Mnemonic, opcode.Mnemonic);
            Assert.Equal(attribute.Mode, opcode.Mode);
            Assert.Equal(attribute.Kind, opcode.Kind);
            Assert.Equal(attribute.Action, opcode.Action);
            Assert.Equal(attribute.InternalCycles, opcode.InternalCycles);
            Assert.Equal(attribute.BitNumber, opcode.BitNumber);
            checkedFields++;
        }

        Assert.NotEqual(0, checkedFields);
    }

    [Fact]
    public void Katalog_ParaZestawOpkodWystepujeRaz()
    {
        List<(CpuSet Set, byte Opcode)> keys = [];

        foreach (Type catalog in new[] { typeof(OpcodeCatalog.Nmos), typeof(OpcodeCatalog.Cmos) })
        {
            foreach ((FieldInfo _, OpcodeAttribute attribute) in AnnotatedFields(catalog))
            {
                keys.Add((attribute.Set, attribute.Opcode));
            }
        }

        Assert.Equal(keys.Count, keys.Distinct().Count());

        // Ten sam bajt w dwóch zestawach jest poprawny (np. STZ nie występuje w NMOS, ale
        // LDA/DEC tak) — kluczem jest para (zestaw, opkod). Liczby nie są wpisane na stałe,
        // żeby nowy opkod nie wymagał poprawiania testu.
        Assert.NotEmpty(keys);
    }

    /// <summary>
    /// Tablica wariantu musi zawierać dokładnie te same opkody co adnotowane pola — porównanie
    /// zawartości, nie tylko liczby elementów. Łapie pole pominięte przy dopisywaniu do tablicy.
    /// </summary>
    [Theory]
    [InlineData(typeof(OpcodeCatalog.Nmos))]
    [InlineData(typeof(OpcodeCatalog.Cmos))]
    public void Katalog_TablicaMaDokladnieTeSameOpkodyCoPola(Type catalog)
    {
        CpuOpcode[] table = catalog == typeof(OpcodeCatalog.Nmos)
            ? OpcodeCatalog.Nmos.Opcodes
            : OpcodeCatalog.Cmos.Additions;

        byte[] fromTable = [.. table.Select(static opcode => opcode.Opcode).Order()];
        byte[] fromFields = [.. AnnotatedFields(catalog)
            .Select(static entry => ((CpuOpcode)entry.Field.GetValue(null)!).Opcode)
            .Order()];

        Assert.Equal(fromFields, fromTable);
    }

    [Fact]
    public void Adnotacja_ZestawMaNazweZgodnaZKolumnnaCsv()
    {
        OpcodeAttribute nmos = new(0xA9, CpuSet.Nmos, "LDA", AddressingMode.Immediate, OpKind.Read, CpuAction.Lda);
        OpcodeAttribute cmos = new(0x64, CpuSet.Cmos65C02, "STZ", AddressingMode.ZeroPage, OpKind.Write, CpuAction.Stz);

        Assert.Equal("nmos", nmos.CsvSet);
        Assert.Equal("c02", cmos.CsvSet);
    }

    private static IEnumerable<(FieldInfo Field, OpcodeAttribute Attribute)> AnnotatedFields(Type catalog)
    {
        foreach (FieldInfo field in catalog.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetCustomAttribute<OpcodeAttribute>() is { } attribute)
            {
                yield return (field, attribute);
            }
        }
    }
}
