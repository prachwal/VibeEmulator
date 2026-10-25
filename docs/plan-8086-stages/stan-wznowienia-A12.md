# Stan wznowienia prac Intel 8086

Stan na 2026-10-25, branch A12 w trakcie; bieżąca zmiana dodaje alias ALU `82` i naprawia zawijanie adresu prefetch przy granicy IP.

## Cel i branche

Zrealizować etapy A1–A12 rdzenia Intel 8086 kolejno, każdy na osobnym branchu bazującym na poprzednim, z weryfikacją i osobnym commitem. Branche: `A0-8086-architektura`, `A1-8086-wektory`, `A2-8086-szkielet`, `A3-8086-transfery-8-bit`, `A4-8086-alu-8-bit`, `A5-8086-transfery-16-bit-i-stos`, `A6-8086-alu-16-bit-shifts`, `A7-8086-sterowanie-przeplywem`, `A8-8086-multiply-divide-bcd`, `A9-8086-string-io`, `A10-8086-prefixes-decoder`, `A11-8086-reset-przerwania-stany`, `A12-8086-pelna-weryfikacja`. A12 zawiera historię A0–A11.

## Bieżący stan Git

- Branch: `A12-8086-pelna-weryfikacja`. Ostatni commit przed obecną zmianą: `18ebcad docs(8086): zaktualizuj kryteria A12`.
- Implementacja `82`, osiem próbek po 20 przypadków, manifest i raporty są gotowe do commita; nie pushować.

## Ostatnia zmiana i dowody

Opkod `82` jest aliasem bajtowej natychmiastowej grupy ALU i współdzieli wykonanie z `80`. Dodano próbkę pierwszych 20 nieprefiksowanych przypadków dla każdego z ośmiu rozszerzeń ModR/M. Wektory ujawniły wrap błędnego 20-bitowego liniowego adresu prefetch na granicy IP; poprawka ponownie wylicza adres jako `CS:offset` z 16-bitowym zawinięciem.

- Pełny filtr przypiętych wektorów `82`, SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`: 16 000 rekordów, **8 041 passed, 7 959 skipped (segment-prefiksowane), 0 mismatched, timeoutów i błędów parsowania**.
- Osiem lokalnych próbek: 160/160; testy wektorów Intel8086: 9 882/9 882; pełny Intel8086 Release: 11 551/11 551.
- Pełna solucja Release: 14 351/14 351. Build Debug/Release: 0 ostrzeżeń i błędów; formatter verify czysty; OpcodeInfo 512/512 zgodnych, 0 rozbieżności.
- Pełny przypięty zestaw: 323 pliki / 646 000 rekordów, **471 612 passed, 174 388 skipped, 0 mismatched, 0 timeoutów i 0 błędów parsowania**.
- Raporty: `/tmp/vibe-8086-a12-op82-after-wrap.json` i `/tmp/vibe-8086-a12-full-after-op82.json`; źródłowy checkout: `/tmp/vibe-8086-a12-source/v1`.
- Pełne buildy Debug/Release, pełne Release testy, formatter i OpcodeInfo zostały uruchomione po tej zmianie i są zielone.

## Co pozostało

1. Sprawdzić `git diff --check` i zatwierdzić zmianę `82` na A12 osobnym commitem; bez pushowania.
2. Dalej mapować i redukować 174 388 jawnych skipów. Szczególnie istotne są brakujące formy FF oraz segment-prefix, F6/F7 i D0–D3.
3. Dodać niezależny test programowy/ROM z jawnym pochodzeniem, licencją i kryterium sukcesu. A12 pozostaje otwarty.

## Polecenia jakości

```bash
MSBuildEnableWorkloadResolver=false dotnet build VibeEmulator.slnx -c Debug --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet build VibeEmulator.slnx -c Release --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet test VibeEmulator.slnx -c Release --nologo -m:1
MSBuildEnableWorkloadResolver=false dotnet format VibeEmulator.slnx --no-restore --verify-no-changes --severity warn
dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code
```
