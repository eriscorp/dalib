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

With `--convert`, image assets are also written as PNG alongside the raw files.
Self-contained formats always convert; palette-dependent formats convert when
their family's palette convention is known:

| Format | Contents | Palette source | PNG conversion |
|--------|----------|----------------|----------------|
| SPF    | UI / interface art | embedded or colorized | yes |
| EFA    | effect animations  | colorized | yes |
| HPF (`stc*`) | wall / static tiles (ia.dat) | `stc*` TBL lookup in the same archive | yes |
| MPF    | monsters (hades.dat) | embedded palette number → `mns*.pal` | yes |
| EPF `efct*` | spell effects (roh.dat) | `effpal.tbl` | yes |
| EPF `item*` | item icon sheets (Legend.dat) | `itempal.tbl`, global icon numbering | yes |
| EPF khan parts (`m…`/`w…`) | equipment / body / face sprites (khan\*.dat) | `khanpal.dat` palette families (letters share families: a/b/n→palb, c/g/j→palc, p/s→palp; body/face m/o→palm skin ramp) | yes |
| ground tiles | terrain (seo.dat `tilea` bank) | `mpt*` TBL lookup | yes, into `tiles/` |
| other EPF families | misc UI art | unknown | no — raw only |

Cross-archive palettes (khan parts, the stray monster in misc.dat) resolve from
sibling `.dat` files in the same directory as the input; when the sibling is
missing, those assets are left as raw extractions.

Multi-frame assets become `name.000.png`, `name.001.png`, …; single frames
become `name.png`. Converted images keep the full original entry name so they
never collide with their raw source.

With `--animate` and/or `--gif` (alongside `--convert`), animated formats
additionally emit looping animations at their authored playback speed: one
`name.anim.*` per multi-frame EFA effect, and one per named MPF monster
sequence (`name.standing.*`, `name.walk.*`, `name.attack.*`, …). `--animate`
writes animated PNG (lossless RGBA; plays in every modern browser and viewer,
older decoders show the first frame); `--gif` writes animated GIF (256-color
quantized, universally supported). Both flags together write both.

## Behavior notes

- A corrupt, hostile, or unreadable archive is reported and counted as a
  failure; the rest of the batch still processes. Exit code is `0` for a fully
  clean run, `2` if any failures occurred, `1` if no archive was found.
- Archives in the extended format (leading `0xFFFFFFFF` word, used by some
  newer clients) are reported as unsupported rather than mis-parsed.
- Entry names are validated so a malicious archive cannot write outside the
  output directory.
