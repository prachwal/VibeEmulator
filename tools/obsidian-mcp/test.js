#!/usr/bin/env node
// Test dymny serwera MCP: handshake, tools/list, wywołanie narzędzi, walidacja ścieżek.
// Uruchomienie: node test.js   (albo: npm test)

import { spawn } from "node:child_process";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));

function startServer(env = {}) {
    const child = spawn(process.execPath, [path.join(here, "server.js")], {
        env: { ...process.env, ...env },
        stdio: ["pipe", "pipe", "pipe"],
    });

    const pending = new Map();
    const buffer = [];
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
            } else {
                buffer.push(message);
            }
        }
    });

    child.stderr.on("data", (chunk) => process.stderr.write(`[server] ${chunk}`));

    const request = (method, params) =>
        new Promise((resolve, reject) => {
            const id = nextId++;
            pending.set(id, resolve);
            child.stdin.write(`${JSON.stringify({ jsonrpc: "2.0", id, method, params })}\n`);
            setTimeout(() => {
                if (pending.delete(id)) {
                    reject(new Error(`timeout: ${method}`));
                }
            }, 5000);
        });

    const notify = (method, params) => {
        child.stdin.write(`${JSON.stringify({ jsonrpc: "2.0", method, params })}\n`);
    };

    return { child, request, notify };
}

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

const { child, request, notify } = startServer();

console.log("Test serwera obsidian-mcp\n");

const init = await request("initialize", {
    protocolVersion: "2024-11-05",
    capabilities: {},
    clientInfo: { name: "test", version: "1.0.0" },
});

check("initialize zwraca protocolVersion", init.result?.protocolVersion === "2024-11-05");
check("initialize zwraca serverInfo.name", init.result?.serverInfo?.name === "obsidian-mcp");
check("initialize deklaruje tools", init.result?.capabilities?.tools !== undefined);

notify("notifications/initialized", {});

const list = await request("tools/list", {});
const toolNames = (list.result?.tools ?? []).map((tool) => tool.name);

check("tools/list zwraca 8 narzędzi", toolNames.length === 8, `jest ${toolNames.length}`);
check("jest vault_info", toolNames.includes("vault_info"));
check("jest query_notes", toolNames.includes("query_notes"));
check("jest search_notes", toolNames.includes("search_notes"));
check("każde narzędzie ma inputSchema", (list.result?.tools ?? []).every((t) => t.inputSchema?.type === "object"));

const ping = await request("ping", {});
check("ping odpowiada pustym obiektem", JSON.stringify(ping.result) === "{}");

const info = await request("tools/call", { name: "vault_info", arguments: {} });
const infoPayload = JSON.parse(info.result?.content?.[0]?.text ?? "{}");
check("vault_info zwraca ścieżkę vaulta", typeof infoPayload.vault === "string");
check("vault_info nie zgłasza błędu", info.result?.isError === false);
check("vault_info liczy rozkład typów", typeof infoPayload.byType === "object");

const index = await request("tools/call", {
    name: "read_note",
    arguments: { path: "Notes/00-Index" },
});
const indexPayload = JSON.parse(index.result?.content?.[0]?.text ?? "{}");
check("read_note parsuje frontmatter", indexPayload.hasFrontmatter === true);
check("read_note zwraca tytuł z metadanych", indexPayload.metadata?.title === "Index", indexPayload.metadata?.title);
check("read_note zwraca listę tagów", Array.isArray(indexPayload.metadata?.tags));
check("read_note zwraca aliasy", Array.isArray(indexPayload.metadata?.aliases));

const byTag = await request("tools/call", {
    name: "query_notes",
    arguments: { tag: "cpu" },
});
const byTagPayload = JSON.parse(byTag.result?.content?.[0]?.text ?? "{}");
check("query_notes filtruje po tagu", byTagPayload.total >= 2, `total=${byTagPayload.total}`);
check("query_notes zwraca metadane", byTagPayload.notes?.[0]?.metadata?.title !== undefined);

const byStatus = await request("tools/call", {
    name: "query_notes",
    arguments: { status: "in-progress" },
});
const byStatusPayload = JSON.parse(byStatus.result?.content?.[0]?.text ?? "{}");
check("query_notes filtruje po statusie", byStatusPayload.total === 1, `total=${byStatusPayload.total}`);

const withMeta = await request("tools/call", {
    name: "list_notes",
    arguments: { includeMetadata: true, limit: 3 },
});
const withMetaPayload = JSON.parse(withMeta.result?.content?.[0]?.text ?? "{}");
check("list_notes z metadanymi zwraca obiekty", typeof withMetaPayload.notes?.[0] === "object");

const tagsResult = await request("tools/call", { name: "list_tags", arguments: { limit: 200 } });
const tagsPayload = JSON.parse(tagsResult.result?.content?.[0]?.text ?? "{}");
check(
    "vault_info i list_tags podają tę samą liczbę tagów",
    infoPayload.tags === tagsPayload.unique,
    `vault_info=${infoPayload.tags}, list_tags=${tagsPayload.unique}`,
);
check(
    "tagi z bloków kodu i kodu w linii nie są liczone",
    !tagsPayload.tags.some((entry) => entry.tag === "imm" || entry.tag === "cs"),
    JSON.stringify(tagsPayload.tags.filter((entry) => entry.tag === "imm").map((entry) => entry.tag)),
);

const escape = await request("tools/call", {
    name: "read_note",
    arguments: { path: "../../../etc/passwd" },
});
check("ścieżka poza vaultem jest blokowana", escape.result?.isError === true, escape.result?.content?.[0]?.text);

const absolute = await request("tools/call", {
    name: "read_note",
    arguments: { path: "/etc/passwd" },
});
check("ścieżka absolutna jest blokowana", absolute.result?.isError === true);

const dotObsidian = await request("tools/call", {
    name: "read_note",
    arguments: { path: ".obsidian/app.json" },
});
check("katalog .obsidian jest blokowany", dotObsidian.result?.isError === true);

const unknown = await request("tools/call", { name: "nope", arguments: {} });
check("nieznane narzędzie zwraca błąd JSON-RPC", unknown.error?.code === -32602);

const badMethod = await request("nope/method", {});
check("nieznana metoda zwraca -32601", badMethod.error?.code === -32601);

child.stdin.end();

console.log(`\nWynik: ${passed} passed, ${failed} failed`);
process.exit(failed === 0 ? 0 : 1);
