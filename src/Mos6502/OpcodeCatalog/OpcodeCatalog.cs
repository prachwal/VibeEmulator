namespace VibeEmulator.Mos6502;

/// <summary>
/// Katalog zarejestrowanych opkodów: jedno pole z adnotacją <see cref="OpcodeAttribute"/> na opkod.
/// Tabele wariantów (<see cref="CpuVariants"/>) składają się dokładnie z tych pól, więc adnotacja
/// i wpis tabeli opisują ten sam opkod. Narzędzie <c>tools/OpcodeInfo --verify-code</c> czyta
/// adnotacje refleksją i porównuje je z <c>docs/cpu/opcodes/*.csv</c>.
/// </summary>
/// <remarks>
/// Każde nowe pole musi mieć adnotację i musi trafić do tablicy <c>Opcodes</c>/<c>Additions</c>;
/// zgodność pilnuje <c>OpcodeCatalogTests</c>. Adnotacja nie jest czytana przez <c>Tick()</c>.
/// <para>
/// Katalog jest rozbity na pliki wariantów: <c>OpcodeCatalog.Nmos.cs</c> (zestaw <c>nmos</c>)
/// i <c>OpcodeCatalog.Cmos.cs</c> (zestaw <c>c02</c>). Oba to klasy zagnieżdżone <c>partial</c>,
/// więc nazwy publiczne (<c>OpcodeCatalog.Nmos.Opcodes</c>) się nie zmieniają.
/// </para>
/// <para>
/// Pole i adnotacja powtarzają te same wartości (atrybut wymaga stałych), więc każdy opkod jest zapisany
/// dwa razy. Po przekroczeniu ~80 wpisów usuń duplikację, budując tablicę z adnotacji raz
/// w inicjalizatorze statycznym. Do tego czasu nie dodawaj trzeciej kopii danych.
/// </para>
/// </remarks>
public static partial class OpcodeCatalog
{
}
