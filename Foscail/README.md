# foscail

Command-line tools for Dark Ages (DOOMVAS v1) client data, built on
[DALib](https://github.com/eriscorp/dalib). *Foscail* is Irish for "open."

## Install

```
dotnet tool install --global Foscail --prerelease
```

## Usage

```
foscail unpack <input> [--output <dir>] [--convert]
```

`<input>` is a `.dat` archive, an archive name without the extension, or a
directory containing `.dat` archives (matched case-insensitively). Each archive
is unpacked into its own subdirectory of the output directory (default
`./foscail-out`).

```
foscail unpack "C:\Program Files\KRU\Dark Ages" -o ./extracted --convert
```

With `--convert`, image formats that carry everything they need to render are
also written as PNG alongside the raw files:

| Format | Contents | Raw extract | PNG conversion |
|--------|----------|-------------|----------------|
| SPF    | UI / interface art | yes | yes |
| EFA    | effect animations  | yes | yes |
| EPF, HPF, MPF, tiles | sprites, walls, monsters, maps | yes | planned (needs TBL/PAL palette resolution) |
| everything else (txt, pal, tbl, ...) | | yes | — |

Multi-frame assets become `name.000.png`, `name.001.png`, …; single frames
become `name.png`. Converted images keep the full original entry name so they
never collide with their raw source.

## Behavior notes

- A corrupt, hostile, or unreadable archive is reported and counted as a
  failure; the rest of the batch still processes. Exit code is `0` for a fully
  clean run, `2` if any failures occurred, `1` if no archive was found.
- Archives in the extended format (leading `0xFFFFFFFF` word, used by some
  newer clients) are reported as unsupported rather than mis-parsed.
- Entry names are validated so a malicious archive cannot write outside the
  output directory.
