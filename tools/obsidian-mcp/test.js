#!/usr/bin/env node
// Test dymny serwera MCP: handshake, tools/list, wywołanie narzędzi, walidacja ścieżek,
// rozwiązywanie OBSIDIAN_VAULT.
// Uruchomienie: node test.js   (albo: npm test)
//
// Test jest HERMETYCZNY: nigdy nie czyta prawdziwego vaulta (~/Obsidian/Vault).
// Na starcie buduje FIXTURE VAULT w katalogu tymczasowym, a na końcu (także po błędzie)
// usuwa katalog tymczasowy i zabija wszystkie uruchomione procesy serwera.
//
// FIXTURE VAULT (stan, na który wskazują asercje):
//   Notes/00-Index.md              type: index,     status: stable,      tags: moc, emulator
//   Notes/01-Setup-projektu.md     type: note,      status: stable,      tags: emulator, cpu
//   Notes/02-Cykle-i-dokladnosc.md type: note,      status: stable,      tags: cpu, timing
//   Notes/03-Model-CPU.md          type: note,      status: in-progress, tags: cpu; inline #model;
//                                  #imm w bloku kodu i `#cs` w kodzie w linii (NIE są tagami)
//   Notes/04-Wektory.md            type: reference, status: stable,      tags (lista blokowa): reference, testy
//   Notes/sub/05-Zagniezdzona.md   type: note,      status: stable,      tags: sub-tag
//   Notes/06-Bez-frontmattera.md   bez frontmattera; inline #emulator, #nowy
//   .obsidian/app.json             ukryty katalog — dostęp musi być zablokowany
//
// Razem: 7 notatek (6 z frontmatterem), statusy: 5× stable, 1× in-progress,
// 9 unikalnych tagów, 3 notatki z tagiem cpu.

import { spawn } from "node:child_process";
import { promises as fs } from "node:fs";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));

// Wszystkie uruchomione serwery — zabijane w `finally` i przy wyjściu procesu.
const running = new Set();

process.on("exit", () => {
    for (const child of running) {
        child.kill();
    }
});

// Wartość `undefined` w `env` oznacza: usuń zmienną z otoczenia procesu serwera.
function startServer(env = {}) {
    const merged = { ...process.env, ...env };

    for (const [key, value] of Object.entries(env)) {
        if (value === undefined) {
            delete merged[key];
        }
    }

    const child = spawn(process.execPath, [path.join(here, "server.js")], {
        env: merged,
        stdio: ["pipe", "pipe", "pipe"],
    });

    running.add(child);
    child.on("exit", () => running.delete(child));

    const pending = new Map();
    let nextId = 1;

    child.stdout.setEncoding("utf8");
    child.stderr.setEncoding("utf8");

    let partial = "";

    child.stdout.on("data", (chunk) => {
        partial += chunk;
        let index;

        while ((index = partial.indexOf("\n")) >= 0) {
            const line = partial.slice(0, index).trim();
            partial = partial.slice(index + 1);

            if (line === "") {
                continue;
            }

            const message = JSON.parse(line);
            const resolver = pending.get(message.id);

            if (resolver) {
                pending.delete(message.id);
                resolver(message);
            }
        }
    });

    child.stderr.on("data", (chunk) => process.stderr.write(`[server] ${chunk}`));

    const request = (method, params) =>
        new Promise((resolve, reject) => {
            const id = nextId++;
            const timer = setTimeout(() => {
                if (pending.delete(id)) {
                    reject(new Error(`timeout: ${method}`));
                }
            }, 5000);

            pending.set(id, (message) => {
                clearTimeout(timer);
                resolve(message);
            });
            child.stdin.write(`${JSON.stringify({ jsonrpc: "2.0", id, method, params })}\n`);
        });

    const notify = (method, params) => {
        child.stdin.write(`${JSON.stringify({ jsonrpc: "2.0", method, params })}\n`);
    };

    const stop = () => {
        if (child.exitCode === null && child.signalCode === null) {
            child.kill();
        }
    };

    return { child, request, notify, stop };
}

// --- Fixture ----------------------------------------------------------------

const text = (...lines) => `${lines.join("\n")}\n`;

const FIXTURE_FILES = {
    "Notes/00-Index.md": text(
        "---",
        "title: Index",
        "aliases:",
        "  - MOC",
        "  - mapa wiedzy",
        "tags:",
        "  - moc",
        "  - emulator",
        "type: index",
        "status: stable",
        "project: vibe-emulator",
        "created: 2026-10-20",
        "updated: 2026-10-21",
        "confidence: measured",
        "sources:",
        '  - "pomiary z tej sesji"',
        "related:",
        '  - "[[01-Setup-projektu]]"',
        '  - "[[03-Model-CPU]]"',
        "---",
        "",
        "# Index",
        "",
        "Mapa wiedzy. Zob. [[01-Setup-projektu]] i [[03-Model-CPU]].",
    ),
    "Notes/01-Setup-projektu.md": text(
        "---",
        "title: Setup projektu",
        "aliases: [Setup]",
        "tags: [emulator, cpu]",
        "type: note",
        "status: stable",
        "project: vibe-emulator",
        "created: 2026-10-20",
        "updated: 2026-10-20",
        "confidence: measured",
        "sources:",
        '  - "dokumentacja projektu"',
        "related:",
        '  - "[[00-Index]]"',
        "---",
        "",
        "# Setup projektu",
        "",
        "Kroki przygotowania. Wracamy do [[00-Index]].",
    ),
    "Notes/02-Cykle-i-dokladnosc.md": text(
        "---",
        "title: Cykle i dokładność",
        "tags: [cpu, timing]",
        "type: note",
        "status: stable",
        "project: vibe-emulator",
        "created: 2026-10-20",
        "updated: 2026-10-20",
        "confidence: measured",
        "---",
        "",
        "# Cykle i dokładność",
        "",
        "Liczenie cykli. Powiązane: [[03-Model-CPU]].",
    ),
    "Notes/03-Model-CPU.md": text(
        "---",
        "title: Model CPU",
        "tags: [cpu]",
        "type: note",
        "status: in-progress",
        "project: vibe-emulator",
        "created: 2026-10-21",
        "updated: 2026-10-21",
        "confidence: hypothesis",
        "---",
        "",
        "# Model CPU",
        "",
        "Model cykli, #model, opisany w [[02-Cykle-i-dokladnosc]].",
        "",
        "```asm",
        "LDA #imm",
        "```",
        "",
        "Kod w linii `#cs` nie jest tagiem.",
    ),
    "Notes/04-Wektory.md": text(
        "---",
        "title: Wektory",
        "tags:",
        "  - reference",
        "  - testy",
        "type: reference",
        "status: stable",
        "project: vibe-emulator",
        "created: 2026-10-20",
        "updated: 2026-10-20",
        "confidence: measured",
        "---",
        "",
        "# Wektory",
        "",
        "Zestawy SingleStepTests, zob. [[03-Model-CPU]].",
    ),
    "Notes/sub/05-Zagniezdzona.md": text(
        "---",
        "title: Zagnieżdżona",
        "tags: [sub-tag]",
        "type: note",
        "status: stable",
        "created: 2026-10-20",
        "updated: 2026-10-20",
        "confidence: measured",
        "---",
        "",
        "# Zagnieżdżona",
        "",
        "Notatka w podkatalogu.",
    ),
    "Notes/06-Bez-frontmattera.md": text(
        "# Bez frontmattera",
        "",
        "Notatka bez frontmattera. Tagi inline: #emulator #nowy. Link: [[00-Index]].",
    ),
    ".obsidian/app.json": text('{ "theme": "obsidian" }'),
};

async function buildFixture(vault) {
    for (const [relative, content] of Object.entries(FIXTURE_FILES)) {
        const absolute = path.join(vault, relative);
        await fs.mkdir(path.dirname(absolute), { recursive: true });
        await fs.writeFile(absolute, content, "utf8");
    }
}

// --- Asercje ----------------------------------------------------------------

let passed = 0;
let failed = 0;

function check(name, condition, detail = "") {
    if (condition) {
        console.log(`  PASS  ${name}`);
        passed++;
    } else {
        console.log(`  FAIL  ${name}${detail ? ` — ${detail}` : ""}`);
        failed++;
    }
}

function payloadOf(response) {
    return JSON.parse(response.result?.content?.[0]?.text ?? "{}");
}

// Jednorazowe uruchomienie serwera z danym środowiskiem: zwraca odpowiedź vault_info.
async function vaultInfoWith(env) {
    const server = startServer(env);

    try {
        const response = await server.request("tools/call", { name: "vault_info", arguments: {} });
        return { response, payload: payloadOf(response) };
    } finally {
        server.stop();
    }
}

// --- Testy ------------------------------------------------------------------

async function runMainChecks(vault) {
    const server = startServer({ OBSIDIAN_VAULT: vault });

    try {
        console.log("Test serwera obsidian-mcp (fixture)\n");

        const init = await server.request("initialize", {
            protocolVersion: "2024-11-05",
            capabilities: {},
            clientInfo: { name: "test", version: "1.0.0" },
        });

        check("initialize zwraca protocolVersion", init.result?.protocolVersion === "2024-11-05");
        check("initialize zwraca serverInfo.name", init.result?.serverInfo?.name === "obsidian-mcp");
        check("initialize deklaruje tools", init.result?.capabilities?.tools !== undefined);

        server.notify("notifications/initialized", {});

        const list = await server.request("tools/list", {});
        const toolNames = (list.result?.tools ?? []).map((tool) => tool.name);

        check("tools/list zwraca 8 narzędzi", toolNames.length === 8, `jest ${toolNames.length}`);
        check("jest vault_info", toolNames.includes("vault_info"));
        check("jest query_notes", toolNames.includes("query_notes"));
        check("jest search_notes", toolNames.includes("search_notes"));
        check("każde narzędzie ma inputSchema", (list.result?.tools ?? []).every((t) => t.inputSchema?.type === "object"));

        const ping = await server.request("ping", {});
        check("ping odpowiada pustym obiektem", JSON.stringify(ping.result) === "{}");

        const info = await server.request("tools/call", { name: "vault_info", arguments: {} });
        const infoPayload = payloadOf(info);
        check("vault_info zwraca ścieżkę vaulta", typeof infoPayload.vault === "string");
        check("vault_info wskazuje fixture (OBSIDIAN_VAULT)", infoPayload.vault === vault, `vault=${infoPayload.vault}`);
        check("vault_info nie zgłasza błędu", info.result?.isError === false);
        check("vault_info: fixture istnieje", infoPayload.exists === true);
        check("vault_info liczy 7 notatek w fixture", infoPayload.notes === 7, `notes=${infoPayload.notes}`);
        check("vault_info liczy rozkład typów", typeof infoPayload.byType === "object");
        check(
            "vault_info: statusy 5× stable i 1× in-progress",
            infoPayload.byStatus?.stable === 5 && infoPayload.byStatus?.["in-progress"] === 1,
            JSON.stringify(infoPayload.byStatus),
        );

        const index = await server.request("tools/call", {
            name: "read_note",
            arguments: { path: "Notes/00-Index" },
        });
        const indexPayload = payloadOf(index);
        check("read_note parsuje frontmatter", indexPayload.hasFrontmatter === true);
        check("read_note zwraca tytuł z metadanych", indexPayload.metadata?.title === "Index", indexPayload.metadata?.title);
        check("read_note zwraca listę tagów", Array.isArray(indexPayload.metadata?.tags));
        check("read_note zwraca aliasy", Array.isArray(indexPayload.metadata?.aliases));

        // fixture: tag cpu ma 01-Setup-projektu, 02-Cykle-i-dokladnosc, 03-Model-CPU
        const byTag = await server.request("tools/call", {
            name: "query_notes",
            arguments: { tag: "cpu" },
        });
        const byTagPayload = payloadOf(byTag);
        check("query_notes filtruje po tagu", byTagPayload.total === 3, `total=${byTagPayload.total}`);
        check("query_notes zwraca metadane", byTagPayload.notes?.[0]?.metadata?.title !== undefined);

        // fixture: dokładnie jedna notatka in-progress — 03-Model-CPU
        const byStatus = await server.request("tools/call", {
            name: "query_notes",
            arguments: { status: "in-progress" },
        });
        const byStatusPayload = payloadOf(byStatus);
        check("query_notes filtruje po statusie", byStatusPayload.total === 1, `total=${byStatusPayload.total}`);
        check(
            "query_notes po statusie zwraca 03-Model-CPU",
            byStatusPayload.notes?.[0]?.path === "Notes/03-Model-CPU.md",
            byStatusPayload.notes?.[0]?.path,
        );

        const withMeta = await server.request("tools/call", {
            name: "list_notes",
            arguments: { includeMetadata: true, limit: 3 },
        });
        const withMetaPayload = payloadOf(withMeta);
        check("list_notes z metadanymi zwraca obiekty", typeof withMetaPayload.notes?.[0] === "object");

        const tagsResult = await server.request("tools/call", { name: "list_tags", arguments: { limit: 200 } });
        const tagsPayload = payloadOf(tagsResult);
        check(
            "vault_info i list_tags podają tę samą liczbę tagów",
            infoPayload.tags === tagsPayload.unique,
            `vault_info=${infoPayload.tags}, list_tags=${tagsPayload.unique}`,
        );
        // fixture: moc, emulator, cpu, timing, model, reference, testy, sub-tag, nowy
        check("list_tags zwraca 9 unikalnych tagów fixture", tagsPayload.unique === 9, `unique=${tagsPayload.unique}`);
        check(
            "tagi z bloków kodu i kodu w linii nie są liczone",
            !tagsPayload.tags.some((entry) => entry.tag === "imm" || entry.tag === "cs"),
            JSON.stringify(tagsPayload.tags.filter((entry) => entry.tag === "imm").map((entry) => entry.tag)),
        );

        const escape = await server.request("tools/call", {
            name: "read_note",
            arguments: { path: "../../../etc/passwd" },
        });
        check("ścieżka poza vaultem jest blokowana", escape.result?.isError === true, escape.result?.content?.[0]?.text);

        const absolute = await server.request("tools/call", {
            name: "read_note",
            arguments: { path: "/etc/passwd" },
        });
        check("ścieżka absolutna jest blokowana", absolute.result?.isError === true);

        // Plik .obsidian/app.json istnieje w fixture — blokada ma działać, a nie brak pliku.
        const dotObsidian = await server.request("tools/call", {
            name: "read_note",
            arguments: { path: ".obsidian/app.json" },
        });
        check("katalog .obsidian jest blokowany", dotObsidian.result?.isError === true);
        check(
            "blokada .obsidian zgłasza zakaz dostępu",
            dotObsidian.result?.content?.[0]?.text?.includes("zabroniony") === true,
            dotObsidian.result?.content?.[0]?.text,
        );

        const unknown = await server.request("tools/call", { name: "nope", arguments: {} });
        check("nieznane narzędzie zwraca błąd JSON-RPC", unknown.error?.code === -32602);

        const badMethod = await server.request("nope/method", {});
        check("nieznana metoda zwraca -32601", badMethod.error?.code === -32601);
    } finally {
        server.stop();
    }
}

// Rozwiązywanie OBSIDIAN_VAULT. HOME wskazuje pusty katalog tymczasowy, więc żadne
// prawdziwe ~/Obsidian nie jest brane pod uwagę; vault nie istnieje — exists:false.
async function runVaultPathChecks(home) {
    console.log("\nRozwiązywanie OBSIDIAN_VAULT\n");

    const defaultVault = path.join(home, "Obsidian", "Vault");

    for (const [label, value] of [
        ["nieustawiona", undefined],
        ["pusta", ""],
        ["same białe znaki", "   "],
    ]) {
        const { response, payload } = await vaultInfoWith({ HOME: home, OBSIDIAN_VAULT: value });

        check(`OBSIDIAN_VAULT ${label} → domyślna ścieżka ~/Obsidian/Vault`, payload.vault === defaultVault, payload.vault);
        check(
            `OBSIDIAN_VAULT ${label}: exists:false bez wyjątku`,
            response.result?.isError === false && payload.exists === false,
            `isError=${response.result?.isError}, exists=${payload.exists}`,
        );
    }

    const tilde = await vaultInfoWith({ HOME: home, OBSIDIAN_VAULT: "~/Nazwa" });
    check(
        "OBSIDIAN_VAULT=~/Nazwa rozwija się do <home>/Nazwa",
        tilde.payload.vault === path.join(home, "Nazwa"),
        tilde.payload.vault,
    );

    const tildeOnly = await vaultInfoWith({ HOME: home, OBSIDIAN_VAULT: "~" });
    check("OBSIDIAN_VAULT=~ rozwija się do <home>", tildeOnly.payload.vault === home, tildeOnly.payload.vault);
}

async function main() {
    const root = await fs.mkdtemp(path.join(os.tmpdir(), "obsidian-mcp-test-"));

    try {
        const vault = path.join(root, "vault");
        const home = path.join(root, "home");

        await buildFixture(vault);
        await fs.mkdir(home, { recursive: true });

        await runMainChecks(vault);
        await runVaultPathChecks(home);
    } finally {
        for (const child of running) {
            child.kill();
        }

        await fs.rm(root, { recursive: true, force: true });
    }
}

try {
    await main();
} catch (error) {
    console.log(`  FAIL  test przerwany wyjątkiem — ${error.message}`);
    failed++;
}

console.log(`\nWynik: ${passed} passed, ${failed} failed`);
process.exitCode = failed === 0 ? 0 : 1;
