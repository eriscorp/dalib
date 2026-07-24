using DALib.Data;
using DALib.Drawing;

namespace Foscail.Conversion;

/// <summary>
///     Shared state for one unpack run: lazily opened sibling archives (khan part palettes live in
///     khanpal.dat; misc.dat monsters use hades.dat palettes) and palette lookups cached per archive
///     so 20k-entry conversions don't rebuild them per entry.
/// </summary>
internal sealed class ConversionContext(string? sourceDir) : IDisposable
{
    private readonly Dictionary<string, PaletteLookup?> Lookups = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IDictionary<int, Palette>?> PaletteSets = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DataArchive?> Siblings = new(StringComparer.OrdinalIgnoreCase);

    public void Dispose()
    {
        foreach (var archive in Siblings.Values)
            archive?.Dispose();
    }

    private readonly Dictionary<string, Palette?> FallbackPalettes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     The single-palette-era fallback: old merged clients (3.61's DarkAges.dat) carry the whole
    ///     world's art but no per-family palette tables — everything renders through legend.pal.
    ///     Gated on legend.pal being present AND no *pal.tbl family table existing, so modern
    ///     multi-palette archives (which carry both) never fall back.
    /// </summary>
    public Palette? GetFallbackPalette(string archiveName, DataArchive archive)
    {
        if (FallbackPalettes.TryGetValue(archiveName, out var cached))
            return cached;

        Palette? palette = null;

        if (archive.TryGetValue("legend.pal", out var legend)
            && !archive.Any(static e => e.EntryName.EndsWith("pal.tbl", StringComparison.OrdinalIgnoreCase)))
            palette = Palette.FromEntry(legend);

        FallbackPalettes[archiveName] = palette;

        return palette;
    }

    public PaletteLookup? GetLookup(string key, Func<PaletteLookup?> factory)
    {
        if (!Lookups.TryGetValue(key, out var lookup))
        {
            lookup = factory();
            Lookups[key] = lookup;
        }

        return lookup;
    }

    public IDictionary<int, Palette>? GetPaletteSet(string key, Func<IDictionary<int, Palette>?> factory)
    {
        if (!PaletteSets.TryGetValue(key, out var palettes))
        {
            palettes = factory();
            PaletteSets[key] = palettes;
        }

        return palettes;
    }

    /// <summary>
    ///     Opens (and caches) another archive from the same source directory. Returns null when the
    ///     source directory is unknown or the archive is absent or unreadable.
    /// </summary>
    public DataArchive? GetSiblingArchive(string fileName)
    {
        if (Siblings.TryGetValue(fileName, out var cached))
            return cached;

        DataArchive? archive = null;

        if (sourceDir is not null)
        {
            var path = Path.Combine(sourceDir, fileName);

            if (File.Exists(path))
                try
                {
                    archive = DataArchive.FromFile(path, memoryMapped: true);
                } catch
                {
                    archive = null;
                }
        }

        Siblings[fileName] = archive;

        return archive;
    }
}
