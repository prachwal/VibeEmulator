# Plan podetapu A12: `MOV` rejestr segmentowy ↔ `r/m16` (`8C`, `8E`)

Status: implementacja ukończona i przechodzi pełne przypięte wektory; końcowe bramki i commit w toku.

## Cel i zakres

Na branchu `A12-8086-pelna-weryfikacja` zaimplementować:

- `8C /r`: przeniesienie rejestru segmentowego do rejestru ogólnego lub pamięci;
- `8E /r`: przeniesienie rejestru ogólnego lub pamięci do rejestru segmentowego.

Uwzględnić wszystkie formy ModR/M i prefiksy segmentowe obecne w przypiętym
źródle. Pamięciowe transfery słowa muszą zachować kolejność bajtów, BHE, parzystość
adresu, zawijanie adresu fizycznego, kolejkę prefetch i każdy cykl T-state.
FLAGS nie mogą się zmienić. Nie zakładać zachowania dla przerwań po `MOV SS`, bo
przypięty zestaw nie dostarcza do tego dowodu.

## Źródła i inwentaryzacja

- SingleStepTests 8086, SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`;
  pełne pliki `v1/8C.json.gz` i `v1/8E.json.gz`, po 2 000 rekordów.
- `8C`: 978 rekordów bez prefiksu, 1 022 z pojedynczym prefiksem segmentowym;
  wszystkie wartości pola `reg` 0–7 występują. Inwentaryzacja: 207/264/240/270/
  243/249/267/260 rekordów dla `/0`–`/7`.
- `8E`: 1 005 rekordów bez prefiksu, 995 z prefiksem segmentowym; występują pola
  `/0` (320), `/2` (340), `/3` (336), `/4` (342), `/6` (316), `/7` (346).
  `/1` i `/5` nie występują. Nie redukować pola `reg` do legalnych nazw bez
  porównania wszystkich wektorów — przypięte dane obejmują aliasy kodowań.
- W pełnym korpusie przed implementacją wszystkie 4 000 rekordów są skipami jako
  niezaimplementowane opkody.
- Źródłowe prefiksy są pojedyncze: ES/CS/SS/DS. Uwzględnić domyślny segment
  adresowania efektywnego (DS/SS zależnie od `r/m`) i jego nadpisanie.

## Kolejność prac

1. Zachować w raporcie inwentaryzację pól ModR/M, segmentów, parzystości adresów,
   długości cykli i początkowego stanu kolejki; wybrać przypadki z hashami do
   diagnostyki rejestru, pamięci i prefiksów.
2. Dodać po 20 pierwszych, niezmienionych rekordów upstream z `8C` i `8E` do
   aktywnego manifestu i README.
3. Zaimplementować kierunek rejestrowy obu opkodów, mapowanie segmentów i
   zweryfikowane zachowanie aliasów pola `reg`.
4. Zaimplementować pamięciowe odczyty/zapisy słowa z transferem wyrównanym i
   nieparzystym, BHE oraz właściwymi stanami magistrali.
5. Dodać obsługę pojedynczego prefiksu segmentowego oraz źródłowe profile cykli
   i okien prefetch. Profile wolno kodować jako dane dopiero po wyprowadzeniu ich
   z przypiętych rekordów.
6. Uruchomić najpierw próbkę lokalną, potem pełne filtry obu plików; poprawiać
   rdzeń przy pierwszej różnicy i powtarzać pełny filtr do 4 000/4 000.
7. Ponowić cały korpus 646 000, porównać zmianę skipów z 4 000, sprawdzić
   niezmienność `87`, `86`, `88`/`89`/`8A`/`8B` i zaktualizować macierz ISA,
   raport oraz checklisty A12.

## Testy i kryteria

- [x] Inwentaryzacja źródła zawiera liczniki ModR/M, prefiksów, długości cykli
  i dokładne identyfikatory/hashy rekordów brzegowych.
- [x] Fixture 8C/8E to pierwsze 20 źródłowych rekordów; testy porównują rejestry,
  segmenty, RAM, kolejkę i wszystkie T-state.
- [x] Pełny `8C.json.gz`: 2 000 passed, 0 skipped, mismatchy, timeoutów i parse
  errors.
- [x] Pełny `8E.json.gz`: 2 000 passed, 0 skipped, mismatchy, timeoutów i parse
  errors.
- [x] Pełny korpus: 646 000 rekordów, passed/skipped zmienione o +4 000/−4 000,
  mismatch/timeout/parse errors = 0.
- [x] Intel8086 13 431/13 431 i pełna solucja Release 16 231/16 231 przechodzą;
  build Debug/Release bez ostrzeżeń; formatter verify, manifest i OpcodeInfo zielone;
  formatter, manifest i OpcodeInfo są zielone.
- [x] Plan i implementacja są osobnymi commitami na branchu A12 (`a0fe0cb` oraz commit implementacji tego podetapu); nie pushowano.

## Polecenia weryfikacyjne

Dowody wykonanych przebiegów: lokalne testy Intel8086 13 431/13 431; pełne filtry
`8C`/`8E` 4 000/4 000; pełny korpus 646 000: 511 519 passed, 134 481 skipped,
0 mismatchy/timeoutów/błędów parsowania. Raport: `A12-mov-segment-report.json`.

```bash
dotnet test tests/VibeEmulator.Intel8086.Tests/VibeEmulator.Intel8086.Tests.csproj -c Release --nologo
python3 -c 'import json; p="tests/VibeEmulator.Intel8086.Tests/Vectors/manifest.json"; m=json.load(open(p)); m["active"]=[{"file":"8C.json","opcode":"8C","records":2000},{"file":"8E.json","opcode":"8E","records":2000}]; json.dump(m,open("/tmp/vibe-a12-8c-8e-manifest.json","w"),indent=2)'
dotnet tools/Intel8086VectorRunner/bin/Release/net10.0/VibeEmulator.Intel8086VectorRunner.dll \
  --vectors /tmp/vibe-8086-a12-complete/v1 --manifest /tmp/vibe-a12-8c-8e-manifest.json \
  --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 \
  --report /tmp/vibe-a12-8c-8e-full-report.json
dotnet tools/Intel8086VectorRunner/bin/Release/net10.0/VibeEmulator.Intel8086VectorRunner.dll \
  --vectors /tmp/vibe-8086-a12-complete/v1 --manifest /tmp/vibe-8086-a12-full-current-manifest.json \
  --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 \
  --report /tmp/vibe-a12-full-after-8c-8e.json
dotnet build VibeEmulator.slnx -c Debug --nologo
dotnet build VibeEmulator.slnx -c Release --nologo
dotnet test VibeEmulator.slnx -c Release --nologo
dotnet format VibeEmulator.slnx --verify-no-changes --severity warn
python3 tools/Intel8086VectorTools/verify_manifest.py --vectors tests/VibeEmulator.Intel8086.Tests/Vectors
dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code
```
