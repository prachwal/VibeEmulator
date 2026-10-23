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

## Kryteria akceptacji

- [ ] Wszystkie słowne formy z manifestu A5 opisane i testowane.
- [ ] Zapis word daje prawidłowe bajty RAM i transfery widoczne na obu bankach magistrali.
- [ ] `PUSH`/`POP` stan końcowy SP, RAM/odczyty i kolejność cykli zgodne z fixture.
- [ ] Segment:offset i zawijanie fizycznego adresu mają cytowane źródło pierwotne.
- [ ] Próbki A3–A5 green; wynik pełnych wektorów jawnie podany albo nieuruchomiony.

## Pułapki

1. Nie zakładać atomowego 16-bitowego accessu: cykle/piny upstream rozstrzygają jak zachowuje się bus przy parzystym i nieparzystym adresie.
2. Wartość word zapisywana do RAM i kolejność bajtów w magistrali to osobne asercje.
3. `PUSH SP` może mieć modelowo istotną kolejność odczytu/zmiany SP; potwierdzić fixture dla konkretnej wersji.
4. Nie używać nowoczesnych reguł reserved flags bez sprawdzenia 8086.

## Dziennik postępu

### 2026-10-23 — `MOV r16, imm16`

Rdzeń obsługuje rejestrowe formy B8–BF (AX/CX/DX/BX/SP/BP/SI/DI). Testy pełnego runnera porównują stan końcowy, RAM, kolejkę i T-states dla 20 rekordów każdego z 8 plików: **160/160**. Jest to podzbiór A5; odczyt/zapis pamięci, rejestry segmentowe, XCHG, stack i banki A0/BHE nie zostały zaimplementowane ani zweryfikowane.

## Dziennik postępu

| data | grupa | pass/manifest | wynik próbki | uwagi źródłowe |
|---|---|---|---|---|
| 2026-10-23 | MOV r16,imm16 B8–BF | 8 plików / 160 przypadków | 160/160 | A5 otwarty; pełne wektory nieuruchomione |
