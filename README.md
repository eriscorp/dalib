# DALib

DALib is a C# class library for *Dark Ages* and related games, targeting .NET 9 and
.NET 10. It reads, renders, and writes the game's data formats and speaks its
network protocol.

## Install

```
dotnet add package DALib --prerelease
```

## What it does

**Archives & maps**

* `.dat` archives (read/write)
* `.map` map files

**Graphics & assets**

* Image formats: SPF, EPF, MPF, HPF, EFA
* Palettes and palette tables (`.pal`, `.tbl`): lookups, cycling, and the
  per-family palette conventions the client uses
* Ground tiles (isometric tile banks), HEA light maps, control/layout files,
  FNT fonts, and effect / tile-animation tables
* Rendering to `SKImage` / PNG via SkiaSharp

**Networking**

* A full typed packet library representing the DOOMVAS protocol, including the 
wire "cryptography" (CRC, dialog obfuscation, per-connection state)

## Companion tool

[**Foscail**](Foscail/README.md) is a command-line tool built on DALib that
unpacks `.dat` archives and converts assets to PNG / APNG / GIF:

```
dotnet tool install --global Foscail --prerelease
```

## Notes

* Rendering on Linux requires SkiSharp native assets: add
  `SkiaSharp.NativeAssets.Linux.NoDependencies` to your **application** (base
  SkiaSharp ships only Windows/macOS natives).

## License

MIT. See [LICENSE](LICENSE) and [CONTRIBUTORS.md](CONTRIBUTORS.md).
