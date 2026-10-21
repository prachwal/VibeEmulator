# Rejestry, flagi, stos, wektory, stan początkowy

Źródła: [WDC W65C02S datasheet](https://eater.net/datasheets/w65c02s.pdf) (tabela rejestru statusu i wektorów),
[NESdev: CPU interrupts](https://www.nesdev.org/wiki/CPU_interrupts) (wersja `oldid=21632`),
[NESdev: CPU power up state](https://www.nesdev.org/wiki/CPU_power_up_state) (`oldid=22091`),
[pagetable: Internals of BRK/IRQ/NMI/RESET](https://www.pagetable.com/?p=410),
Blargg `cpu_reset` (readme), `nestest.log`, wektory SingleStepTests. Wszystko przeczytane 2026-10-21.

## Rejestry

| rejestr | szerokość | uwagi |
|---|---|---|
| `A` | 8 bit | akumulator |
| `X`, `Y` | 8 bit | indeksy |
| `S` | 8 bit | wskaźnik stosu; stos leży na stronie `$0100`–`$01FF`; odłożenie to zapis pod `$0100 + S`, a **potem** `S--` |
| `PC` | 16 bit | |
| `P` | 8 bit | rejestr statusu, niżej |

W naszym kodzie `CpuState.SP` jest typu `ushort`, ale przechowuje wartość 8-bitową (wektory podają `s` jako 0–255).

## Rejestr statusu `P`

| bit | flaga | uwagi |
|---|---|---|
| 7 | `N` | ujemny: kopia bitu 7 wyniku |
| 6 | `V` | przepełnienie |
| 5 | — | nieużywany; w wektorach SingleStepTests **zawsze 1** |
| 4 | `B` | **nie istnieje fizycznie w CPU.** Pojawia się tylko jako bit 4 bajtu odłożonego na stos: `PHP` i `BRK` odkładają go jako **1**, IRQ i NMI jako **0** |
| 3 | `D` | tryb dziesiętny (2A03 go nie realizuje; patrz [decimal-mode.md](decimal-mode.md)) |
| 2 | `I` | blokada IRQ (NMI jej nie dotyczy) |
| 1 | `Z` | zero |
| 0 | `C` | przeniesienie |

Datasheet WDC zapisuje rejestr jako `N V 1 1 D I Z C`, czyli bity 5 i 4 jako 1 w bajcie odkładanym przez `BRK`/`PHP`.

## Wektory

| sygnał | adres |
|---|---|
| NMI | `$FFFA` (młodszy), `$FFFB` (starszy) |
| RESET | `$FFFC`, `$FFFD` |
| IRQ i BRK | `$FFFE`, `$FFFF` |

## Stan po włączeniu zasilania i po resecie

Dla Ricoh 2A03G (NES); rejestry A, X, Y, PC i S dla klasycznego 6502 podobnie (patrz niżej).

| rejestr | po włączeniu | po resecie |
|---|---|---|
| `A`, `X`, `Y` | 0 | bez zmian |
| `PC` | wartość z `$FFFC`/`$FFFD` | wartość z `$FFFC`/`$FFFD` |
| `S` | `$00 − 3 = $FD` | `S −= 3` |
| `C`, `Z`, `D`, `V`, `N` | 0 | bez zmian |
| `I` | 1 | 1 |

- Blargg `cpu_reset` podaje po włączeniu `P = $34` (taką wartość widzi `PHP`, z bitami 5 i 4), po resecie: flaga `I`
  ustawiona (P OR `$04`), `S` zmniejszone o 3, **nic nie zapisane na stos**, RAM bez zmian.
- `nestest` w trybie automatycznym startuje z `PC = $C000`, `SP = $FD`, `P = $24`, `A = X = Y = 0`, licznik cykli = 7
  (pierwsza linia `nestest.log`).
- Klasyczny 6502 (pagetable): po włączeniu `S` ma wartość 0, a sekwencja resetu trzykrotnie go zmniejsza (fałszywe
  „zapisy” na stos są odczytami), więc kończy na `$FD`.
- Zawartość RAM po włączeniu jest nieokreślona (NESdev); nie polegaj na niej.
- Po resecie flaga `D` jest na NMOS **niezdefiniowana**, a na 65C02 wyczyszczona (Bruce Clark, patrz
  [decimal-mode.md](decimal-mode.md)).

## Co sprawdza nasz kod

`CpuState.Reset()` ustawia `A = X = Y = 0`, `SP = $FD`, `P = Unused | InterruptDisable` (= `$24`), `PC = 0`.
To zgadza się ze startem `nestest` (`P:24 SP:FD`), ale **nie jest** pełną sekwencją resetu (siedem cykli, odczyt
wektora, brak zapisów).

## Nie przeczytano

Strony NESdev „CPU registers”, „CPU status flag behavior” i „CPU instructions” (referencja wpływu każdej instrukcji na
flagi) nie zostały pobrane. Wpływ instrukcji na flagi opisuje [NESdev: Instruction reference](https://www.nesdev.org/wiki/Instruction_reference)
(pobrane, nie przeczytane w całości) i wektory SingleStepTests, które są wzorcem referencyjnym dla zakresu opkodów w repo
(pochodzą z implementacji referencyjnej zgodnej z dokumentacją, a nie z zapisu krzemu).
