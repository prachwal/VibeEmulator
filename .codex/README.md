# Hooki Codex

Konfiguracja projektu: [hooks.json](hooks.json). Wykorzystuje wspólne skrypty z
`.github/hooks/` przez [adapter](hooks/copilot_adapter.py); konfiguracja Copilota pozostaje bez zmian.
Wymagania: Python 3, Bash, git i dotnet, a dla wspólnych skryptów jq lub Python 3.

| Zdarzenie | Działanie |
|---|---|
| `PreToolUse` (`Bash`, `apply_patch`) | guard niebezpiecznych poleceń |
| `PostToolUse` (`Bash`, `apply_patch`) | formatowanie wskazanych plików C#; adapter rozpoznaje nagłówki łatki |
| `Stop` | wspólny build Debug i weryfikacja formatowania, z ochroną przed pętlą |

Adapter zamienia decyzję `ask` na `deny` z powodem, ponieważ Codex obecnie nie obsługuje `ask` w `PreToolUse`.
Zgoda w rozmowie nie przełącza tej blokady automatycznie; operację można wykonać ręcznie.
Guard analizuje tekst znanych pól poleceń; nie jest pełnym parserem powłoki ani łatki.
Formatter rozpoznaje ścieżki przekazane osobno i nagłówki `apply_patch`; nie wyciąga plików z dowolnych poleceń Bash.

## Aktywacja

Otwórz projekt w nowej sesji Codex. Projektowa warstwa `.codex/` musi być zaufana.
W CLI użyj `/hooks`, przejrzyj i zatwierdź trzy definicje. Zmiana definicji wymaga ponownego przeglądu.
Pliki konfiguracji same nie oznaczają, że hooki już działają w istniejącej sesji.
Źródło: [oficjalna dokumentacja OpenAI — Hooks](https://learn.chatgpt.com/docs/hooks).

## Weryfikacja

```bash
PYTHONDONTWRITEBYTECODE=1 python3 .codex/hooks/test_adapter.py
for t in .github/hooks/test-*.sh; do bash "$t"; done
```

Testy adaptera nie budują ani nie formatują roboczego repozytorium.
