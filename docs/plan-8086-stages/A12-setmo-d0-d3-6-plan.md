# A12 — nieudokumentowane `SETMO/SETMOC` (`D0–D3 /6`)

## Cel i branch

Rozszerzyć istniejący cycle-exact rdzeń Intel 8086 o zachowanie przypiętego P80C86A-2 dla grupy przesunięć `D0–D3`, rozszerzenie ModR/M `/6`. Dotychczasowe dekodowanie jawnie pomija `/6`; nie wolno traktować go jak zwykłego `SHL` bez porównania wszystkich pól wektorów i cykli.

Pracować na branchu `A12-8086-pelna-weryfikacja`, bazującym na poprzednim A12. Ten plan jest osobnym commitem przed implementacją; kod, fixture’y i testy dostaną osobny commit implementacyjny, po nim osobny checkpoint dokumentacji. Bieżąca checklista realizacji: [A12-checklista-realizacji.md](A12-checklista-realizacji.md).

## Dowody i zakres

- Źródło wektorów: `SingleStepTests/8086`, commit `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`; pliki `D0.6.json.gz`–`D3.6.json.gz`, po 2 000 rekordów, **8 000 łącznie**. Każdy status w przypiętym inwentarzu to `undocumented`; nazwy testów rozróżniają `setmo` dla count=1 i `setmoc` dla count=CL.
- Podział źródła: D0 989 bez prefiksu / 1 011 z prefiksem; D1 1 001 / 999; D2 1 011 / 989; D3 1 000 / 1 000. Wszystkie cztery pliki zawierają `mod=00/01/10/11`, rejestry i pamięć oraz przypadki z prefiksem ES/CS/SS/DS.
- Przypięty manifest opisuje zachowanie `/6` jako nieudokumentowane, a instrukcja Intel 8086 wymienia `/4` SHL/SAL, `/5` SHR i `/7` SAR, pozostawiając pozycję `/6` pustą. Rozbicie statusów upstream rozróżnia `undocumented` od `undefined`; nie klasyfikować tych rekordów jako nieokreślone bez dowodu.
- Przegląd mikrokodu 8086 opisuje `/6` jako `SETMO` (ustawienie wyniku na wszystkie jedynki); wektory są rozstrzygającym źródłem szczegółów wyniku, FLAGS, zachowania CL=0, pamięci i cyklów. Trzeba odtworzyć te szczegóły z pełnego filtra, nie z samego opisu mnemonic.

Źródła: [Intel 8086 Family User’s Manual, October 1979](https://www.inf.pucrs.br/~calazans/undergrad/orgcomp_EC/mat_microproc/intel-8086_family_Users_Manual.pdf), tabela rozszerzeń grupy shift; [README przypiętego SingleStepTests/8086](https://github.com/SingleStepTests/8086/blob/e71c68d215a6bb8c356bd4cb3842de3bef345ca9/README.md); Ken Shirriff, [Undocumented 8086 instructions explained by the microcode](https://www.righto.com/2023/07/undocumented-8086-instructions.html).

## Inwentaryzacja

| opcode | operacja w rekordach | bez prefiksu | prefiks segmentowy | ModR/M /6 |
|---|---|---:|---:|---|
| `D0` | `SETMO r/m8,1` | 989 | 1 011 | `00/01/10/11` |
| `D1` | `SETMO r/m16,1` | 1 001 | 999 | `00/01/10/11` |
| `D2` | `SETMOC r/m8,CL` | 1 011 | 989 | `00/01/10/11`, w tym 165 rekordów z `CL=0` |
| `D3` | `SETMOC r/m16,CL` | 1 000 | 1 000 | `00/01/10/11`, w tym 148 rekordów z `CL=0` |
| **Razem** | | **4 001** | **3 999** | **8 000** |

Fixture’y wybierać deterministycznie jako pierwsze 20 rekordów źródłowych każdej niepustej kombinacji opcode × prefiks (brak albo ES/CS/SS/DS) × `mod` (00/01/10/11). Przypięte źródło ma **80/80 niepustych grup**, każda zawiera co najmniej 47 rekordów; fixture’y będą obejmowały 1 600 przypadków. Walidator sprawdzi dokładną liczbę na grupę; pozostałe pełne rekordy zostają poza repo.

## Zakres implementacji

1. Rozszerzyć obsługę `/6` wyłącznie dla `D0–D3`; zachować rozróżnienie `SETMO` (count=1) i `SETMOC` (CL), bez zmiany istniejących rozszerzeń `/0–/5,/7`.
2. Odtworzyć wynik rejestru/pamięci oraz FLAGS zgodnie z maskami metadanych, w tym niezmienniki dla `CL=0` i wszystkie wartości CL występujące w źródle.
3. Zachować dekodowanie ModR/M, adresowanie efektywne, domyślny segment DS/SS i override ES/CS/SS/DS dla form pamięciowych.
4. Porównać dokładnie każdy T-state, transfer odczytu/zapisu, adres fizyczny, kolejkę i jej zawartość, początkowy/końcowy IP oraz pełny stan końcowy.
5. Zmiana ALU nie może zmienić zachowania `/4` SHL/SAL. Dodać kontrole regresji dla istniejących shiftów i uruchomić pełne filtry czterech legalnie obsługiwanych rozszerzeń `/0–/5,/7`, jeśli zmiana współdzieli ich gorącą ścieżkę.

## Kolejność pracy i weryfikacja

1. Zatwierdzić ten plan w osobnym commicie na A12 przed zmianami kodu.
2. Wyeksportować lokalne fixture’y z przypiętego checkoutu, zachowując źródłowy JSON bez zmian, `test_num`, hash, ModR/M extension i `flagsMask`; zweryfikować manifest i pokrycie grup.
3. Zaimplementować zachowanie `/6` w tabeli/ALU/dziele wykonania grup shift, z testem xUnit dla wszystkich fixture’ów i czytelnym raportem błędu.
4. W trakcie diagnozy uruchamiać tylko fixture’y i krótkie filtry. Po ustabilizowaniu uruchomić **raz** pełne cztery pliki, 8 000/8 000, oraz **raz** cały korpus 646 000. Kryterium: zero skipów w D0.6–D3.6, mismatchy, timeoutów i błędów parsowania. Oczekiwany wynik pełnego korpusu przy dokładnym odblokowaniu tych wszystkich rekordów: **580 970 passed / 65 030 skipped (89,9%)**; każde odchylenie przeanalizować.
5. Wykonać pełne bramki: build Debug/Release, testy Release, `dotnet format ... --verify-no-changes --severity warn`, OpcodeInfo, walidator manifestu i `git diff --check`. Testy diagnostyczne powtarzać po poprawkach; pełny korpus ponowić tylko jeśli zmiana dotknie już sprawdzonego wyniku albo testy wskażą regresję.
6. Zaktualizować raport pakietu, macierz ISA, inwentarz skipów, README wektorów, plan A12 i checklistę. Commit implementacji oddzielić od planu i checkpointu dokumentacji; nie wypychać.

Przykładowe komendy końcowe (manifest pełnego filtra wygenerować z czterech plików `.6.json.gz`, zachowując metadane i źródłowy SHA):

```bash
dotnet test tests/VibeEmulator.Intel8086.Tests/VibeEmulator.Intel8086.Tests.csproj -c Release --nologo
dotnet tools/Intel8086VectorRunner/bin/Release/net10.0/VibeEmulator.Intel8086VectorRunner.dll \
  --vectors /tmp/vibe-8086-a12-complete/v1 \
  --manifest /tmp/a12-setmo-d0-d3-6-manifest.json \
  --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 \
  --report /tmp/a12-setmo-d0-d3-6-full-filter.json
dotnet tools/Intel8086VectorRunner/bin/Release/net10.0/VibeEmulator.Intel8086VectorRunner.dll \
  --vectors /tmp/vibe-8086-a12-complete/v1 \
  --manifest /tmp/a12-setmo-full-corpus-manifest.json \
  --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 \
  --report /tmp/a12-setmo-full-corpus.json
dotnet build VibeEmulator.slnx -c Debug --nologo
dotnet build VibeEmulator.slnx -c Release --nologo
dotnet test VibeEmulator.slnx -c Release --nologo
dotnet format VibeEmulator.slnx --verify-no-changes --severity warn
dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code
```

## Testy i kryteria

- [x] Lokalne fixture’y: 1 600/1 600 z 80 grup, test xUnit 1/1 passed.
- [x] Pełne `D0.6`–`D3.6`: 8 000/8 000; zero skipów, mismatchy, timeoutów i błędów parsowania.
- [x] Pełny korpus: 580 970/646 000 passed, 65 030 skipped, 0 mismatchy/timeoutów/parse errors (89,9%; +8 000 passed).
- [x] Buildy Debug/Release: zero ostrzeżeń i błędów.
- [x] Pełna solucja Release: 22 241/22 241, w tym Intel8086 19 441/19 441; bez pominiętych testów.
- [x] Formatter verify, OpcodeInfo 512/512, walidator manifestu 1 542 wpisy / 30 199 wskazań, `git diff --check`.
- [x] Commit implementacji po planie `aaed1ed`; hash w raporcie; checkpoint dokumentacji w osobnym commicie.

## Kryterium ukończenia

Wszystkie 8 000 rekordów `D0–D3 /6` przechodzi pełne porównanie; istniejące rozszerzenia shift nie mają regresji; pełny korpus, bramki, klasyfikacja źródeł, checklisty i osobne commity planu/implementacji/checkpointu zawierają aktualne liczby i hashe commitów.
