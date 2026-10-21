---
applyTo: ".github/workflows/*.yml"
description: "CI GitHub Actions: workflow musi powtarzać definicję ukończenia z copilot-instructions (build Release, testy, format, hooki, MCP), używać SDK z global.json i nie osłabiać bramek jakości."
---

# CI (`.github/workflows/`)

## Zakres

- `ci.yml` wykonuje bramki z „Definicji ukończenia” w `.github/copilot-instructions.md`: `dotnet restore`, build
  **Release**, `dotnet test -c Release`, `dotnet format --verify-no-changes --severity warn`, `--verify-code` katalogu
  opkodów (adnotacje `[Opcode]` vs `docs/cpu/opcodes/*.csv`), testy hooków (`.github/hooks/test-*.sh`) i
  `node tools/obsidian-mcp/test.js`. Build `Debug` nie jest powtarzany w CI — zostaje
  w obowiązkach lokalnych.
- Nowa bramka lokalna (kolejne polecenie w definicji ukończenia, nowy katalog testów) musi trafić także tutaj — CI jest
  egzekwatorem tej samej definicji, nie osobnym zestawem zasad.

## Czego nie robić

- Nie osłabiaj bramek: `|| true`, `continue-on-error` przy build/test/format, pominięte kroki ani `VERIFY_BUILD_SKIP=1`.
- Nie wpisuj wersji SDK w YAML — `actions/setup-dotnet` bierze `global.json` przez `global-json-file`.
- Nie zmieniaj wersji `actions/*` w zmianie dotyczącej czego innego; to osobna, uzasadniona zmiana.
- Nie dodawaj sekretów ani `permissions: write-all`; wystarcza `contents: read`.

## Cache i kolejność

- Cache NuGet jest kluczowany hashem `global.json`, `Directory.Packages.props`, `Directory.Build.props` i `**/*.csproj`
  (repo nie ma `packages.lock.json`). Zmiana tych plików musi unieważniać cache — nie usuwaj ich z klucza.
- Kolejność: checkout → SDK → cache → Node → restore → build → test → format → katalog opkodów → hooki → MCP. Kroki zależne od zbudowanego
  kodu używają `--no-build`/`--no-restore` dopiero po odpowiednim kroku.
- Krok katalogu opkodów uruchamia zbudowaną DLL (`tools/OpcodeInfo/bin/Release/net10.0/…`), a nie `dotnet run` —
  dzięki temu wynik nie zależy od ponownego builda, a kod wyjścia `1` zatrzymuje workflow.
- `concurrency` anuluje przebieg na tym samym ref; nie usuwaj tego bez powodu (podwójne przebiegi na PR).

## Weryfikacja

- Zmianę w CI sprawdź lokalnie tymi samymi poleceniami (z `-c Release`) i uruchom na PR; zielony lokalny build nie
  zastępuje przebiegu workflow.
