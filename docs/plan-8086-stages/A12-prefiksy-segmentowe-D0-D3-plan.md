# A12 — prefiksy segmentowe dla `D0–D3`

## Cel i punkt bazowy

Rozszerzyć istniejące, nieprefiksowane instrukcje shift/rotate (`D0–D3`) o wszystkie zaobserwowane prefiksy segmentowe dla rozszerzeń ModR/M `/0–/5,/7`. Nie zmieniać semantyki i cykli form nieprefiksowanych. Implementacja ma odpowiadać każdemu T-state, kolejce, stanowi, odczytowi/zapisowi pamięci i flagom przypiętych wektorów.

Źródło: `SingleStepTests/8086`, SHA `e71c68d215a6bb8c356bd4cb3842de3bef345ca9`. W `v1/` jest 8 plików rozszerzeń dla każdego z czterech opcode’ów; każdy ma 2 000 rekordów, razem 64 000. W całej grupie 31 905 rekordów zaczyna się od prefiksu segmentowego; 3 999 z nich to `/6`, więc zakresem implementacji jest **27 906** rekordów `/0–/5,/7`. Pozostałe **4 001** nieprefiksowanych `/6` są skipami już raportowanymi pod osobnym powodem; prefiksowane `/6` są częścią ogólnej kategorii skipów segment-prefiksowanych.

Punkt bazowy z pełnego korpusu: 527 519/646 000 passed, 118 481 skipped, 0 mismatched, 0 timeoutów, 0 błędów parsowania (**81,7%**). Przypięta macierz pokazuje pełne nieprefiksowane pokrycie istniejących legalnych form: register-direct 7 027/7 027 i memory 21 067/21 067.

## Inwentaryzacja i próbki lokalne

- Dla każdej kombinacji prefiksu ES/CS/SS/DS × opcode `D0–D3` × rozszerzenie `/0–/5,/7` wybrać po 20 rekordów rejestrowych i 20 pamięciowych z przypiętego źródła: 224 grupy × 20 = **4 480 fixture’ów**.
- Zachować rekordy źródłowe bez modyfikacji i w zwartej postaci; manifest zawiera SHA, ścieżkę źródłową, `test_num`, rozszerzenie, prefiks i maski flag z metadanych upstream.
- Testy repo uruchamiają wszystkie fixture’y przez istniejący `Intel8086VectorTests`; nie dodawać osobnej implementacji komparatora ani zależności.
- Nie dodawać `/6` do aktywnego manifestu implementacji shift/rotate bez osobnego rozstrzygnięcia jego zachowania na 8086.

## Zakres implementacji

1. W dekoderze prefiksów segmentowych dopuścić `D0–D3`, zachowując ich bajt prefiksu i prawidłowy indeks ModR/M/displacement w kolejce.
2. Dla form pamięciowych użyć `_segmentOverride` przy wyznaczaniu adresu efektywnego i statusu segmentu magistrali; domyślny segment DS/SS oraz override ES/CS/SS/DS wynikają z wektorów.
3. Dla form rejestrowych sprawdzić obserwowane zachowanie prefiksu (zmiana liczby cykli, faz fetch i kolejki); nie zakładać, że prefiks jest ignorowany.
4. Dla `D0/D1` zachować count=1, dla `D2/D3` użyć CL, w tym wartości CL=0 i całego źródłowego zakresu. Zachować maski OF/AF przypisane konkretnemu opcode’owi i rozszerzeniu.
5. Ustalić profil odczytu, RMW, zapisu, prefetch i końcowej kolejki ze wszystkich 27 906 przypiętych rekordów. Profile tablicowe dodawać tylko wtedy, gdy konieczne do odtworzenia cyklu i bez pochodzenia wektorowego ich nie wymyślać.
6. Zostawić `/6` jako osobny przypadek do klasyfikacji; ta implementacja nie ma obniżać liczby skipów przez zgadywanie działania nieudokumentowanego rozszerzenia.

## Kolejność i komendy

1. Plan ma osobny commit na branchu `A12-8086-pelna-weryfikacja`.
2. Eksportować 4 480 lokalnych fixture’ów z checkoutu pinu; zweryfikować liczby na kombinację i zgodność rekordów z `test_num`/`test_hash` źródła.
3. Wdrożyć prefiks i profilowanie cykli; po zakończeniu zmian uruchomić lokalne fixture’y (szybki test diagnostyczny).
4. Uruchomić pełny filtr 64 000 rekordów `D0–D3` (32 pliki). Kryterium: 56 000 obsługiwanych rekordów passed, 8 000 jawnych `/6` skipped, zero mismatchy/timeoutów/parse errors; potwierdzić podział prefiksowany/nieprefiksowany w raporcie.
5. Uruchomić pełny korpus 646 000 raz po ustabilizowaniu pakietu. Oczekiwany wynik, jeżeli dokładnie 27 906 przypadków przejdzie bez regresji: 555 425 passed, 90 575 skipped, zero mismatchy/timeoutów/parse errors. Różny wynik wymaga analizy, a nie dopasowania kryterium.
6. Uruchomić testy Intel8086 i pełną solucję Release, buildy Debug/Release, formatter, walidator manifestu i OpcodeInfo. Zaktualizować ten plan, główny plan A12, checklistę dalszych prac, README wektorów i raport z nowym `% = passed / 646 000`.
7. Zatwierdzić implementację osobno od commitu planu. Nie wypychać.

```bash
dotnet test tests/VibeEmulator.Intel8086.Tests/VibeEmulator.Intel8086.Tests.csproj -c Release --nologo
dotnet run --project tools/Intel8086VectorRunner/VibeEmulator.Intel8086VectorRunner.csproj -c Release -- \
  --vectors /tmp/vibe-8086-a12-complete/v1 \
  --manifest /tmp/vibe-a12-d0-d3-manifest.json \
  --source-sha e71c68d215a6bb8c356bd4cb3842de3bef345ca9 \
  --report /tmp/vibe-a12-d0-d3-full-report.json
dotnet build VibeEmulator.slnx -c Debug --nologo
dotnet build VibeEmulator.slnx -c Release --nologo
dotnet test VibeEmulator.slnx -c Release --nologo
dotnet format VibeEmulator.slnx --verify-no-changes --severity warn
dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code
```

Manifest filtra 32 pliki `D0.0`–`D3.7` z `/tmp/vibe-8086-a12-full-current-manifest.json`, zachowując ich maski flag. Źródłowe pliki `.json.gz` pozostają nietknięte poza repo. Raport runnera może zwrócić kod 1, bo poprawne kryterium zawiera 8 000 jawnych `/6` skipów; oceniać statusy i manifest, nie sam kod wyjścia.

## Testy i kryteria

- [ ] Fixture’y mają po 20 rejestrowych i 20 pamięciowych rekordów dla każdej z 224 grup, 4 480 łącznie; dane zgodne z pinem i manifestem.
- [ ] Testy lokalne wszystkich 4 480 fixture’ów przechodzą, w tym stan, RAM, FLAGS z maskami źródła, kolejka, każdy T-state i dostęp segmentu.
- [ ] Pełny filtr D0–D3: 64 000 rekordów, 56 000 passed, 8 000 skipów wyłącznie dla `/6`, 0 mismatchy, timeoutów i błędów parsowania.
- [ ] Pełny korpus: wynik wzrasta z 527 519 o dokładnie 27 906, a skipy maleją o tyle samo; zero nowych mismatchy/timeoutów/parse errors.
- [ ] Buildy Debug/Release, testy Intel8086 i całej solucji Release, formatter, manifest i OpcodeInfo zielone; raporty i checklisty zaktualizowane.
- [ ] Plan i implementacja zapisane osobnymi commitami na branchu A12; branch nie został wypchnięty.

