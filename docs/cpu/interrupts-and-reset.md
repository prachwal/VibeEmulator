# Przerwania i reset: IRQ, NMI, BRK, RESET

Źródła (wszystkie przeczytane 2026-10-21):
[NESdev: CPU interrupts](https://www.nesdev.org/wiki/CPU_interrupts) (`oldid=21632`; kopia z Internet Archive, bo serwer
odpowiada 403 na pobieranie skryptem),
[pagetable: Internals of BRK/IRQ/NMI/RESET on a MOS 6502](https://www.pagetable.com/?p=410) (Michael Steil, 2010),
[Bruce Clark: Decimal Mode](http://www.6502.org/tutorials/decimal_mode.html) (sekcja 5),
źródło testu przerwań Dormanna (`6502_interrupt_test.a65`), readme Blargga `cpu_interrupts_v2`.

## Wektory i podsumowanie różnic

| sygnał | wektor | odkłada PC i P | bit `B` w odłożonym `P` |
|---|---|---|---|
| NMI | `$FFFA/$FFFB` | tak | 0 |
| RESET | `$FFFC/$FFFD` | **nie** (cykle istnieją, ale jako odczyty) | — |
| IRQ | `$FFFE/$FFFF` | tak | 0 |
| BRK | `$FFFE/$FFFF` | tak | **1** |

`BRK` to instrukcja **dwubajtowa**: bajt po opkodzie jest odczytywany i odrzucany, a `PC` zwiększa się o 2
(NESdev, datasheet WDC: „The BRK instruction for both the NMOS 6502 and 65C02 is a 2 byte instruction”).
Wszystkie cztery sekwencje to ta sama sekwencja siedmiu cykli; różnią się wektorem, tym, czy faktycznie zapisują, i bitem `B`.

## Sekwencja BRK (siedem cykli)

| # | adres | R/W | opis |
|---|---|---|---|
| 1 | `PC` | R | pobranie opkodu, `PC++` |
| 2 | `PC` | R | odczyt następnego bajtu (odrzucany), `PC++` |
| 3 | `$0100+S` | W | odłożenie PCH, `S--` |
| 4 | `$0100+S` | W | odłożenie PCL, `S--` |
| | | | *w tym miejscu stan linii przerwań decyduje o wektorze* |
| 5 | `$0100+S` | W | odłożenie `P` z `B = 1`, `S--` |
| 6 | `$FFFE` | R | PCL z wektora, ustawienie flagi `I` |
| 7 | `$FFFF` | R | PCH z wektora |

## Sekwencja IRQ i NMI (siedem cykli)

Jak `BRK`, z trzema różnicami: opkod z cyklu 1 jest **odrzucany** (do rejestru instrukcji wstawiane jest `$00`, więc
CPU dekoduje `BRK`), `PC` **nie** zwiększa się w cyklach 1–2, a `P` odkładane jest z `B = 0`.

| # | adres | R/W | opis |
|---|---|---|---|
| 1 | `PC` | R | pobranie opkodu (odrzucony) |
| 2 | `PC` | R | odczyt bajtu (odrzucony), bez `PC++` |
| 3 | `$0100+S` | W | PCH, `S--` |
| 4 | `$0100+S` | W | PCL, `S--` |
| 5 | `$0100+S` | W | `P` z `B = 0`, `S--` |
| 6 | A | R | PCL, ustawienie `I` (A = `$FFFE` dla IRQ, `$FFFA` dla NMI) |
| 7 | A+1 | R | PCH (`$FFFF` lub `$FFFB`) |

## Sekwencja RESET

Ta sama sekwencja, ale cykle 3–5 to **odczyty**, nie zapisy: `S` zmniejsza się trzykrotnie bez zmiany pamięci
(dlatego po resecie `I = 1`, a `S` spada o 3). Wektor `$FFFC/$FFFD` jest czytany w cyklach 6–7.
Ślad z Visual6502 (pagetable) pokazuje dziewięć cykli od zwolnienia resetu do pierwszego pobrania opkodu;
datasheet WDC mówi o sekwencji resetu „lasting seven clock cycles”.
Niektóre egzemplarze 6502 blokują zapisy podczas resetu, inne nie; 2A03 blokuje (NESdev).

## Kiedy CPU „zauważa” przerwanie

- **NMI** jest wrażliwe na zbocze (opadające, aktywny stan niski). Detektor zbocza próbkuje linię w fazie φ2 każdego
  cyklu i podnosi wewnętrzny sygnał, gdy linia przeszła z wysokiej na niską. Sygnał utrzymuje się do obsłużenia NMI.
- **IRQ** jest wrażliwe na poziom: niski poziom w φ2 podnosi sygnał na jeden następny cykl.
- Wyjścia detektorów są próbkowane w ustalonych punktach. Dla większości instrukcji jest to **ostatni cykl instrukcji,
  przed pobraniem następnego opkodu**. Precyzyjnie liczy się stan linii na końcu przedostatniego cyklu.
- Jeśli oczekują i NMI, i IRQ, obsługiwane jest NMI, a IRQ „zapomniane” (zwykle wykryje się je ponownie).
- Sekwencje przerwań same **nie próbkują** przerwań, więc przynajmniej jedna instrukcja procedury obsługi wykona się
  przed kolejnym przerwaniem.

## Opóźnienie po `CLI`, `SEI`, `PLP`; zachowanie `RTI`

- `RTI` zmienia maskowanie IRQ **natychmiast**: przywraca flagę `I` ze stosu przed próbkowaniem. Jeśli IRQ czeka, a `RTI`
  czyści `I`, CPU obsłuży IRQ zaraz po `RTI`.
- `CLI`, `SEI` i `PLP` zmieniają flagę `I` **po** próbkowaniu (jak wszystkie instrukcje dwucyklowe próbkują na końcu
  pierwszego cyklu), więc mogą opóźnić przerwanie o jedną instrukcję. Przykład: przy oczekującym IRQ i ustawionym `I`
  wykonanie `CLI` wykona jeszcze następną instrukcję, dopiero potem wejdzie w procedurę IRQ.
- To opóźnienie dotyczy tylko blokowania; sama wartość flagi jest zmieniona od razu (`CLI` + `PHP` odkłada `I = 0`).
  (Blargg `cpu_interrupts_v2`, `1-cli_latency`: m.in. „Exactly one instruction after CLI should execute before IRQ is
  taken”, „CLI RTI should not allow any IRQs”.)

## Gałęzie a przerwania

Przerwania są zawsze próbkowane przed drugim cyklem gałęzi (pobranie operandu), ale **nie** przed trzecim cyklem
wykonanej gałęzi. Dla wykonanej gałęzi przekraczającej stronę przerwania są próbkowane przed cyklem korekty PCH.
Wykrycie w którymkolwiek z tych punktów (także tylko w pierwszym) uruchamia przerwanie.

## Hijacking (przechwycenie wektora)

Jeśli drugie przerwanie pojawi się bardzo blisko pierwszego, 6502 (a więc i 2A03) może użyć niewłaściwego wektora.
Przykład z NESdev: NMI zgłoszone w pierwszych czterech cyklach `BRK` powoduje, że `BRK` wykonuje się normalnie
(`PC` rośnie, `P` odłożone z `B = 1`), ale skok idzie na **wektor NMI**, nie IRQ/BRK. O wektorze decyduje stan sygnałów
w cyklu 4 (przed odłożeniem `P`). Podobnie NMI może przechwycić IRQ, a IRQ może przechwycić BRK (niewidoczne, bo ten sam
wektor). `BRK` może więc zostać „anulowane” przez NMI; procedura NMI może wykryć to po bicie `B` odłożonym na stosie.
Zachowanie testuje `cpu_interrupts_v2` (`2-nmi_and_brk`, `3-nmi_and_irq`).

## Flaga `D` podczas przerwań

| wariant | `BRK`/IRQ/NMI | RESET |
|---|---|---|
| NMOS 6502 | **nie zmieniają** `D` | `D` niezdefiniowane |
| 65C02, 65816 | czyszczą `D` **po** odłożeniu `P` | czyści `D` |

Źródło: Bruce Clark, sekcja 5. Test przerwań Dormanna ma opcję `D_clear` (0 = nie czyści, NMOS; 1 = czyści, CMOS).

## Test przerwań Dormanna: wymagania harnessu

(Źródło: `6502_interrupt_test.a65`.) Test wymaga **rejestru zwrotnego** podłączonego do linii IRQ i NMI. Konfiguracja domyślna:

| parametr | wartość | znaczenie |
|---|---|---|
| `I_port` | `$BFFC` | adres rejestru zwrotnego |
| `I_ddr` | 0 | brak rejestru kierunku |
| `I_drive` | 1 | otwarty kolektor (0 = totem pole) |
| `IRQ_bit` | 0 | bit sterujący IRQ |
| `NMI_bit` | 1 | bit sterujący NMI (−1 = brak NMI) |
| `I_filter` | `$7F` | bit 7 oznacza „diag stop” w emulatorze |
| `D_clear` | 0 | patrz wyżej |

Wymaga też zapisywalnych wektorów w ROM i możliwej do zmiany flagi `I`. `zero_page = $0A` (6 bajtów),
`data_segment = $0200` (4 bajty), `code_segment = $0400`. Wynik jak w innych testach Dormanna: pętla `PC`.

### Polaryzacja rejestru — potwierdzona przebiegiem (2026-10-22)

Wcześniejszy opis („polaryzacja niezweryfikowana”) można zamknąć: test **przechodzi** przy założeniu, że
**zapisana wartość steruje liniami wprost** — bit 0 = IRQ, bit 1 = NMI, ustawiony bit = żądanie aktywne.
Implementacja w `tools/CpuHarness` (`--watch-register 0xBFFC`) przepisuje bity na `IrqLevel`/`NmiLevel` rdzenia
w każdym cyklu; wynik: pętla sukcesu po 3 016 cyklach (wariant NMOS) i 3 058 (65C02), 24 zapisy rejestru.
Gdyby polaryzacja była odwrotna, test utknąłby w pułapce — a tak się nie stało. Zakres: 1050/1068 instrukcji,
więc potwierdzenie dotyczy przypadków użytych przez ten test, nie wszystkich możliwych sekwencji.

## Nie przeczytano

Opis hijackingu na poziomie tranzystorów ([Visual6502 writeup](http://visual6502.org/), odwołanie z NESdev) oraz
Programming Manual MOS (strona 131, na którą powołuje się pagetable) nie zostały pobrane.
