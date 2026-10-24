# Stan wznowienia prac Intel 8086

Stan na 2026-10-25, w trakcie A12.

## Cel i branche

Zrealizować etapy A1–A12 rdzenia Intel 8086 kolejno, każdy na osobnym branchu bazującym na poprzednim, z weryfikacją i osobnym commitem. Branche: `A0-8086-architektura`, `A1-8086-wektory`, `A2-8086-szkielet`, `A3-8086-transfery-8-bit`, `A4-8086-alu-8-bit`, `A5-8086-transfery-16-bit-i-stos`, `A6-8086-alu-16-bit-shifts`, `A7-8086-sterowanie-przeplywem`, `A8-8086-multiply-divide-bcd`, `A9-8086-string-io`, `A10-8086-prefixes-decoder`, `A11-8086-reset-przerwania-stany`, `A12-8086-pelna-weryfikacja`. A12 zawiera historię A0–A11.

## Git i bieżące zmiany

- Branch `A12-8086-pelna-weryfikacja`, ostatni commit `7c3145b fix(core): odtworz granice IDIV procesora 8086` (rodzic `4fa60c4`).
- Bez pushowania. Working tree jest czysty; poprawka zakresu `IDIV`, testy, fixture oraz dokumentacja są zatwierdzone.
- Nowy fixture: `tests/VibeEmulator.Intel8086.Tests/Vectors/A8Arithmetic/F6.7.idiv-minimum-overflow.json`, dwa oryginalne rekordy SST 259 i 1330 z zachowanymi `test_hash`.

## Diagnoza i poprawka

Pełny przypięty zestaw SingleStepTests/8086 SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`, 323 pliki / 646 000 rekordów, wcześniej miał 449 569 passed, 196 429 skipped, 2 mismatched. Oba mismatchy były Type 0 dla `F6.7`: test 259 `IDIV byte [SS:BP-1FFEh]`, hash `2bf85c12cc23051ffb1fa0712a778162777711b08bc334c9495293af4a0fc24c`; test 1330 `IDIV CH`, hash `8eac0b0e86b15f0699cd8d9499050fb175141689d31b6edeb19e9c82be8c63c7`.

Przyczyna: helper dopuszczał `-128`/`-32768` jak późniejsze x86. Podręcznik Intel 8086 Family, sekcja IDIV s. 2-73, określa symetryczne zakresy `-127..127` i `-32767..32767`; minimum `-128`/`-32768` zgłasza Type 0. Potwierdzenie dopisano w `docs/cpu/sources.md`. Zmieniono `TryDivideByte/Word`, testy graniczne oraz manifest A8. Izolowany zestaw poprzednich 23 mismatchy przechodzi **23/23**.

## Wynik pełnego przebiegu po poprawce

- 646 000 rekordów: **449 571 passed, 196 429 skipped, 0 mismatched, 0 timeoutów, 0 błędów parsowania**.
- Raport: `/tmp/vibe-8086-a12-full-after-idiv-boundary.json`; log: `/tmp/vibe-8086-a12-full-after-idiv-boundary.log`.
- Intel8086 Release po dodaniu fixture: 11 251/11 251; pełna solucja Release: 14 051/14 051.
- Pełna bramka po zmianie: build Debug/Release 0 ostrzeżeń/błędów; formatter verify czysty; OpcodeInfo 512/512 zgodnych.

## Kolejne kroki

1. Kontynuować otwarte kryteria: zmapować 196 429 skipów na opkody/formy i wdrażać brakujące prawidłowe instrukcje jako osobno weryfikowane poprawki A12.
2. Zaplanować niezależny test ROM/program z jawną licencją i warunkami sukcesu. A12 pozostaje otwarty mimo zera mismatchy w bieżącym pełnym przebiegu.

## Komendy

```bash
MSBuildEnableWorkloadResolver=false dotnet build VibeEmulator.slnx -c Debug --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet build VibeEmulator.slnx -c Release --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet test VibeEmulator.slnx -c Release --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet format VibeEmulator.slnx --verify-no-changes --severity warn
dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code
```
