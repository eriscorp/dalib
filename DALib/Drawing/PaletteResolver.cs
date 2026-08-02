#region
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DALib.Data;
using DALib.Definitions;
#endregion

namespace DALib.Drawing;

/// <summary>
///     Resolves which palette colors a legacy Dark Ages asset (<c>.epf</c> and the khan family here; other
///     formats to follow). An asset frame is a grid of palette indices; the palette lives elsewhere — in the
///     same archive, a sibling <c>.dat</c>, keyed sometimes by a lookup table and sometimes by a number only
///     the client knows. This is the implementation of comhaigne's <c>palette-resolution.md</c> specification
///     (transcribed from ChaosAssetManager). It selects a palette; it does not decode, draw, or animate.
/// </summary>
/// <remarks>
///     Instance-scoped so its lookup caches live and die with the open archive (the spec's answer to CAM's
///     27-field static <c>Reset()</c>). Sibling archives (<c>khanpal.dat</c>, <c>legend.dat</c>) are supplied
///     by the caller through an <see cref="ArchiveProvider" /> so the resolver names no filesystem paths.
/// </remarks>
public sealed class PaletteResolver
{
    private readonly DataArchive Archive;
    private readonly string ArchiveName;
    private readonly ArchiveProvider Provider;

    private readonly Dictionary<string, PaletteLookup?> LookupCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<int, Palette>?> IndexedCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Palette?> FixedCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DataArchive?> SiblingCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Creates a resolver for one open archive.
    /// </summary>
    /// <param name="archiveName">
    ///     The archive's file name (with or without the <c>.dat</c> extension). Dispatch is by this name.
    /// </param>
    /// <param name="archive">
    ///     The open archive the entries belong to.
    /// </param>
    /// <param name="provider">
    ///     Supplies sibling archives by file name (e.g. <c>khanpal.dat</c>, <c>legend.dat</c>). May return null;
    ///     the affected rules then resolve to null while every other rule still works.
    /// </param>
    public PaletteResolver(string archiveName, DataArchive archive, ArchiveProvider provider)
    {
        ArchiveName = NormalizeArchiveName(archiveName);
        Archive = archive;
        Provider = provider;
    }

    /// <summary>
    ///     Resolves the palette for an entry, or null if no rule matched (the caller should then fall back to a
    ///     manual picker).
    /// </summary>
    /// <param name="entry">
    ///     The entry to resolve.
    /// </param>
    /// <param name="frameIndex">
    ///     The frame within the entry. Used only by the legend <c>item</c> rule, whose palette changes between
    ///     frames of a single <c>.epf</c>; every other rule ignores it.
    /// </param>
    public ResolvedPalette? Resolve(DataArchiveEntry entry, int frameIndex = 0)
    {
        var ext = Path.GetExtension(entry.EntryName)
                      .ToLowerInvariant();

        // .spf/.efa/.pal carry their own colors and need no resolution. .hpf/.mpf/tilesets are specified but
        // deferred here pending the mpt +1 (spec) vs +2 (DALib RenderMap) tile-keying reconciliation.
        return ext == ".epf" ? ResolveEpf(entry, frameIndex) : null;
    }

    private ResolvedPalette? ResolveEpf(DataArchiveEntry entry, int frameIndex)
        => ArchiveName switch
        {
            "legend"   => ResolveLegend(entry, frameIndex),
            "national" => SiblingLegendPal("national/all"),
            "misc"     => SiblingLegendPal("misc/all"),
            "roh"      => ResolveRoh(entry),
            "setoa"    => ResolveSetoa(entry),
            _          => ArchiveName.Contains("khan", StringComparison.OrdinalIgnoreCase) ? ResolveKhan(entry) : null
        };

    #region legend.dat
    private ResolvedPalette? ResolveLegend(DataArchiveEntry entry, int frameIndex)
    {
        var name = BaseName(entry);
        entry.TryGetNumericIdentifier(out var id);

        // order is load-bearing: `line` (rule 5) matches before rule 7's list, which also names it (dead there)
        if (name.StartsWith("bkstory", StringComparison.Ordinal))
            return Indexed(Archive, "main", "backpal", id, "legend/bkstory");

        if (name.StartsWith("item", StringComparison.Ordinal))
            return Table(Archive, "main", "itempal", "item", ((id - 1) * ITEMS_PER_SHEET) + frameIndex + 1, KhanPalOverrideType.None, "legend/item");

        if (name.StartsWith("field", StringComparison.Ordinal))
            return Indexed(Archive, "main", "field", id, "legend/field");

        if (name.StartsWith("skill", StringComparison.Ordinal) || name.StartsWith("spell", StringComparison.Ordinal))
            return Fixed(Archive, "main", "legend01.pal", "legend/skill");

        if (name.StartsWith("line", StringComparison.Ordinal))
            return Fixed(Archive, "main", "legend01.pal", "legend/line");

        if (name.StartsWith("f0", StringComparison.Ordinal))
            return Fixed(Archive, "main", "legend.pal", "legend/f0");

        if (StartsWithAny(name, "clock01", "emo", "mask", "ms", "question", "rain", "snow", "woodbk"))
            return Fixed(Archive, "main", "legend01.pal", "legend/emo");

        if (name.StartsWith("staff", StringComparison.Ordinal))
            return Fixed(Archive, "main", "staff.pal", "legend/staff");

        return Fixed(Archive, "main", "legend.pal", "legend/default");
    }
    #endregion

    #region roh.dat
    private ResolvedPalette? ResolveRoh(DataArchiveEntry entry)
    {
        var name = BaseName(entry);
        entry.TryGetNumericIdentifier(out var id);

        if (name.StartsWith("efct", StringComparison.Ordinal))
            return Table(Archive, "main", "effpal", "eff", id, KhanPalOverrideType.None, "roh/efct");

        if (name.StartsWith("mefc", StringComparison.Ordinal))
            return Table(Archive, "main", "mefcpal", "mefc", id, KhanPalOverrideType.None, "roh/mefc");

        return null;
    }
    #endregion

    #region setoa.dat
    private ResolvedPalette? ResolveSetoa(DataArchiveEntry entry)
    {
        var name = BaseName(entry);
        entry.TryGetNumericIdentifier(out var id);

        // rule 1 is a real lookup; rules 2-25 are hand-mapped constants over gui*.pal. Order is load-bearing:
        // more specific prefixes (dlgcre01, emot00, lsbackm, setup12..) precede their shorter cousins.
        if (name.StartsWith("field", StringComparison.Ordinal))
            return Indexed(Archive, "main", "field", id, "setoa/field", forceFieldZero: true);

        if (name.StartsWith("dlgcre01", StringComparison.Ordinal)) return Constant(8, "setoa/dlgcre01");
        if (StartsWithAny(name, "gbicon02", "mernum")) return Constant(0, "setoa/gbicon02");
        if (StartsWithAny(name, "emot00", "emotdlg")) return Constant(0, "setoa/emot00");
        if (name.StartsWith("lsbackm", StringComparison.Ordinal)) return Constant(0, "setoa/lsbackm");
        if (StartsWithAny(name, "setup12", "setup13", "setup14")) return Constant(0, "setoa/setup12");
        if (StartsWithAny(name, "gbicon12", "orb")) return Constant(1, "setoa/gbicon12");
        if (StartsWithAny(name, "gbicon01", "gbicon03")) return Constant(2, "setoa/gbicon01");
        if (StartsWithAny(name, "emot", "equip02", "mouse")) return Constant(3, "setoa/emot");
        if (name.StartsWith("legends", StringComparison.Ordinal)) return Constant(3, "setoa/legends");
        if (name.StartsWith("nation", StringComparison.Ordinal)) return Constant(5, "setoa/nation");
        if (StartsWithAny(name, "lback", "dlgcre", "lod0", "setup")) return Constant(4, "setoa/lback");
        if (StartsWithAny(name, "skill0", "spell0")) return Constant(6, "setoa/skill0");
        if (name.StartsWith("lodbk", StringComparison.Ordinal)) return Constant(7, "setoa/lodbk");
        if (name.StartsWith("staff", StringComparison.Ordinal)) return Constant(9, "setoa/staff");
        if (StartsWithAny(name, "lsback", "lss")) return Constant(10, "setoa/lsback");
        if (name.StartsWith("leicon", StringComparison.Ordinal)) return Constant(10, "setoa/leicon");
        if (name.StartsWith("ldi", StringComparison.Ordinal)) return Constant(11, "setoa/ldi");
        if (StartsWithAny(name, "lwmap", "tmapv")) return Constant(12, "setoa/lwmap");
        if (StartsWithAny(name, "bw_back", "bw_check")) return Constant(13, "setoa/bw_back");
        if (StartsWithAny(name, "kdesc", "key", "khotkey")) return Constant(14, "setoa/kdesc");
        if (name.StartsWith("lg_", StringComparison.Ordinal)) return Constant(15, "setoa/lg_");
        if (name.StartsWith("bw_flag", StringComparison.Ordinal)) return Constant(16, "setoa/bw_flag");
        if (name.StartsWith("album_b", StringComparison.Ordinal) || name.Equals("album", StringComparison.Ordinal)) return Constant(17, "setoa/album");

        return Constant(0, "setoa/default");
    }
    #endregion

    #region khan
    private ResolvedPalette? ResolveKhan(DataArchiveEntry entry)
    {
        var name = BaseName(entry);

        if (name.Length < 2)
            return null;

        var khanpal = Sibling("khanpal.dat");

        if (khanpal is null)
            return null;

        var isMale = name[0] == 'm';
        var letter = RemapKhanLetter(char.ToLowerInvariant(name[1]));

        switch (letter)
        {
            case 'm':
            {
                // bodies have no table; the palette numbers are the in-game body-colour values. Resolve to the
                // lowest available palm number and report the rule so a host can offer the rest.
                var map = IndexedMap(khanpal, "khanpal", "palm");

                if ((map is null) || (map.Count == 0))
                    return null;

                var lowest = map.Keys.Min();

                return new ResolvedPalette(map[lowest], lowest, false, PaletteSourceKind.Indexed, "khan/body");
            }
            case 'n':
                // pants palettes are generated from legend.dat's color0.tbl dye ramp, not stored. Resolve to
                // dye index 0 (indices 0-15 are the valid pants range).
                return KhanPantsDye();
            case 'b' or 'c' or 'e' or 'f' or 'h' or 'i' or 'l' or 'p' or 'u' or 'w':
            {
                entry.TryGetNumericIdentifier(out var id, 3); // khan names carry more than one number
                var overrideType = isMale ? KhanPalOverrideType.Male : KhanPalOverrideType.Female;

                return Table(khanpal, "khanpal", $"pal{letter}", $"pal{letter}", id, overrideType, "khan/letter");
            }
            default:
                return null;
        }
    }

    private ResolvedPalette? KhanPantsDye()
    {
        var legend = Sibling("legend.dat");

        if ((legend is null) || !legend.TryGetValue("color0.tbl", out var colorEntry))
            return null;

        var colorTable = ColorTable.FromEntry(colorEntry);

        if (!colorTable.TryGetValue(0, out var dyeZero))
            return null;

        var palette = new Palette().Dye(dyeZero);

        return new ResolvedPalette(palette, 0, false, PaletteSourceKind.Indexed, "khan/pants");
    }

    private static char RemapKhanLetter(char letter)
        => letter switch
        {
            'a'        => 'b',
            'g' or 'j' => 'c',
            'o'        => 'm',
            's'        => 'p',
            _          => letter
        };
    #endregion

    #region palette builders
    private ResolvedPalette? Table(
        DataArchive archive,
        string archiveTag,
        string tablePattern,
        string palettePattern,
        int id,
        KhanPalOverrideType overrideType,
        string ruleId)
    {
        var lookup = GetLookup(archive, archiveTag, tablePattern, palettePattern);

        if ((lookup is null) || (lookup.Palettes.Count == 0))
            return null;

        var rawNumber = lookup.Table.GetPaletteNumber(id, overrideType);
        var luminanceBlended = rawNumber >= LUMINANCE_THRESHOLD;
        var palette = lookup.GetPaletteForId(id, overrideType);

        return new ResolvedPalette(
            palette,
            luminanceBlended ? rawNumber - LUMINANCE_THRESHOLD : rawNumber,
            luminanceBlended,
            PaletteSourceKind.Table,
            ruleId);
    }

    private ResolvedPalette? Indexed(DataArchive archive, string archiveTag, string pattern, int id, string ruleId, bool forceFieldZero = false)
    {
        var map = IndexedMap(archive, archiveTag, pattern, forceFieldZero);

        if ((map is null) || !map.TryGetValue(id, out var palette))
            return null;

        return new ResolvedPalette(palette, id, false, PaletteSourceKind.Indexed, ruleId);
    }

    private ResolvedPalette? Constant(int number, string ruleId)
    {
        var map = IndexedMap(Archive, "main", "gui");

        if ((map is null) || !map.TryGetValue(number, out var palette))
            return null;

        return new ResolvedPalette(palette, number, false, PaletteSourceKind.Constant, ruleId);
    }

    private ResolvedPalette? Fixed(DataArchive archive, string archiveTag, string entryName, string ruleId)
    {
        var palette = GetFixed(archive, archiveTag, entryName);

        return palette is null ? null : new ResolvedPalette(palette, NumberInName(entryName), false, PaletteSourceKind.Fixed, ruleId);
    }

    private ResolvedPalette? SiblingLegendPal(string ruleId)
    {
        var legend = Sibling("legend.dat");

        return legend is null ? null : Fixed(legend, "legend", "legend.pal", ruleId);
    }
    #endregion

    #region caches
    private PaletteLookup? GetLookup(DataArchive archive, string archiveTag, string tablePattern, string palettePattern)
    {
        var key = $"{archiveTag}|{tablePattern}|{palettePattern}";

        if (LookupCache.TryGetValue(key, out var cached))
            return cached;

        PaletteLookup? lookup;

        try
        {
            lookup = PaletteLookup.FromArchive(tablePattern, palettePattern, archive);

            if (lookup.Palettes.Count == 0)
                lookup = null;
        } catch
        {
            lookup = null;
        }

        LookupCache[key] = lookup;

        return lookup;
    }

    private Dictionary<int, Palette>? IndexedMap(DataArchive archive, string archiveTag, string pattern, bool forceFieldZero = false)
    {
        var key = $"{archiveTag}|{pattern}";

        if (IndexedCache.TryGetValue(key, out var cached))
            return cached;

        Dictionary<int, Palette>? map;

        try
        {
            map = Palette.FromArchive(pattern, archive);

            if (map.Count == 0)
                map = null;

            // wart: setoa's stray fielde00.pal also parses to id 0 and can win slot 0 over the real
            // field000.pal. Force field000.pal into slot 0. The parser is correct; the archive is odd.
            if ((map is not null) && forceFieldZero && archive.TryGetValue("field000.pal", out var field000))
                map[0] = Palette.FromEntry(field000);
        } catch
        {
            map = null;
        }

        IndexedCache[key] = map;

        return map;
    }

    private Palette? GetFixed(DataArchive archive, string archiveTag, string entryName)
    {
        var key = $"{archiveTag}|{entryName}";

        if (FixedCache.TryGetValue(key, out var cached))
            return cached;

        var palette = archive.TryGetValue(entryName, out var entry) ? Palette.FromEntry(entry) : null;
        FixedCache[key] = palette;

        return palette;
    }

    private DataArchive? Sibling(string fileName)
    {
        if (SiblingCache.TryGetValue(fileName, out var cached))
            return cached;

        var archive = Provider(fileName);
        SiblingCache[fileName] = archive;

        return archive;
    }
    #endregion

    #region helpers
    private const int ITEMS_PER_SHEET = 266;
    private const int LUMINANCE_THRESHOLD = 1000;

    private static string BaseName(DataArchiveEntry entry)
        => Path.GetFileNameWithoutExtension(entry.EntryName)
               .ToLowerInvariant();

    private static bool StartsWithAny(string name, params string[] prefixes)
    {
        foreach (var prefix in prefixes)
            if (name.StartsWith(prefix, StringComparison.Ordinal))
                return true;

        return false;
    }

    private static string NormalizeArchiveName(string archiveName)
    {
        var name = archiveName.EndsWith(".dat", StringComparison.OrdinalIgnoreCase)
            ? archiveName[..^4]
            : archiveName;

        return name.ToLowerInvariant();
    }

    private static int NumberInName(string entryName)
    {
        var digits = new string(Path.GetFileNameWithoutExtension(entryName)
                                    .Where(char.IsDigit)
                                    .ToArray());

        return int.TryParse(digits, out var value) ? value : 0;
    }
    #endregion
}

/// <summary>
///     Supplies a sibling archive by file name (e.g. <c>khanpal.dat</c>, <c>legend.dat</c>) to a
///     <see cref="PaletteResolver" />, or null when the host cannot provide it.
/// </summary>
public delegate DataArchive? ArchiveProvider(string fileName);

/// <summary>
///     The palette a <see cref="PaletteResolver" /> selected for an entry, plus how it was found.
/// </summary>
/// <param name="Palette">
///     The resolved palette.
/// </param>
/// <param name="PaletteNumber">
///     The palette number, after any luminance (≥1000) subtraction. For fixed rules, the number parsed from
///     the palette file name (or 0).
/// </param>
/// <param name="IsLuminanceBlended">
///     True when the palette carries luminance alpha; a Skia consumer must render it with straight
///     (unpremultiplied) alpha.
/// </param>
/// <param name="Kind">
///     Which kind of source the rule used.
/// </param>
/// <param name="RuleId">
///     Stable rule identifier (e.g. <c>setoa/gbicon12</c>, <c>khan/letter</c>, <c>legend/bkstory</c>). Lets a
///     UI report which rule fired and lets a test assert on the rule instead of comparing colors.
/// </param>
public sealed record ResolvedPalette(
    Palette Palette,
    int PaletteNumber,
    bool IsLuminanceBlended,
    PaletteSourceKind Kind,
    string RuleId);
