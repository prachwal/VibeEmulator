# AGENTS.md

Emulator C64 / rodziny 6502 w C#/.NET 10, cycle-exact. Dokumentacja i komentarze są po polsku. Pełne reguły dla agentów:
[.github/copilot-instructions.md](.github/copilot-instructions.md) oraz pliki w [.github/instructions/](.github/instructions/)
(ładowane według ścieżki). Dla Claude Code: [CLAUDE.md](CLAUDE.md).

## Polecenia (definicja ukończenia)

```bash
dotnet build VibeEmulator.slnx -c Debug --nologo
dotnet build VibeEmulator.slnx -c Release --nologo
dotnet test VibeEmulator.slnx -c Release --nologo
dotnet format VibeEmulator.slnx --verify-no-changes --severity warn
dotnet tools/OpcodeInfo/bin/Release/net10.0/VibeEmulator.OpcodeInfo.dll --verify-code   # adnotacje vs CSV
for t in .github/hooks/test-*.sh; do bash "$t"; done   # po zmianie hooków
node tools/obsidian-mcp/test.js                          # po zmianie serwera MCP
```

## Trzy reguły krytyczne

1. **Zachowanie CPU nie z pamięci.** Opkody, cykle i dummy read bierz z wektorów SingleStepTests i `docs/cpu/`. Nieuruchomionych
   pełnych wektorów nie wolno przedstawiać jako weryfikacji.
2. **Warstwy.** `Memory` i `Cpu.Abstractions` nie znają 6502, `Mos6502` nie zna C64; różnice wariantów to dane (`CpuVariant`), nie `if`.
3. **Zielony build, zero ostrzeżeń** (`TreatWarningsAsErrors`). Nie commituj ani nie wypychaj bez wyraźnej prośby; recenzja pliku
   to sam raport, bez pytania o naniesienie poprawek.

## Dokumentacja

- Plan i stan realizacji: [docs/plan-c64-cycle-exact.md](docs/plan-c64-cycle-exact.md)
- Sprzęt, opkody, cykle, testy: [docs/cpu/README.md](docs/cpu/README.md)

## Skille projektu

Skille znajdują się w `.agents/skills/`. Przed wykonaniem pasującego zadania przeczytaj odpowiedni `SKILL.md`
i wskazane w nim zasoby.

## Hooki Codex

Konfiguracja: [.codex/hooks.json](.codex/hooks.json). Wspólne skrypty Copilota są uruchamiane przez adapter;
aktywacja i ograniczenia: [.codex/README.md](.codex/README.md).
