# Źródła

Pobrane i przeczytane 2026-10-21. „Przeczytano” = treść wczytana i użyta; „nie przeczytano” = adres znany lub pobrany, ale
treść nieużyta. Kopie robocze nie są w repo (testy GPL i duże pliki wektorów trzymamy poza nim).

## Specyfikacje i opracowania

| źródło | status | co z niego |
|---|---|---|
| [Zilog Z80 Family CPU User Manual (UM0080, rev. 11, 2016)](https://www.zilog.com/docs/z80/z80cpu_um.pdf) | **przeczytano** (rozdziały: pin descriptions, interrupt response, instrukcje `LD A,I`/`LD A,R`) | linie INT/NMI/RESET, IM 0/1/2, HALT; `LD A,I`/`LD A,R` zeruje `P/V`, gdy przerwanie wystąpi podczas instrukcji; RESET wymaga minimum 3 taktów i resetuje PC, I, R, IFF1/2, IM=0 |
| [The Undocumented Z80 Documented, v0.91 (Sean Young)](https://www.ballyalley.com/ml/z80_docs/Undocumented%20Z80%20Documented,%20The%20%28Sean%20Young%29.pdf) | **przeczytano** (rozdziały 5.2–5.5, 6.2 i 7.1) | empiryczne timing przerwań, wartość `FF` na niepodłączonej magistrali IM0, IM2 bez wyrównania adresu wektora, NMI pozostawia IFF2 bez zmiany |
| [redcode/Z80 Wiki: Interrupts](https://github.com/redcode/Z80/wiki/Interrupts) | **przeczytano** (2026-10-22) | IM0 z prefiksami, sekwencje M-cycle/T-state IM0/1/2 i NMI, opóźnienie EI, zachowanie HALT |
| [redcode/Z80: test-Z80.c](https://github.com/redcode/Z80/blob/master/sources/test-Z80.c) | **przeczytano** (2026-10-22) | inwentarz dostępnych zestawów programowych Z80 i metadane testów Patrika Raka |
| [agn453/ZEXALL](https://github.com/agn453/ZEXALL) | **przeczytano i uruchomiono** (2026-10-22; commit `8f71d418bae69a476a5a0e5c6e122c8801b8d9f4`) | licencja GPL-2.0; wejście CP/M pod `0100h`, inicjalizacja `SP` z worda pod `0006h`, BDOS 9 dla łańcuchów, `JP 0` po zakończeniu; ZEXDOC/ZEXALL zaliczyły po 67/67 grup — szczegóły i SHA-256 w [Z11](../plan-z80-stages/Z11-harness-programow-zex.md) |
| [WDC W65C02S datasheet, 8.10.2018](https://eater.net/datasheets/w65c02s.pdf) | **przeczytano** (tekst wyciągnięty ze strumieni PDF; ekstrakcja rozbijała niektóre cyfry) | tabela operacji (212 opkodów), tabela opkodów nieprawidłowych, rejestr statusu, wektory, uwaga o `BRK` |
| [NESdev: CPU interrupts](https://www.nesdev.org/wiki/CPU_interrupts) | **przeczytano**, `oldid=21632` | sekwencje, próbkowanie, hijacking |
| [NESdev: CPU power up state](https://www.nesdev.org/wiki/CPU_power_up_state) | **przeczytano**, `oldid=22091` | stan po włączeniu i resecie (2A03G) |
| [NESdev: CPU](https://www.nesdev.org/wiki/CPU) | **przeczytano**, `oldid=20204` | 2A03 bez trybu dziesiętnego; opkody nieoficjalne identyczne |
| [NESdev: CPU unofficial opcodes](https://www.nesdev.org/wiki/CPU_unofficial_opcodes) | **przeczytano**, `oldid=21171` | macierz opkodów, nazwy, uwagi |
| [NESdev: Emulator tests](https://www.nesdev.org/wiki/Emulator_tests) | **przeczytano** (część o CPU) | opisy testów |
| [NESdev: Instruction reference](https://www.nesdev.org/wiki/Instruction_reference) | pobrano, **nie przeczytano w całości** | wpływ instrukcji na flagi |
| NESdev: CPU ALL, CPU addressing modes | pobrano, **nie przeczytano** | |
| NESdev: CPU registers, CPU status flag behavior, CPU instructions | **nie pobrano** | |
| [pagetable: Internals of BRK/IRQ/NMI/RESET](https://www.pagetable.com/?p=410) (M. Steil, 2010) | **przeczytano** | sekwencje, ślad resetu |
| [Bruce Clark: Decimal Mode](http://www.6502.org/tutorials/decimal_mode.html) | **przeczytano** (do Dodatku B włącznie) | algorytmy, flagi, cykle, D przy przerwaniach |
| [6502.org: opcodes](http://www.6502.org/tutorials/6502opcodes.html) | adres odpowiada, **nie przeczytano** | |
| [Oxyron: 6502 opcodes](https://www.oxyron.de/html/opcodes02.html), [Oxyron: 65C02](https://www.oxyron.de/html/opcodesc02.html) | **przeczytano**, sparsowano | tabele opkodów (kopia lustrzana z 2024) |
| [masswerk: „Illegal” Opcodes Demystified](https://masswerk.at/nowgobang/2021/6502-illegal-opcodes) | **przeczytano**, sparsowano | tabele nieoficjalnych, aliasy |
| [No More Secrets: NMOS 6510 Unintended Opcodes](https://www.cpcwiki.eu/imgs/7/7d/NoMoreSecrets-NMOS6510UnintendedOpcodes-20212412.pdf) | **nie przeczytano** | autorytet dla semantyki niestabilnych opkodów |
| [C64-Wiki: Undocumented opcode](https://www.c64-wiki.com/wiki/Undocumented_opcode) | tylko fragment z wyszukiwania | 151 + 105 |
| [perfect6502](https://github.com/mist64/perfect6502), [Visual6502](http://visual6502.org/) | adresy odpowiadają, **nie badano** | wzorzec tranzystorowy |

**Uwaga o dostępie:** serwer NESdev odpowiada kodem 403 na pobieranie skryptem, więc strony pobrano z kopii Internet Archive
(`web.archive.org/web/2024id_/…`, z `--compressed`). Numery rewizji (`oldid`) pochodzą ze stopki pobranych kopii.

## Źródła do planu 8086

- [Intel, The 8086 Family User's Manual, October 1979 (9800722-03)](https://www.inf.pucrs.br/~calazans/undergrad/orgcomp_EC/mat_microproc/intel-8086_family_Users_Manual.pdf): **przeczytano częściowo** 2026-10-24 — tabela 2-21 podaje dla `DIV` 80–90 zegarów (`r/m8`), 144–162 (`r/m16`), a pamięciowe formy mają odpowiednio `(86–96)+EA` i `(150–168)+EA`; dla `IDIV`: 101–112, 165–184, `(107–118)+EA`, `(171–190)+EA`. Dla `IMUL` rejestrowego: 80–98 (`r/m8`) i 128–154 (`r/m16`). Tabela opisuje też `ESC`: 2 zegary dla rejestru, `8+EA` dla pamięci. Dla transferu słowa pod nieparzystym adresem instrukcja dodaje 4 zegary. Sekcja o stringach (2-42) podaje, że przy przerwaniu wieloprefiksowego REP procesor pamięta wyłącznie prefiks bezpośrednio poprzedzający instrukcję; po powrocie pozostałe prefiksy nie zostają odtworzone. Pozostałe reguły sprzętowe 8086 wymagają osobnej weryfikacji. Kopia na Bitsavers zwracała HTTP 403 podczas wcześniejszej próby.
- [Intel, ASM86 Language Reference Manual (121703-003, November 1983)](https://bitsavers.trailing-edge.com/pdf/intel/ISIS_II/121703-003_ASM86_Language_Reference_Manual_Nov83.pdf): **przeczytano częściowo** 2026-10-24 — sekcja instrukcji oznacza `PUSHA` (60h), `POPA` (61h), `BOUND` (62h), `PUSH immediate` (68h/6Ah), przesunięcia z licznikiem immediate (C0h/C1h), `ENTER` (C8h) i `LEAVE` (C9h) jako iAPX 186; nie należą do dekodowania docelowego 8086.
- [Intel, ASM86 Language Reference Manual (121703-003, November 1983)](https://bitsavers.trailing-edge.com/pdf/intel/ISIS_II/121703-003_ASM86_Language_Reference_Manual_Nov83.pdf): **przeczytano częściowo** 2026-10-24 — sekcja instrukcji oznacza `PUSHA` (60h), `POPA` (61h), `BOUND` (62h), `PUSH immediate` (68h/6Ah), przesunięcia z licznikiem immediate (C0h/C1h), `ENTER` (C8h) i `LEAVE` (C9h) jako iAPX 186; nie należą do dekodowania docelowego 8086. Sekcja 6-81 opisuje użycie wielu prefiksów jako zależne od procesora i odsyła do manuala konkretnego CPU.
- [Intel, iAPX 86/88/186/188 User's Manual (210912-001, 1985)](https://www.bitsavers.org/components/intel/8086/210912-001_iAPX_86_88_186_188_Users_Manual_1985.pdf): **przeczytano częściowo** 2026-10-24 — potwierdza zestaw instrukcji iAPX 186, w tym PUSH immediate, PUSHA/POPA i przesunięcia z natychmiastowym licznikiem. Używany wyłącznie do rozdzielenia kodowań późniejszej generacji od zachowania celu P80C86A-2.

## Testy

| źródło | licencja | status |
|---|---|---|
| [SingleStepTests/8086](https://github.com/SingleStepTests/8086/tree/e71c68d215a6bb8c356bd4cb3842de3bef345ca9) | MIT; odczytano `LICENSE` | **inwentarz sprawdzony programowo** (2026-10-24): 323 pliki JSON gzip, 646 000 rekordów, 323 pliki MOO gzip, 256 kluczy metadata; próbki 20/plik wyeksportowano poza repo; testów wykonania CPU nie uruchomiono; szczegóły w [README wektorów 8086](../../tests/VibeEmulator.Intel8086.Tests/Vectors/README.md) |
| [Klaus Dormann: 6502_65C02_functional_tests](https://github.com/Klaus2m5/6502_65C02_functional_tests) | **GPL-3.0** (`license.txt`); test dziesiętny Clarka: domena publiczna | **przeczytano**: `readme.txt`, nagłówki i konfiguracja wszystkich czterech testów, listingi funkcjonalnego i 65C02 |
| [christopherpow/nes-test-roms](https://github.com/christopherpow/nes-test-roms) (`nestest.*`, testy Blargga) | nie sprawdzano | **przeczytano**: `nestest.txt`, `nestest.log`, nagłówek `nestest.nes`, readme: `instr_test-v5`, `instr_timing`, `cpu_interrupts_v2`, `branch_timing_tests`, `cpu_reset`, `cpu_timing_test6`, `cpu_dummy_writes`, `nes_instr_test`, `status.txt` |
| [SingleStepTests/65x02](https://github.com/SingleStepTests/65x02) | MIT | commit `2f6980a2d95757486c7bee24355c360e40e2a224`; **sprawdzono programowo** |
| [agn453/ZEXALL](https://github.com/agn453/ZEXALL) | GPL-2.0 | commit `8f71d418bae69a476a5a0e5c6e122c8801b8d9f4`; ZEXDOC i ZEXALL uruchomione przez harness Z80, oba PASS, 67/67 grup, bez `ERROR`; binaria i JSON w ignorowanym `.test-data/` |

## Historia poprawek dokumentacji

- 2026-10-23: recenzja planu 8086 skorygowała format źródła na `*.json.gz` i identyfikatory na `test_hash`/`test_num` na podstawie przypiętych plików. A1 zinwentaryzował wszystkie pliki i rekordy, rozdzielił inwentarz od aktywnych próbek, doprecyzował granice cykli/QS i zależności prefiksów; ograniczenia istniejących kontraktów sprawdzono w kodzie.
- 2026-10-21: plan podawał, że test Dormanna kończy się „kodem sukcesu w `$0000`”. Poprawiono na pętlę `jmp *` pod
  `$3469` (test funkcjonalny) i `$24F1` (test 65C02), z listingów. Lista „reszty 65C02” w planie była niepełna;
  poprawiono na 61 nowych opkodów i 44 niezdefiniowane jako NOP (datasheet WDC).
- 2026-10-21: po recenzji README indeksu skrócono sekcję o stanie implementacji do streszczenia z odnośnikiem do planu,
  dopisano zakres weryfikacji wektorów (próbka w repo, pełne pliki poza nim) i stan `BRK`.

## Co jest zweryfikowane programowo (nie tylko przeczytane)

- Tabele opkodów vs wektory: 256 opkodów × 2 zestawy ([opcodes.md](opcodes.md)).
- Adresy sukcesu Dormanna z listingów: `$3469` (funkcjonalny), `$24F1` (65C02).
- Wzór kolumny `PPU` w `nestest.log` (8991 linii) i użyte w nim opkody nieoficjalne.
- Rozmiary obrazów testowych Dormanna: 65 536 bajtów.
- Nagłówek `nestest.nes`: mapper 0, PRG 16 KB, CHR 8 KB.
