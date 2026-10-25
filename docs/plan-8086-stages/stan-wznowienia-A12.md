# Stan wznowienia prac Intel 8086

Stan na 2026-10-25, branch A12 w trakcie; bieżąca zmiana dodaje `PUSHF`/`POPF` (`9C`/`9D`).

## Cel i branche

Zrealizować etapy A1–A12 rdzenia Intel 8086 kolejno, każdy na osobnym branchu bazującym na poprzednim, z weryfikacją i osobnym commitem. Branche: `A0-8086-architektura`, `A1-8086-wektory`, `A2-8086-szkielet`, `A3-8086-transfery-8-bit`, `A4-8086-alu-8-bit`, `A5-8086-transfery-16-bit-i-stos`, `A6-8086-alu-16-bit-shifts`, `A7-8086-sterowanie-przeplywem`, `A8-8086-multiply-divide-bcd`, `A9-8086-string-io`, `A10-8086-prefixes-decoder`, `A11-8086-reset-przerwania-stany`, `A12-8086-pelna-weryfikacja`. A12 zawiera historię A0–A11.

## Bieżący stan Git

- Branch: `A12-8086-pelna-weryfikacja`. Ostatni commit: `ccea2a8 feat(core): dodaj alias ALU 82 dla 8086`.
- Implementacja `PUSHF`/`POPF`, dwie próbki po 20 przypadków, manifest i aktualizacje raportów są gotowe do commita; nie pushować.

## Ostatnia zmiana i dowody

`PUSHF` i `POPF` (`9C`/`9D`) korzystają ze ścieżki transferu słowa na stos. `POPF` maskuje zarezerwowane bity według wzorca w dokładnych wektorach 8086.

- Pełny filtr `9C`/`9D`, SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`: 4 000/4 000; lokalne próbki 40/40; wektory Intel8086 Release 9 922/9 922.
- Pełny Intel8086 Release: 11 591/11 591; pełna solucja Release: 14 391/14 391.
- Build Debug/Release: 0 ostrzeżeń i błędów; formatter verify czysty; OpcodeInfo 512/512 zgodnych, 0 rozbieżności.
- Pełne przypięte źródło: 323 pliki / 646 000 rekordów, **475 612 passed, 170 388 skipped, 0 mismatched, 0 timeoutów, 0 błędów parsowania**.
- Raporty: `/tmp/vibe-8086-a12-9c9d-full-after-fix.json` i `/tmp/vibe-8086-a12-full-after-pushf-popf.json`; dane źródłowe odtworzone w `/tmp/vibe-8086-a12-complete/v1`.
- Wszystkie wymagane buildy, testy, formatter i OpcodeInfo uruchomione po zmianie i są zielone.

## Co pozostało

1. Sprawdzić `git diff --check` i zatwierdzić zmianę `PUSHF`/`POPF` na A12 osobnym commitem; bez pushowania.
2. Dalej mapować i redukować 170 388 jawnych skipów. Szczególnie istotne są brakujące formy FF oraz segment-prefix, F6/F7 i D0–D3.
3. Dodać niezależny test programowy/ROM z jawnym pochodzeniem, licencją i kryterium sukcesu. A12 pozostaje otwarty.

## Polecenia jakości

```bash
MSBuildEnableWorkloadResolver=false dotnet build VibeEmulator.slnx -c Debug --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet build VibeEmulator.slnx -c Release --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet test VibeEmulator.slnx -c Release --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet format VibeEmulator.slnx --no-restore --verify-no-changes --severity warn
dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code
```
