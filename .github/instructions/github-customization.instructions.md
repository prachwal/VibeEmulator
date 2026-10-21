---
applyTo: ".github/instructions/**,.github/prompts/**,.github/skills/**/SKILL.md,.github/hooks/*.json"
description: "Poprawny frontmatter i format plików konfiguracji Copilota w .github: instructions (applyTo), prompty (tools tylko z agentem), skille (name = katalog), hooki (version, timeoutSec)."
---

# Pliki konfiguracji Copilota w `.github/`

Każda reguła poniżej wynika z dokumentacji VS Code / GitHub albo z ostrzeżenia, które edytor faktycznie wyświetlił. Po dodaniu
lub zmianie pliku sprawdź zakładkę **Problems** i uruchom zadanie w **świeżym** czacie lokalnym.

## Instructions (`*.instructions.md`)

- Frontmatter: `applyTo` (glob względem roota repo) i `description`. **Wiele globów oddziel przecinkiem bez spacji**:
  `"src/**/*.cs,tests/**/*.cs"`.
- **Nie używaj `applyTo: "**"`** — plik ładowałby się do każdego zapytania i zjadał kontekst. Zawężaj do ścieżek, których
  dotyczy; `description` ma zawierać słowa-triggery (agent może załadować plik na żądanie, gdy opis pasuje do zadania).
- Plik z samym `description` (bez `applyTo`) nie dołącza się automatycznie; bez obu pól trzeba go dołączyć ręcznie.
- Opcjonalnie `excludeAgent: "code-review"` lub `"cloud-agent"` (GitHub), jeśli reguły nie mają dotyczyć danego agenta.
- Unikaj **sprzecznych** reguł w różnych plikach (często dołączane są razem, np. `csharp.instructions.md` i
  `c64-emulation.instructions.md` dla `StateMachineCpuCore.cs`). Reguły nie są specyficzne dla jednego zadania.
- `copilot-instructions.md` jest dołączany zawsze: trzymaj go krótkim, szczegóły przenoś do plików z `applyTo`.

## Prompty (`*.prompt.md`)

- Pola: `name` (nazwa po `/`), `description`, `argument-hint`, `agent` (`ask`, `agent`, `plan` lub nazwa własnego agenta),
  `model`, `tools`. Uruchamianie: `/nazwa` w czacie lokalnym.
- **`tools` działa tylko z agentem.** W prompcie z `agent: "ask"` atrybut `tools` jest ignorowany i VS Code zgłasza
  ostrzeżenie — nie ustawiaj go; tryb `ask` jest tylko do odczytu. Prompt, który ma edytować pliki lub uruchamiać polecenia,
  używa `agent: "agent"`.
- Prompt opisuje procedurę i format wyniku; nie powtarza reguł z instrukcji, tylko na nie wskazuje.

## Skille (`.github/skills/<nazwa>/SKILL.md`)

- `name` = nazwa katalogu: tylko małe litery, cyfry i myślniki, do 64 znaków. **Niedozwolone znaki w nazwie powodują ciche niezaładowanie skilla.**
- `description` do 1024 znaków: co skill robi **i kiedy go użyć**. Opcjonalnie `argument-hint`, `user-invocable`,
  `disable-model-invocation`.
- Zasoby (skrypty, przykłady) powołuj linkami względnymi z `SKILL.md`; niepowołane pliki nie są ładowane.
- Klucz ustawienia `chat.agentSkillsLocations` jest przestarzały; `.github/skills` jest wykrywany domyślnie.

## Hooki (`.github/hooks/*.json`)

- Format: `{"version": 1, "hooks": {"<Zdarzenie>": [{"type": "command", "command": "…", "timeoutSec": N}]}}`. Zdarzenia
  PascalCase (`PreToolUse`, `PostToolUse`, `Stop`) używają payloadu snake_case. Szczegóły i kontrakt: `hooks-shell.instructions.md`.
