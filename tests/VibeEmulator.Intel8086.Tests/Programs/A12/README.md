# Niezależny program testowy 8086

`independent-smoke.asm` jest autorskim, samokontrolującym programem dla 8086. Nie pochodzi z ROM-u ani z zewnętrznego zestawu diagnostycznego. Program i jego listing w teście C# są objęte licencją MIT z pliku [LICENSE.txt](LICENSE.txt).

Listing wykonuje:

```asm
mov ax, 2
add ax, 3
cmp ax, 5
jne failure
hlt
failure:
mov ax, 0xDEAD
hlt
```

Bajty od offsetu `0000h`: `B8 02 00 05 03 00 3D 05 00 75 01 F4 B8 AD DE F4`.
Test uruchamia CPU od `0000:0000`; wypełnia pamięć programem i podaje sześciobajtowe okno kolejki na każdej granicy instrukcji. To jawny hostowy feeder wymagany przez aktualny interfejs rdzenia, nie dowód autonomicznego pobierania instrukcji przez magistralę.

Uruchomienie:

```bash
dotnet test tests/VibeEmulator.Intel8086.Tests/VibeEmulator.Intel8086.Tests.csproj \
  -c Release --filter FullyQualifiedName~Intel8086IndependentProgramTests --nologo
```

Kryteria sukcesu: HLT w maksymalnie 256 taktach, `AX=0005h`, `IP=000Ch`. Gałąź `failure` ustawia `AX=DEADh`, więc błędne porównanie lub skok jest wykrywalny. Test jest niezależnym sprawdzeniem sekwencji instrukcji, ale nie testuje DOS, BIOS, urządzeń, autonomicznego prefetch ani pełnej zgodności cyklowej.
