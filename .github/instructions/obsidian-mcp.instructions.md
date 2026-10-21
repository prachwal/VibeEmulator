---
applyTo: "tools/obsidian-mcp/**/*.js,tools/obsidian-mcp/package.json"
description: "Serwer MCP nad vaultem Obsidiana (Node, ESM, zero zależności): kontrakt JSON-RPC po stdio, ścieżka vaulta, bezpieczeństwo ścieżek, hermetyczne testy na fixture."
---

# Serwer MCP `tools/obsidian-mcp`

Minimalny serwer MCP (stdio, JSON-RPC 2.0, jedna wiadomość JSON na linię) nad katalogiem `.md`. **Zero zależności npm** — nie
dodawaj paczek. Node w trybie ESM (`"type": "module"`), komentarze po polsku.

## Kontrakt i bezpieczeństwo

- Narzędzia (`vault_info`, `list_notes`, `read_note`, `query_notes`, `create_note`, `append_note`, `search_notes`, `list_tags`)
  zwracają JSON w polu `content[].text`; błąd domenowy to `isError: true`, nie wyjątek protokołu.
- Wszystkie ścieżki z wywołań rozwiązuj przez `resolveInVault`: odrzucaj ścieżki absolutne, `..` i dostęp do `.obsidian`.
  Serwer **nie ma narzędzia kasowania** — zapis jest nieodwracalny z poziomu MCP, więc `create_note` nie nadpisuje bez
  `overwrite: true`.
- Ścieżka vaulta: `OBSIDIAN_VAULT`; pusta lub złożona z białych znaków wartość jest jak nieustawiona (domyślnie
  `~/Obsidian/Vault`), wiodące `~` jest rozwijane. Serwer nie rozwiązuje `realpath`, więc korzeń vaulta może być dowiązaniem
  symbolicznym.
- Stdout wolno pisać **wyłącznie** wiadomościami protokołu; diagnostykę kieruj na stderr.

## Testy (`node tools/obsidian-mcp/test.js`)

- Test jest **hermetyczny**: buduje fixture vault w `os.tmpdir()`, przekazuje go przez `OBSIDIAN_VAULT`, sprząta w `finally`
  i zabija procesy serwera. Nigdy nie czyta ani nie zapisuje prawdziwego vaulta użytkownika (`~/Obsidian`) — asercje o
  zawartości vaulta zależałyby od jego stanu (tak popsuło się to, gdy przybyło notatek).
- Dowód hermetyczności: ten sam wynik z pustym `HOME`, sumy kontrolne plików prawdziwego vaulta bez zmian, brak katalogów
  `obsidian-mcp-test-*` w `/tmp`.
- Nowe zachowanie serwera = nowa asercja na fixture. Test kończy się „Wynik: N passed, M failed” i kodem ≠ 0 przy porażce.
- Zapis do prawdziwego vaulta (wiedza projektowa) robi się osobno, przez klienta MCP, z kopią zapasową zmienianych notatek.
