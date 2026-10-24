# Etap A5 — transfery 16-bit, segmenty i stos

Poziom: **II. Zestaw bazowy, część 3** · Zależność: A3/A4

## Zakres

Formy słowne transferu `MOV` między rejestrami, pamięcią, immediate i rejestrami segmentowymi; `PUSH`/`POP`; `PUSHF`/`POPF`; słowny `XCHG`; `LEA` i testy EA wykorzystać z A3; `LDS`/`LES` tylko po potwierdzeniu obecności w 8086 oraz manifestu. Konkretne opcode forms dopisać z tabeli A1. Rejestrów segmentowych nie traktować jak zwykłych rejestrów ogólnych.

## Kolejność

1. Odczyt/zapis word operand w rejestrze i pamięci z zachowaniem dwóch bajtów i kolejności magistrali.
2. Mapowanie rejestrów 16-bit oraz segmentowych; kontrola form niedozwolonych według CPU 8086.
3. `PUSH`/`POP`: kolejność aktualizacji SP i magistrali ustalić z wektorów i źródła producenta.
4. `PUSHF`/`POPF`: maskowanie/zarezerwowane bity flag ustalić źródłem; oddzielić od stanu ALU.
5. `LDS`/`LES`: odczytać wskaźnik i segment z pamięci w wymaganej kolejności; test adresu granicznego.
6. Zaplanować testy skutków zapisu rejestrów segmentowych (w tym granic przyjęcia przerwań po zapisie SS), ze źródłem i scenariuszami ciągłymi domykanymi w A11.
7. Dodać przypadki słów pod adresem parzystym/nieparzystym, pamięci wrap, segment offset `FFFF` i banki A0/BHE.

## Następny podetap przed implementacją

1. Dodać `MOV r/m16,r16` (`89`) i `MOV r16,r/m16` (`8B`) wyłącznie dla `mod=11`, wybierając pierwsze 20 rekordów źródłowych spełniających dokładny opcode i tryb rejestrowy.
2. Fixture upstream pokazuje dwie fazy T-state: `F` z opcode i `S` z ModR/M; kolejka wejściowa długości 5 kończy z 2 bajtami, długości 6 z 3. Potwierdzić to całą filtrowaną grupą przed uznaniem zakresu.
3. Mapowanie r16 współdzielić z B8–BF; nie wykonywać write/read pamięci przez ten sam skrót.
4. Uruchomić pełne filtrowane źródłowe `89`/`8B mod=11` przez runner A2 i porównać stan, kolejkę oraz każdy T-state.

## Kryteria akceptacji

- [ ] Wszystkie słowne formy z manifestu A5 opisane i testowane.
- [x] Częściowe kryterium: `MOV r16,imm16` B8–BF, rejestrowe ModR/M `MOV` 89/8B (`mod=11`) oraz rejestrowe `PUSH`/`POP` 50–5F przechodzą lokalne próbki end-to-end.
- [ ] Zapis word daje prawidłowe bajty RAM i transfery widoczne na obu bankach magistrali.
- [x] Rejestrowe `PUSH`/`POP` 50–5F: końcowy SP, RAM/odczyty i każdy T-state zgodne z fixture; pełny filtr 32 000/32 000. Formy segmentowe, pamięciowe i FLAGS pozostają otwarte.
- [x] Reguła fizycznego adresu segment:offset i 20-bitowe zawijanie ma źródło pierwotne Intel w `docs/cpu/sources.md` i wspólnym decoderze A3; transfery pamięciowe słów nadal nie są pokryte.
- [x] Próbki stosu 50–5F: 320/320; pełny filtr 32 000/32 000. Poprzednie podzestawy: A3–A5 1 120/1 120, pełny B8–BF 16 000/16 000, pełny filtr A5 89/8B 490/490.

## Pułapki

1. Nie zakładać atomowego 16-bitowego accessu: cykle/piny upstream rozstrzygają jak zachowuje się bus przy parzystym i nieparzystym adresie.
2. Wartość word zapisywana do RAM i kolejność bajtów w magistrali to osobne asercje.
3. `PUSH SP` może mieć modelowo istotną kolejność odczytu/zmiany SP; potwierdzić fixture dla konkretnej wersji.
4. Nie używać nowoczesnych reguł reserved flags bez sprawdzenia 8086.

## Dziennik postępu

### 2026-10-23 — `MOV r16, imm16`

Rdzeń obsługuje rejestrowe formy B8–BF (AX/CX/DX/BX/SP/BP/SI/DI). Testy pełnego runnera porównują stan końcowy, RAM, kolejkę i T-states dla 20 rekordów każdego z 8 plików: **160/160**. Jest to podzbiór A5; odczyt/zapis pamięci, rejestry segmentowe, XCHG, stack i banki A0/BHE nie zostały zaimplementowane ani zweryfikowane.

## Następny podetap

Dodać pamięciowe formy `MOV` 16-bit i transfer banków według wektorów 89/8B oraz dekodera effective address A3. Każdą parzystość adresu i liczbę transferów wyprowadzić z fixture; nie zakładać, że forma rejestrowa ma te same fazy. Następnie przejść do MOV segmentów, stosu, PUSHF/POPF i XCHG.

### 2026-10-23 — rejestrowy ModR/M word `MOV` (`89`/`8B`)

Rdzeń obsługuje 89/8B tylko dla `mod=11`, z mapowaniem 16-bitowych rejestrów współdzielonym z B8–BF. Dodano 20 pierwszych rekordów spełniających `bytes[0] == opcode` i `mod=11` dla obu kierunków. Runner porównał stan końcowy, kolejkę prefetch i oba T-states dla 40/40; pełne filtrowane upstream: 89 234/234 i 8B 256/256, łącznie **490/490**, bez skipów i mismatchy. Test projektu: **1 120/1 120**. Formy z pamięcią i transfery segmentowe pozostają otwarte.

## Dziennik postępu

| data | grupa | pass/manifest | wynik próbki | uwagi źródłowe |
|---|---|---|---|---|
| 2026-10-23 | próbka MOV r16,imm16 B8–BF | 8 plików / 160 przypadków | 160/160 | A5 otwarty; pamięć i stos niezaimplementowane |
| 2026-10-23 | pełne MOV r16,imm16 B8–BF | 16 000/16 000; SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9` | wszystkie stany, RAM, kolejka i T-states zgodne; 0 skip/mismatch | pozostałe formy A5 otwarte |
| 2026-10-23 | MOV word ModR/M rejestrowe 89/8B | 40 próbek | 40/40; pełny filtr 490/490 | 89=234 i 8B=256; pamięć nieaktywna |
| 2026-10-24 | rejestrowe PUSH/POP 50–5F | 320 próbek | 320/320; pełny filtr 32 000/32 000 | obejmuje PUSH SP, parzyste/nieparzyste SS:SP i kolejki 5/6; pozostałe formy stosu otwarte |
