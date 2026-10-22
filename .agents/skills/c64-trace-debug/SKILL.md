---
name: c64-trace-debug
description: 'Debug C64 cycle-exact behaviour with the tools/C64Trace tracer. Use when checking cycle counts, bus accesses (RMW, dummy reads), badline / AEC / BA cycle stealing, raster timing, or when a CPU/VIC change must be proved cycle by cycle. Covers running the tracer, tracing your own program, stop conditions, JSON output and the limits of the model.'
argument-hint: 'Opisz, co chcesz sprawdzić (np. "ile cykli kradnie badline w linii 48" albo "czy INC zapisuje dwa razy")'
---

# Debugowanie C64 tracerem (`tools/C64Trace`)

## When to Use

- Zmieniasz `StateMachineCpuCore`, `VicII`, `C64Bus` i chcesz **zobaczyć** efekt w cyklach, a nie zgadywać.
- Pytanie brzmi „w którym cyklu?”: badline, `AEC`/`BA`, dummy read, RMW, przekroczenie strony.
- Test jednostkowy jest czerwony i trzeba zobaczyć, co CPU robi na magistrali.
- Chcesz sprawdzić własny fragment kodu 6502 bez edytowania `ScenarioBuilder.cs`.

## Uruchamianie

Zbuduj raz, potem uruchamiaj gotową bibliotekę (to polecenie jest dopuszczone w auto-approve
w `.vscode/settings.json`; `dotnet run` wymaga zatwierdzenia):

```bash
dotnet build tools/C64Trace -c Release
dotnet tools/C64Trace/bin/Release/net10.0/VibeEmulator.C64Trace.dll <scenariusz|opcje>
```

Zawsze `Release`. `--help` wypisuje wszystkie opcje, scenariusze i kody wyjścia.

**Kontrakt:** wynik na `stdout`; ostrzeżenia i komunikaty informacyjne na `stderr` (w trybach
`vice` i `json` także nagłówek). Kod wyjścia: `0` OK, `2` błąd użycia, `3` CPU zawiesił się (JAM)
bez `--until jam`. Dane do analizy bierz z `--format json`, tekst tabelaryczny jest dla człowieka.

## Przepisy

### 1. Prześledź własny program

```bash
... --hex "A9 7F 8D 00 03 4C 00 02"            # bajty hex; domyślnie ładowane pod $0200, PC=$0200
... --load program.prg                         # .prg: 2 pierwsze bajty = adres ładowania
... --hex "A8 AA 8D 00 03" --org 0300 --reg A=11 --poke 0400=10 --cycles 9
```

`--poke ADRES=WART` przechodzi przez magistralę (działa też dla rejestrów VIC, np. `D011=18`)
i **nie trafia do śladu**. Uwaga na kolejność: `--poke` na adres kodu nadpisuje załadowane bajty.
Pamięć poza programem to zera (opkod `00` = `BRK`), a program, który „wypadnie” poza swój koniec,
trafia w nieznany opkod i kończy się JAM-em (kod wyjścia 3). Dlatego kończ program `JMP`-em w pętlę
albo ogranicz `--cycles`.
Własny program bez `--cycles`/`--until`/`--instr` kończy się po 1000 cykli — **a okno `--from/--to`
nie może wyjść poza ten limit**, więc dla późnych cykli podaj `--cycles` (patrz przepis 4).

### 2. Zatrzymaj się dokładnie tam, gdzie trzeba

```bash
... --until write=0300          # pierwszy zapis CPU pod $0300 (wiersz z zapisem jest w śladzie)
... --until read=D012           # odczyt rejestru rastra
... --until pc=0202             # cykl pobrania opkodu spod adresu
... --until raster=48:14        # linia:cykl_w_linii (liczone od 0, wartości sprzed ticku)
... --until jam                 # zawieszenie CPU (nieznany opkod); wtedy kod wyjścia 0
... --instr 3                   # po 3 ukończonych instrukcjach
... --cycles 200                # twardy limit cykli maszyny (1..1000000)
```

`--until` można powtarzać; zatrzymuje pierwszy spełniony. Powód zatrzymania jest w ostatniej linii
(`Zatrzymano: …` w tekście, pole `stop` w JSON): `until:<warunek>`, `instr-limit`, `cycle-limit`,
`jam`, `end`. `--from/--to` tylko zawężają **wyświetlane** cykle.

### 3. Przeliczenie rastra na numer cyklu

Numer cyklu maszyny (liczony od 1, jak w kolumnie `cykl`) wynika z pozycji rastra:

```text
cykl = linia * 63 + cykl_w_linii + 1        # np. raster=48:14  →  48*63 + 14 + 1 = 3039
```

Sprawdzone na całej klatce (19 656 cykli). Odwrotnie: `linia = (cykl-1) / 63`, `cykl_w_linii = (cykl-1) % 63`.

### 4. Badline, AEC i BA

Żeby dostać badline w linii 48 ($30), ustaw `DEN=1` i `YSCROLL=0` w `$D011`:

```bash
... --hex "EA 4C 00 02" --poke D011=18 --cycles 3100 --from 3036 --to 3085
```

(`--cycles` jest tu obowiązkowe: bez niego własny program kończy się po 1000 cykli i `--from 3036`
daje błąd użycia.)

Zmierzone: w linii 48 `BA` jest niskie w cyklach **12–54** (43), `AEC` w **15–54** (40 cykli kradzieży,
cykle maszyny 3040–3079). W cyklach 12–14 CPU jeszcze pracuje (w tabeli: `cpu` + `BA`). Bez
`--poke D011=18` stan po resecie daje pierwszą badline w linii 51. Przez klatkę: 25 linii po 40 cykli
= 1000 skradzionych (`stolenCycles` w podsumowaniu).

### 5. Analiza wyniku JSON

Jedna linia = jeden cykl, ostatnia = podsumowanie. Adresy 4-cyfrowe, bajty 2-cyfrowe, hex wielkimi literami, jako stringi.

```json
{"type":"cycle","cycle":6,"line":0,"lineCycle":5,"aec":true,"ba":true,"pc":"0202","opcode":"8D",
 "instruction":"STA abs $0300","newInstruction":false,"a":"7F","x":"00","y":"00","sp":"FD",
 "flags":"nv-bdIzc","p":"24","jammed":false,"accesses":[{"rw":"W","addr":"0300","value":"7F"}]}
{"type":"summary","scenario":"custom","stop":"until:write=0300","cycles":6,"cpuCycles":6,
 "stolenCycles":0,"jammed":false,"shown":6}
```

```bash
... --format json | python3 -c 'import sys,json
rows=[json.loads(l) for l in sys.stdin]
print([r["cycle"] for r in rows if r["type"]=="cycle" and not r["aec"]][:3])'
```

Wiersz pokazuje stan **po** ticku (rejestry, `pc` instrukcji), a `line`/`lineCycle`/`aec`/`ba`
— stan VIC użyty w tym cyklu. `accesses` to wszystkie dostępy CPU do magistrali w danym cyklu.

### 6. Sprawdzenie typowych niezmienników

- RMW: scenariusz `rmw` pokazuje zapis starej i nowej wartości (dwa `W` pod ten sam adres).
- Koszt instrukcji: `... --instr 1` i porównaj `cycles` w podsumowaniu z oczekiwanym.
- Koszt klatki: scenariusz `count` (pomiar, bez śladu).

## Ograniczenia modelu — nie wyciągaj za dużo ze śladu

- Rdzeń zna dziś tylko część opkodów obu wariantów — aktualny zakres podaje `tools/OpcodeInfo --verify-code`
  (`counts.attributes`, `missingInCodeByClass`) i tabela „Stan realizacji” w `docs/plan-c64-cycle-exact.md`; reszta to JAM
  i `???` w kolumnie instrukcji. Zgodność cykl po cyklu jest potwierdzona wektorami dla zaimplementowanych opkodów (w tym BRK),
  nie dla pozostałych. Brakuje m.in.: opkodów nieoficjalnych NMOS (etapy J/K), sekwencji RESET. Stan faz:
  `docs/plan-c64-cycle-exact.md`.
- CLI tworzy maszynę **bez ROM-ów** (brak opcji ładowania KERNAL/BASIC/CHARGEN), więc cokolwiek pod
  `$A000+` to RAM.
- Ślad zawiera dostępy **CPU**. Pobrania VIC-II (c-access, g-access, sprite DMA) nie są modelowane.
- CIA i SID to szkielety; IRQ (VIC + CIA1) i NMI (CIA2) są dostarczane do CPU przez `IInterruptLines`;
  `$00/$01` nie są jeszcze podpięte do `ProcessorPort` w `C64Bus`.
- `--format vice` jest tylko „zbliżony” do logu VICE; w repo nie ma logu referencyjnego, więc zgodności
  nie potwierdzono. Zgodność ze sprzętem wymaga porównania z referencją (VICE, Tom Harte), nie samego śladu.
- Nie używaj `--dump` do plików poza `artifacts/`; narzędzie nadpisuje wskazany plik bez pytania.

## Zasada

Zmieniasz zachowanie cyklowe → najpierw pokaż ślad (przed i po), potem zmieniaj oczekiwane wartości
w testach `tests/VibeEmulator.Core.Tests/C64/`. Wartości cykli weryfikuj z dokumentacją/VICE, nie ze śladem.
