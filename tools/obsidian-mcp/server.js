#!/usr/bin/env node
// Serwer MCP (stdio, JSON-RPC 2.0) udostępniający vault Obsidiana.
// Zero zależności: protokół zaimplementowany bezpośrednio, bo aplikacja Obsidian
// nie musi działać — vault to zwykły katalog z plikami .md.
//
// Zmienne środowiskowe:
//   OBSIDIAN_VAULT — ścieżka do vaulta (domyślnie ~/Obsidian/Vault)

import { promises as fs } from "node:fs";
import os from "node:os";
import path from "node:path";
import readline from "node:readline";

const VAULT = path.resolve(
    process.env.OBSIDIAN_VAULT ?? path.join(os.homedir(), "Obsidian", "Vault"),
);

const SERVER_INFO = { name: "obsidian-mcp", version: "1.0.0" };
const SUPPORTED_VERSIONS = ["2024-11-05", "2025-03-26", "2025-06-18"];
const NOTE_EXTENSION = ".md";

// --- Bezpieczeństwo ścieżek -------------------------------------------------

function resolveInVault(relativePath) {
    if (typeof relativePath !== "string" || relativePath.trim() === "") {
        throw new Error("Ścieżka notatki jest wymagana.");
    }

    // Odrzucamy ścieżki absolutne i wyjście poza vault, także po normalizacji.
    if (path.isAbsolute(relativePath)) {
        throw new Error(`Ścieżka absolutna jest zabroniona: ${relativePath}`);
    }

    const absolute = path.resolve(VAULT, relativePath);
    const relative = path.relative(VAULT, absolute);

    if (relative.startsWith("..") || path.isAbsolute(relative)) {
        throw new Error(`Ścieżka poza vaultem jest zabroniona: ${relativePath}`);
    }

    if (relative.split(path.sep).includes(".obsidian")) {
        throw new Error("Dostęp do katalogu .obsidian jest zabroniony.");
    }

    return absolute;
}

function ensureExtension(relativePath) {
    return relativePath.endsWith(NOTE_EXTENSION) ? relativePath : `${relativePath}${NOTE_EXTENSION}`;
}

// --- Operacje na vaulcie ----------------------------------------------------

async function walk(dir, output) {
    let entries;

    try {
        entries = await fs.readdir(dir, { withFileTypes: true });
    } catch (error) {
        if (error.code === "ENOENT") {
            return;
        }

        throw error;
    }

    for (const entry of entries) {
        if (entry.name.startsWith(".")) {
            continue;
        }

        const full = path.join(dir, entry.name);

        if (entry.isDirectory()) {
            await walk(full, output);
        } else if (entry.isFile() && entry.name.endsWith(NOTE_EXTENSION)) {
            output.push(path.relative(VAULT, full));
        }
    }
}

async function listNotes() {
    const notes = [];
    await walk(VAULT, notes);
    notes.sort();
    return notes;
}

function countTags(content) {
    const counts = new Map();

    // 1. Frontmatter YAML — tak Obsidian zapisuje tagi notatki.
    const frontmatter = content.match(/^---\r?\n([\s\S]*?)\r?\n---/);

    if (frontmatter) {
        const lines = frontmatter[1].split(/\r?\n/);
        let inTags = false;

        for (const line of lines) {
            const inline = line.match(/^(?:tags|tag)\s*:\s*\[(.*)\]\s*$/);

            if (inline) {
                for (const tag of inline[1].split(",")) {
                    addTag(counts, unquote(tag));
                }

                inTags = false;
                continue;
            }

            if (/^(?:tags|tag)\s*:\s*$/.test(line)) {
                inTags = true;
                continue;
            }

            const listItem = line.match(/^\s*-\s*(.+?)\s*$/);

            if (inTags && listItem) {
                addTag(counts, unquote(listItem[1]));
                continue;
            }

            if (inTags && line.trim() !== "" && !line.startsWith(" ")) {
                inTags = false;
            }
        }
    }

    // 2. Tagi inline: #tag, ale nie w nagłówkach markdown ani w URL-ach.
    //    Najpierw usuwamy bloki kodu i kod w linii — inaczej `LDA #imm` w tabeli
    //    albo `#tag` w przykładzie trafia do listy tagów jako prawdziwy tag.
    const body = frontmatter ? content.slice(frontmatter[0].length) : content;
    const textOnly = stripCode(body);

    for (const match of textOnly.matchAll(/(^|[\s(])#([\p{L}\p{N}_/-]{1,64})/gu)) {
        addTag(counts, match[2]);
    }

    return counts;
}

/// Usuwa bloki kodu (``` i ~~~) oraz kod w linii (`...`), zachowując resztę treści.
function stripCode(markdown) {
    return markdown
        .replace(/^[ \t]*(```|~~~)[\s\S]*?^[ \t]*\1/gm, "")
        .replace(/`[^`\n]*`/g, "");
}

function unquote(value) {
    return value.trim().replace(/^["']|["']$/g, "");
}

function addTag(counts, tag) {
    if (!tag) {
        return;
    }

    counts.set(tag, (counts.get(tag) ?? 0) + 1);
}

// --- Frontmatter YAML (podzbiór) --------------------------------------------
// Obsługuje: skalary, listy w linii [a, b], listy blokowe (- a), cudzysłowy.
// Nie jest to pełny parser YAML — tylko to, co Obsidian zapisuje w notatkach.

function parseFrontmatter(content) {
    const match = content.match(/^---\r?\n([\s\S]*?)\r?\n---\r?\n?/);

    if (!match) {
        return { metadata: {}, body: content, hasFrontmatter: false };
    }

    const metadata = {};
    let currentKey = null;

    for (const raw of match[1].split(/\r?\n/)) {
        const line = raw.replace(/\s+$/, "");

        if (line === "" || line.trimStart().startsWith("#")) {
            continue;
        }

        const listItem = line.match(/^\s+-\s+(.*)$/);

        if (listItem && currentKey) {
            metadata[currentKey].push(stripQuotes(listItem[1]));
            continue;
        }

        const pair = line.match(/^([\w-]+)\s*:\s*(.*)$/);

        if (!pair) {
            continue;
        }

        const [, key, rest] = pair;

        if (rest === "") {
            metadata[key] = [];
            currentKey = key;
            continue;
        }

        metadata[key] = parseInlineValue(rest);
        currentKey = null;
    }

    // Klucz bez wartości i bez listy traktujemy jako null, nie pustą tablicę.
    for (const [key, value] of Object.entries(metadata)) {
        if (Array.isArray(value) && value.length === 0) {
            metadata[key] = null;
        }
    }

    return { metadata, body: content.slice(match[0].length), hasFrontmatter: true };
}

function parseInlineValue(rest) {
    const inline = rest.match(/^\[(.*)\]\s*$/);

    if (inline) {
        const items = inline[1]
            .split(",")
            .map((item) => stripQuotes(item))
            .filter((item) => item !== "");

        return items.length === 0 ? [] : items;
    }

    return stripQuotes(rest);
}

function stripQuotes(value) {
    return value.trim().replace(/^"(.*)"$/, "$1").replace(/^'(.*)'$/, "$1");
}

async function readNoteWithMetadata(relativePath) {
    const absolute = resolveInVault(relativePath);
    const content = await fs.readFile(absolute, "utf8");
    const parsed = parseFrontmatter(content);

    return { absolute, content, ...parsed };
}

/// Zbiera metadane wszystkich notatek — używane przez query_notes i vault_info.
async function collectMetadata() {
    const notes = await listNotes();
    const entries = [];

    for (const note of notes) {
        const { metadata } = await readNoteWithMetadata(note);
        entries.push({ path: note, metadata });
    }

    return entries;
}

function asArray(value) {
    if (value === null || value === undefined) {
        return [];
    }

    return Array.isArray(value) ? value : [value];
}

function matchesFilters(metadata, filters) {
    const { tag, type, status, project } = filters;

    if (tag && !asArray(metadata.tags).includes(tag) && !asArray(metadata.tag).includes(tag)) {
        return false;
    }

    if (type && metadata.type !== type) {
        return false;
    }

    if (status && metadata.status !== status) {
        return false;
    }

    if (project && metadata.project !== project) {
        return false;
    }

    return true;
}

function countBy(entries, key) {
    const counts = {};

    for (const { metadata } of entries) {
        const value = metadata[key];

        if (typeof value === "string" && value !== "") {
            counts[value] = (counts[value] ?? 0) + 1;
        }
    }

    return counts;
}

/// Zbiera wszystkie tagi (frontmatter + inline). Jedno źródło prawdy dla
/// vault_info i list_tags — inaczej oba narzędzia podają różne liczby.
async function collectTagCounts() {
    const notes = await listNotes();
    const totals = new Map();

    for (const note of notes) {
        const content = await fs.readFile(path.join(VAULT, note), "utf8");

        for (const [tag, count] of countTags(content)) {
            totals.set(tag, (totals.get(tag) ?? 0) + count);
        }
    }

    return totals;
}

async function searchNotes(query, useRegex, limit) {
    const notes = await listNotes();
    const matcher = useRegex
        ? new RegExp(query, "u")
        : null;
    const needle = query.toLowerCase();
    const hits = [];

    for (const note of notes) {
        const absolute = path.join(VAULT, note);
        const content = await fs.readFile(absolute, "utf8");
        const lines = content.split("\n");
        const matches = [];

        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];
            const matched = matcher ? matcher.test(line) : line.toLowerCase().includes(needle);

            if (matched) {
                matches.push({ line: i + 1, text: line.slice(0, 200) });
            }
        }

        if (matches.length > 0) {
            hits.push({ note, matches: matches.slice(0, 10) });
        }

        if (hits.length >= limit) {
            break;
        }
    }

    return hits;
}

// --- Definicje narzędzi -----------------------------------------------------

const TOOLS = [
    {
        name: "vault_info",
        description: "Zwraca ścieżkę vaulta, liczbę notatek, tagów oraz rozkład typów i statusów z frontmattera.",
        inputSchema: { type: "object", properties: {}, additionalProperties: false },
        handler: async () => {
            const entries = await collectMetadata();
            const tags = await collectTagCounts();

            return {
                vault: VAULT,
                notes: entries.length,
                tags: tags.size,
                withFrontmatter: entries.filter((entry) => Object.keys(entry.metadata).length > 0).length,
                byType: countBy(entries, "type"),
                byStatus: countBy(entries, "status"),
                byProject: countBy(entries, "project"),
                exists: await fs
                    .stat(VAULT)
                    .then((s) => s.isDirectory())
                    .catch(() => false),
            };
        },
    },
    {
        name: "list_notes",
        description: "Listuje notatki .md w vaulcie. Opcjonalnie filtruje po fragmencie ścieżki i dołącza metadane.",
        inputSchema: {
            type: "object",
            properties: {
                filter: { type: "string", description: "Filtr podciągu ścieżki, np. 'Notes/'." },
                includeMetadata: {
                    type: "boolean",
                    default: false,
                    description: "Zwróć sparsowany frontmatter razem ze ścieżką.",
                },
                limit: { type: "integer", minimum: 1, maximum: 1000, default: 200 },
            },
            additionalProperties: false,
        },
        handler: async ({ filter, includeMetadata = false, limit = 200 }) => {
            let notes = await listNotes();

            if (filter) {
                notes = notes.filter((note) => note.includes(filter));
            }

            notes = notes.slice(0, limit);

            if (!includeMetadata) {
                return { total: notes.length, notes };
            }

            const items = [];

            for (const note of notes) {
                const { metadata } = await readNoteWithMetadata(note);
                items.push({ path: note, metadata });
            }

            return { total: items.length, notes: items };
        },
    },
    {
        name: "read_note",
        description: "Czyta treść notatki oraz jej metadane z frontmattera YAML.",
        inputSchema: {
            type: "object",
            properties: { path: { type: "string", description: "np. Notes/00-Index.md" } },
            required: ["path"],
            additionalProperties: false,
        },
        handler: async (args) => {
            const { content, metadata, hasFrontmatter } = await readNoteWithMetadata(
                ensureExtension(args.path),
            );

            return {
                path: args.path,
                bytes: Buffer.byteLength(content, "utf8"),
                hasFrontmatter,
                metadata,
                content,
            };
        },
    },
    {
        name: "query_notes",
        description:
            "Filtruje notatki po metadanych: tag, type, status, project. Zwraca ścieżkę i frontmatter.",
        inputSchema: {
            type: "object",
            properties: {
                tag: { type: "string", description: "Tag z frontmattera lub inline, np. 'cpu'." },
                type: { type: "string", description: "np. note, index, reference." },
                status: { type: "string", description: "np. draft, in-progress, stable." },
                project: { type: "string" },
                limit: { type: "integer", minimum: 1, maximum: 500, default: 100 },
            },
            additionalProperties: false,
        },
        handler: async ({ tag, type, status, project, limit = 100 }) => {
            const entries = await collectMetadata();
            const matches = entries.filter((entry) => matchesFilters(entry.metadata, { tag, type, status, project }));

            return {
                filters: { tag: tag ?? null, type: type ?? null, status: status ?? null, project: project ?? null },
                total: matches.length,
                notes: matches.slice(0, limit),
            };
        },
    },
    {
        name: "create_note",
        description: "Tworzy notatkę. Domyślnie nie nadpisuje istniejącej.",
        inputSchema: {
            type: "object",
            properties: {
                path: { type: "string", description: "np. Notes/Nowa-notatka" },
                content: { type: "string" },
                overwrite: { type: "boolean", default: false },
            },
            required: ["path", "content"],
            additionalProperties: false,
        },
        handler: async (args) => {
            const relative = ensureExtension(args.path);
            const absolute = resolveInVault(relative);
            const overwrite = args.overwrite === true;

            await fs.mkdir(path.dirname(absolute), { recursive: true });

            if (!overwrite) {
                const exists = await fs
                    .access(absolute)
                    .then(() => true)
                    .catch(() => false);

                if (exists) {
                    throw new Error(`Notatka już istnieje: ${relative}. Użyj overwrite=true, aby nadpisać.`);
                }
            }

            await fs.writeFile(absolute, args.content, "utf8");

            return { path: relative, bytes: Buffer.byteLength(args.content, "utf8"), overwrite };
        },
    },
    {
        name: "append_note",
        description: "Dopisuje treść na końcu notatki (tworzy ją, jeśli nie istnieje).",
        inputSchema: {
            type: "object",
            properties: {
                path: { type: "string" },
                content: { type: "string" },
            },
            required: ["path", "content"],
            additionalProperties: false,
        },
        handler: async (args) => {
            const relative = ensureExtension(args.path);
            const absolute = resolveInVault(relative);

            await fs.mkdir(path.dirname(absolute), { recursive: true });
            await fs.appendFile(absolute, args.content, "utf8");

            return { path: relative, appended: Buffer.byteLength(args.content, "utf8") };
        },
    },
    {
        name: "search_notes",
        description: "Przeszukuje notatki (podciąg lub wyrażenie regularne) i zwraca trafienia z numerami linii.",
        inputSchema: {
            type: "object",
            properties: {
                query: { type: "string" },
                regex: { type: "boolean", default: false },
                limit: { type: "integer", minimum: 1, maximum: 100, default: 20 },
            },
            required: ["query"],
            additionalProperties: false,
        },
        handler: async ({ query, regex = false, limit = 20 }) => {
            const hits = await searchNotes(query, regex === true, limit);
            return { query, regex, notes: hits.length, hits };
        },
    },
    {
        name: "list_tags",
        description: "Zwraca tagi (#tag) z całego vaulta wraz z liczbą wystąpień.",
        inputSchema: {
            type: "object",
            properties: { limit: { type: "integer", minimum: 1, maximum: 500, default: 100 } },
            additionalProperties: false,
        },
        handler: async ({ limit = 100 }) => {
            const notes = await listNotes();
            const totals = new Map();

            for (const note of notes) {
                const content = await fs.readFile(path.join(VAULT, note), "utf8");

                for (const [tag, count] of countTags(content)) {
                    totals.set(tag, (totals.get(tag) ?? 0) + count);
                }
            }

            const tags = [...totals.entries()]
                .sort((a, b) => b[1] - a[1])
                .slice(0, limit)
                .map(([tag, count]) => ({ tag, count }));

            return { unique: totals.size, tags };
        },
    },
];

const TOOL_BY_NAME = new Map(TOOLS.map((tool) => [tool.name, tool]));

// --- Protokół JSON-RPC ------------------------------------------------------

function send(message) {
    process.stdout.write(`${JSON.stringify(message)}\n`);
}

function reply(id, result) {
    send({ jsonrpc: "2.0", id, result });
}

function replyError(id, code, message) {
    send({ jsonrpc: "2.0", id, error: { code, message } });
}

function textResult(payload) {
    return {
        content: [{ type: "text", text: JSON.stringify(payload, null, 2) }],
        isError: false,
    };
}

async function handle(message) {
    const { id, method, params } = message;

    // Powiadomienia (bez id) nie wymagają odpowiedzi.
    if (id === undefined || id === null) {
        return;
    }

    switch (method) {
        case "initialize": {
            const requested = params?.protocolVersion;
            const protocolVersion = SUPPORTED_VERSIONS.includes(requested)
                ? requested
                : SUPPORTED_VERSIONS[SUPPORTED_VERSIONS.length - 1];

            reply(id, {
                protocolVersion,
                capabilities: { tools: { listChanged: false } },
                serverInfo: SERVER_INFO,
                instructions: `Vault Obsidiana: ${VAULT}`,
            });
            return;
        }

        case "ping":
            reply(id, {});
            return;

        case "tools/list":
            reply(id, {
                tools: TOOLS.map(({ name, description, inputSchema }) => ({
                    name,
                    description,
                    inputSchema,
                })),
            });
            return;

        case "tools/call": {
            const name = params?.name;
            const tool = TOOL_BY_NAME.get(name);

            if (!tool) {
                replyError(id, -32602, `Nieznane narzędzie: ${name}`);
                return;
            }

            try {
                const result = await tool.handler(params?.arguments ?? {});
                reply(id, textResult(result));
            } catch (error) {
                // Błąd narzędzia zgłaszamy jako wynik isError, zgodnie z MCP.
                reply(id, {
                    content: [{ type: "text", text: error.message }],
                    isError: true,
                });
            }

            return;
        }

        default:
            replyError(id, -32601, `Nieobsługiwana metoda: ${method}`);
    }
}

const input = readline.createInterface({ input: process.stdin, crlfDelay: Infinity });

// Licznik zapytań w locie. Bez niego zamknięcie stdin ubija proces przed
// wygenerowaniem odpowiedzi (istotne przy wywołaniu z pipe'a, nie w VS Code).
let inFlight = 0;
let inputClosed = false;

// Żądania wykonujemy SEKOWENCYJNIE. Bez tego dwa komunikaty przysłane w jednym
// buforze (np. create + append) wykonują się współbieżnie i wyniki zależą od
// tego, który I/O skończy się pierwszy — create potrafi zobaczyć plik utworzony
// przez append i zwrócić fałszywy błąd "już istnieje".
let queue = Promise.resolve();

function exitWhenDrained() {
    if (inputClosed && inFlight === 0) {
        process.exit(0);
    }
}

input.on("line", (line) => {
    const trimmed = line.trim();

    if (trimmed === "") {
        return;
    }

    let message;

    try {
        message = JSON.parse(trimmed);
    } catch {
        replyError(null, -32700, "Niepoprawny JSON.");
        return;
    }

    inFlight++;

    queue = queue
        .then(() => handle(message))
        .catch((error) => {
            replyError(message.id ?? null, -32603, error.message);
        })
        .finally(() => {
            inFlight--;
            exitWhenDrained();
        });
});

input.on("close", () => {
    inputClosed = true;
    exitWhenDrained();
});

process.on("uncaughtException", (error) => {
    process.stderr.write(`obsidian-mcp: ${error.message}\n`);
    process.exit(1);
});
