#!/usr/bin/env python3
"""Konwersja składni as65 (Klaus Dormann) na ca65, żeby dało się zasemblować testy Dormanna.

Dlaczego: testy przerwań i dziesiętny nie mają gotowych binariów, a dołączony do repozytorium źródłowego
asembler `as65` to 32-bitowy plik ELF dla Linuksa (`ld-linux.so.2`), którego nie ma na typowej instalacji
amd64. `ca65`/`ld65` są dostępne, ale ich składnia różni się od as65.

Ten skrypt zawiera **wyłącznie reguły translacji składni** — nie zawiera kodu testów (te są GPL-3.0 i
zostają poza repozytorium). Skrypt jest naszym kodem pomocniczym, używanym w etapie O planu.

Obsługiwane konstrukcje (tyle używa `6502_interrupt_test.a65`):
  org X / data / bss / code   → .segment "ZEROPAGE"|"DATA"|"CODE"|"VECTORS"
  if / else / endif           → .if / .else / .endif (także z wcięciem)
  NAME macro [param] / endm   → .macro NAME[, param] / .endmacro
  \\1, \\2 (parametry pozycyjne) → nazwy parametrów (arg1, arg2… gdy brak deklaracji)
  NAME\\?   (etykieta lokalna)  → NAME + .local NAME w makrze
  equ / =                     → .set (pozwala na powtórną definicję przy rozwinięciu makra)
  dw / db / ds                → .word / .byte / .res
  etykieta bez dwukropka      → etykieta z dwukropkiem (osobna linia)
  end symbol, noopt           → usunięte

Użycie:
  python3 as65_to_ca65.py <źródło.a65> <wynik.s>

Linkowanie: `ld65 -C <cfg>` z segmentami ZEROPAGE/DATA/CODE/VECTORS pod adresami z konfiguracji testu
($000A/$0200/$0400/$FFFA dla testu przerwania; $0000/$0400 dla testu dziesiętnego).
"""
import re
import sys

SEGMENT_BY_ORG = {
    'zero_page': 'ZEROPAGE',
    'data_segment': 'DATA',
    'code_segment': 'CODE',
}


def convert(text: str) -> str:
    out: list[str] = []

    for raw in text.split('\n'):
        stripped = raw.strip()
        indented = bool(raw[:1].isspace())

        if not stripped or stripped.startswith(';'):
            out.append(raw)
            continue

        # --- dyrektywy strukturalne -------------------------------------
        match = re.match(r'^org\s+(.+?)\s*(?:;.*)?$', stripped)
        if match:
            target = match.group(1).strip()
            out.append(f'.segment "{SEGMENT_BY_ORG.get(target, "VECTORS")}"')
            continue

        match = re.match(r'^if\s+(.+?)\s*(?:;.*)?$', stripped)
        if match:
            out.append(f'.if {match.group(1)}')
            continue

        if re.match(r'^else\b', stripped):
            out.append('.else')
            continue

        if re.match(r'^endif\b', stripped):
            out.append('.endif')
            continue

        match = re.match(r'^(\w+)\s+macro\s*(?:(\w+))?\s*(?:;.*)?$', stripped)
        if match:
            params = f' {match.group(2)}' if match.group(2) else ''
            out.append(f'.macro {match.group(1)}{params}')
            continue

        if re.match(r'^endm\b', stripped):
            out.append('.endmacro')
            continue

        if re.match(r'^noopt\b', stripped):
            continue

        if re.match(r'^end\s', stripped):
            continue

        if re.match(r'^data\s*(?:;.*)?$', stripped):
            out.append('.segment "DATA"')
            continue

        if re.match(r'^code\s*(?:;.*)?$', stripped):
            out.append('.segment "CODE"')
            continue

        if re.match(r'^bss\s*(?:;.*)?$', stripped):
            out.append('.segment "BSS"')
            continue

        # --- instrukcje i dane ------------------------------------------
        raw = raw.replace('!=', '<>')
        raw = raw.replace('skip\\?', ':')
        raw = re.sub(r'^(\s+|\w+\s+)dw\s+', lambda m: m.group(1) + '.word ', raw)
        raw = re.sub(r'^(\s+|\w+\s+)db\s+', lambda m: m.group(1) + '.byte ', raw)
        # as65 „equ" i „=" to definicje, które mogą się powtarzać przy każdym rozwinięciu makra —
        # ca65 wymaga wtedy .set (jak w porcie amb5l).
        raw = re.sub(r'^(\w+)(\s+)equ\s+', r'\1\2.set ', raw)
        raw = re.sub(r'^(\w+)(\s*)=\s+', r'\1\2.set ', raw)
        raw = re.sub(r'^(\w+)(\s+)ds\s+', r'\1\2.res ', raw)
        raw = re.sub(r'^(\s+)ds\s+', r'\1.res ', raw)
        raw = raw.replace('include "report.i65"', '.include "report.i65"')

        # definicja stałej (equ/=) nie jest etykietą
        if re.match(r'^[A-Za-z_]\w*\s*\.set\s', raw):
            out.append(raw)
            continue

        # etykieta i instrukcja w jednej linii: ca65 wymaga osobnych linii
        match = re.match(r'^([A-Za-z_][A-Za-z0-9_]*):(\s+)(\S.*)$', raw)
        if match:
            out.append(f'{match.group(1)}:')
            out.append(f'    {match.group(3)}')
            continue

        # etykieta bez dwukropka: w kolumnie 1, przed instrukcją/dyrektywą
        if not indented:
            match = re.match(r'^([A-Za-z_][A-Za-z0-9_]*)(\s*)(.*)$', raw)
            if match and match.group(3):
                out.append(f'{match.group(1)}:')
                out.append(f'    {match.group(3)}')
                continue
            if match:
                out.append(f'{match.group(1)}:')
                continue

        out.append(raw)

    return add_macro_parameters(out).replace('!=', '<>')


def add_macro_parameters(lines: list[str]) -> str:
    """Parametry makr: as65 używa \\1, \\2 (pozycyjnie); ca65 wymaga nazw.

    Dla makra z zadeklarowanym parametrem \\1 zastępujemy jego nazwą, dla makra bez deklaracji
    dopisujemy nazwy ``arg1``, ``arg2``… i zastępujemy odpowiednio. Etykiety ``NAME\\?`` stają się
    ``NAME`` + ``.local NAME`` (odpowiednik unikalności per rozwinięcie).
    """
    result: list[str] = []
    start = -1
    params: list[str] = []

    for line in lines:
        if line.startswith('.macro '):
            header = [token.strip(',') for token in line.split()]
            header = [token for token in header if token]
            params = header[2:]
            start = len(result)
            result.append(line)
            continue

        if line.startswith('.endmacro') and start >= 0:
            body_lines = result[start + 1:]
            used = set()
            for body_line in body_lines:
                used.update(int(n) for n in re.findall(r'\\(\d+)', body_line))

            for index in sorted(used):
                while len(params) < index:
                    params.append(f'arg{len(params) + 1}')

            local_names: list[str] = []
            for offset, body_line in enumerate(body_lines):
                for index in sorted(used, reverse=True):
                    body_line = body_line.replace('\\' + str(index), params[index - 1])
                for local in re.findall(r'([A-Za-z_]\w*)\\\?', body_line):
                    if local not in local_names:
                        local_names.append(local)
                body_line = re.sub(r'([A-Za-z_]\w*)\\\?', r'\1', body_line)
                body_line = body_line.replace('\\?', '')
                result[start + 1 + offset] = body_line

            if local_names:
                result.insert(start + 1, '        .local ' + ', '.join(local_names))

            name = result[start].split()[1].strip(',')
            result[start] = f'.macro {name} ' + ', '.join(params) if params else f'.macro {name}'
            start = -1
            params = []

        result.append(line)

    return '\n'.join(result)


def main() -> int:
    source, target = sys.argv[1], sys.argv[2]
    with open(source, encoding='latin-1') as handle:
        text = handle.read()
    with open(target, 'w', encoding='latin-1') as handle:
        handle.write(convert(text))
    print('ok')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
