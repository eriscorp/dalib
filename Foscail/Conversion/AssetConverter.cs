using System.Text.RegularExpressions;
using DALib.Data;
using DALib.Definitions;
using DALib.Drawing;
using SkiaSharp;

namespace Foscail.Conversion;

/// <summary>
///     Converts image assets to PNG. Self-contained formats (SPF embeds or carries its colors; EFA is
///     colorized) always convert. Palette-dependent formats (EPF/HPF/MPF/tiles) convert when their
///     family's palette convention is known: stc* HPF statics and ground tiles use the archive's own
///     TBL lookups (same conventions as DALib's RenderMap), MPF monsters carry an embedded palette
///     number into the mns*.pal set, efct/item EPFs use effpal/itempal, and khan part EPFs resolve
///     through khanpal.dat with male/female overrides. Families with no grounded convention are left
///     as raw extractions. With animate, EFA files and MPF sequences additionally emit looping APNGs
///     at their authored intervals.
/// </summary>
internal static partial class AssetConverter
{
    // itempal.tbl is keyed by global icon number with a fixed 266-icon sheet capacity: every range
    // boundary in the retail table is ≡1 (mod 266). Sheets may carry fewer actual frames (item041
    // has 247), so the stride must be this constant, never the file's own frame count.
    private const int ICONS_PER_ITEM_SHEET = 266;

    // khan entry names are <gender m|w><part letter><sprite id, 3-4 digits><anim: 2 digits or one
    // letter>.epf — e.g. mu05501 is sprite 55 anim 01, MA007C is sprite 7 anim c. The sprite id
    // (not the whole digit run) keys the palette tables; the part letter selects the pal* family.
    [GeneratedRegex(@"^([mw])([a-z])(\d{3,4})(\d{2}|[a-z])?$", RegexOptions.IgnoreCase)]
    private static partial Regex KhanEntryRegex();

    /// <summary>Writes a PNG per renderable frame. Returns the number of images written (0 if unsupported).</summary>
    public static int ConvertEntry(
        DataArchiveEntry entry,
        DataArchive archive,
        string archiveName,
        ConversionContext context,
        string destDir,
        AnimationFormats animationFormats = AnimationFormats.None)
    {
        var ext = Path.GetExtension(entry.EntryName).ToLowerInvariant();

        return ext switch
        {
            ".spf" => ConvertSpf(entry, destDir),
            ".efa" => ConvertEfa(entry, destDir, animationFormats),
            ".hpf" => ConvertHpf(entry, archive, archiveName, context, destDir),
            ".mpf" => ConvertMpf(entry, archive, archiveName, context, destDir, animationFormats),
            ".epf" => ConvertEpf(entry, archive, archiveName, context, destDir, animationFormats),
            _      => 0
        };
    }

    // part letter -> khanpal.dat palette family, per the client's own routing (grounded via
    // Brigid's AislingRenderer, which reimplements it): several letters share a family, and the
    // body/face letters (m, o) use the palm0-9 skin-tone ramp, which has no TBL — the empty table
    // resolves every id to palette 0, i.e. the default skin tone.
    private static string KhanPaletteFamily(char partLetter)
        => partLetter switch
        {
            'c' or 'g' or 'j' => "palc",
            'e'               => "pale",
            'f'               => "palf",
            'h'               => "palh",
            'i'               => "pali",
            'l'               => "pall",
            'm' or 'o'        => "palm",
            'p' or 's'        => "palp",
            'u'               => "palu",
            'w'               => "palw",
            _                 => "palb" // 'a', 'b', 'n', and anything unknown
        };

    /// <summary>
    ///     Renders every ground tile in the archive's tile bank (seo.dat's tilea + mpt palettes) into a
    ///     tiles/ subdirectory. Returns the number of images written (0 when the archive has no bank).
    /// </summary>
    public static int ConvertTiles(DataArchive archive, string archiveName, ConversionContext context, string destDir)
    {
        if (!archive.Contains("tilea.bmp"))
            return 0;

        var lookup = context.GetLookup($"{archiveName}:mpt", () => NonEmpty(PaletteLookup.FromArchive("mpt", archive)));

        if (lookup is null)
            return 0;

        var tiles = Tileset.FromArchive("tilea", archive);
        var tileDir = Path.Combine(destDir, "tiles");
        Directory.CreateDirectory(tileDir);

        for (var i = 0; i < tiles.Count; i++)
        {
            // +2 is the library's own bg-tile keying; see Graphics.RenderMap
            var palette = lookup.GetPaletteForId(i + 2);

            using var image = Graphics.RenderTile(tiles[i], palette);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var fs = File.Create(Path.Combine(tileDir, $"tile{i:D5}.png"));

            data.SaveTo(fs);
        }

        return tiles.Count;
    }

    private static int ConvertSpf(DataArchiveEntry entry, string destDir)
    {
        var spf = SpfFile.FromEntry(entry);
        var written = 0;

        for (var i = 0; i < spf.Count; i++)
        {
            var frame = spf[i];

            // Empty placeholder frames carry non-positive (often negative) dimensions and no pixel
            // data. Real frames keep their source index in the filename, so gaps are faithful.
            if ((frame.PixelWidth <= 0) || (frame.PixelHeight <= 0))
                continue;

            using var image = spf.Format == SpfFormatType.Colorized
                ? Graphics.RenderImage(frame)
                : Graphics.RenderImage(frame, spf.PrimaryColors!);

            WritePng(image, destDir, entry.EntryName, i, spf.Count);
            written++;
        }

        return written;
    }

    private static int ConvertEfa(DataArchiveEntry entry, string destDir, AnimationFormats animationFormats)
    {
        var efa = EfaFile.FromEntry(entry);
        var written = 0;
        var frames = new List<SKImage>(efa.Count);

        try
        {
            for (var i = 0; i < efa.Count; i++)
                frames.Add(Graphics.RenderImage(efa[i], efa.BlendingType));

            for (var i = 0; i < frames.Count; i++)
            {
                WritePng(frames[i], destDir, entry.EntryName, i, efa.Count);
                written++;
            }

            if (frames.Count > 1)
            {
                written += AnimationWriter.Write(
                    frames,
                    destDir,
                    $"{entry.EntryName}.anim",
                    efa.FrameIntervalMs > 0 ? efa.FrameIntervalMs : 100,
                    animationFormats);
            }
        } finally
        {
            foreach (var frame in frames)
                frame.Dispose();
        }

        return written;
    }

    // stc*.hpf statics (ia.dat): file numbers are 0-based, the stc TBLs are 1-based — the +1 is the
    // library's own keying; see Graphics.RenderMap's foreground path
    private static int ConvertHpf(
        DataArchiveEntry entry,
        DataArchive archive,
        string archiveName,
        ConversionContext context,
        string destDir)
    {
        if (!entry.TryGetNumericIdentifier(out var id))
            return 0;

        var lookup = context.GetLookup($"{archiveName}:stc", () => NonEmpty(PaletteLookup.FromArchive("stc", archive)));

        var palette = lookup?.GetPaletteForId(id + 1) ?? context.GetFallbackPalette(archiveName, archive);

        if (palette is null)
            return 0;

        using var image = Graphics.RenderImage(HpfFile.FromEntry(entry), palette);

        WritePng(image, destDir, entry.EntryName, 0, 1);

        return 1;
    }

    // MPF monsters (hades.dat, one stray in misc.dat): the file embeds its palette number, which
    // indexes the mns*.pal set living alongside the MPFs in hades.dat
    private static int ConvertMpf(
        DataArchiveEntry entry,
        DataArchive archive,
        string archiveName,
        ConversionContext context,
        string destDir,
        AnimationFormats animationFormats)
    {
        var palettes = context.GetPaletteSet(
            $"{archiveName}:mns",
            () =>
            {
                var own = Palette.FromArchive("mns", archive);

                if (own.Count > 0)
                    return own;

                var hades = context.GetSiblingArchive("hades.dat");

                return hades is null ? null : Palette.FromArchive("mns", hades);
            });

        var mpf = MpfFile.FromEntry(entry);
        Palette? palette = null;

        if (palettes is not null)
            palettes.TryGetValue(mpf.PaletteNumber, out palette);

        palette ??= context.GetFallbackPalette(archiveName, archive);

        if (palette is null)
            return 0;

        var written = 0;

        for (var i = 0; i < mpf.Count; i++)
        {
            var frame = mpf[i];

            if ((frame.PixelWidth <= 0) || (frame.PixelHeight <= 0))
                continue;

            using var image = Graphics.RenderImage(frame, palette);

            WritePng(image, destDir, entry.EntryName, i, mpf.Count);
            written++;
        }

        if (animationFormats != AnimationFormats.None)
            written += WriteMpfAnimations(mpf, palette, entry.EntryName, destDir, animationFormats);

        return written;
    }

    // MPF headers name their animation sequences (start index + frame count each) and carry the
    // playback interval; each non-empty sequence becomes its own looping animation
    private static int WriteMpfAnimations(
        MpfFile mpf,
        Palette palette,
        string entryName,
        string destDir,
        AnimationFormats animationFormats)
    {
        // authored intervals cluster at 100-500ms; 10000 is a no-authored-interval sentinel carried
        // by half the retail monsters (294/581) and would look frozen — fall back to the corpus mode
        var delayMs = mpf.AnimationIntervalMs is > 0 and < 10000 ? mpf.AnimationIntervalMs : 200;
        var written = 0;

        foreach (var (name, start, count) in (ReadOnlySpan<(string, int, int)>)
                 [
                     ("standing", mpf.StandingFrameIndex, mpf.StandingFrameCount),
                     ("walk", mpf.WalkFrameIndex, mpf.WalkFrameCount),
                     ("attack", mpf.AttackFrameIndex, mpf.AttackFrameCount),
                     ("attack2", mpf.Attack2StartIndex, mpf.Attack2FrameCount),
                     ("attack3", mpf.Attack3StartIndex, mpf.Attack3FrameCount)
                 ])
        {
            if ((count < 2) || (start < 0) || (start + count > mpf.Count))
                continue;

            var frames = new List<SKImage>(count);

            try
            {
                for (var i = start; i < start + count; i++)
                {
                    var frame = mpf[i];

                    frames.Add(
                        (frame.PixelWidth <= 0) || (frame.PixelHeight <= 0)
                            ? RenderEmpty()
                            : Graphics.RenderImage(frame, palette));
                }

                written += AnimationWriter.Write(frames, destDir, $"{entryName}.{name}", delayMs, animationFormats);
            } finally
            {
                foreach (var frame in frames)
                    frame.Dispose();
            }
        }

        return written;
    }

    private static SKImage RenderEmpty()
    {
        using var bitmap = new SKBitmap(1, 1, SKColorType.Bgra8888, SKAlphaType.Premul);
        bitmap.Erase(SKColors.Transparent);

        return SKImage.FromBitmap(bitmap);
    }

    private static int ConvertEpf(
        DataArchiveEntry entry,
        DataArchive archive,
        string archiveName,
        ConversionContext context,
        string destDir,
        AnimationFormats animationFormats)
    {
        var name = Path.GetFileNameWithoutExtension(entry.EntryName);

        // efct*.epf effects and mefc*.epf motion effects (roh.dat): effpal.tbl / mefcpal.tbl are
        // keyed by the 1-based effect number. The explicit "*pal" table patterns matter — a bare
        // "eff" would merge effect.tbl garbage.
        var effectFamily = name.StartsWith("efct", StringComparison.OrdinalIgnoreCase) ? ("effpal", "eff")
            : name.StartsWith("mefc", StringComparison.OrdinalIgnoreCase) ? ("mefcpal", "mefc")
            : default((string, string)?);

        if (effectFamily is var (tablePattern, palettePattern) && entry.TryGetNumericIdentifier(out var effectId))
        {
            var lookup = context.GetLookup(
                $"{archiveName}:{palettePattern}",
                () => NonEmpty(PaletteLookup.FromArchive(tablePattern, palettePattern, archive)));

            var effectPalette = lookup is not null
                ? lookup.GetPaletteForId(effectId)
                : context.GetFallbackPalette(archiveName, archive);

            if (effectPalette is null)
                return 0;

            // effect sequences (Effect.tbl) carry frame selectors but no timing — the interval comes
            // from the server packet at runtime — so file-order at a default interval is the export
            return RenderEpfFrames(
                entry,
                destDir,
                (_, _) => effectPalette,
                animationBaseName: $"{entry.EntryName}.anim",
                animationDelayMs: 100,
                animationFormats: animationFormats);
        }

        // item*.epf icon sheets (Legend.dat): itempal.tbl is keyed by global icon number — file N's
        // frames occupy the 1-based range (N-1)*frameCount+1 .. N*frameCount (ranges observed 266 wide)
        if (name.StartsWith("item", StringComparison.OrdinalIgnoreCase) && entry.TryGetNumericIdentifier(out var itemFileId))
        {
            var lookup = context.GetLookup(
                $"{archiveName}:item",
                () => NonEmpty(PaletteLookup.FromArchive("itempal", "item", archive)));

            if (lookup is not null)
                return RenderEpfFrames(
                    entry,
                    destDir,
                    (frameIndex, _) => lookup.GetPaletteForId((itemFileId - 1) * ICONS_PER_ITEM_SHEET + frameIndex + 1));

            var itemFallback = context.GetFallbackPalette(archiveName, archive);

            return itemFallback is null ? 0 : RenderEpfFrames(entry, destDir, (_, _) => itemFallback);
        }

        // khan part sprites: palettes live in khanpal.dat, part letter -> family per KhanPaletteFamily
        var khanMatch = KhanEntryRegex().Match(name);

        if (khanMatch.Success)
        {
            var family = KhanPaletteFamily(char.ToLowerInvariant(khanMatch.Groups[2].Value[0]));

            var lookup = context.GetLookup(
                $"khanpal:{family}",
                () =>
                {
                    var khanpal = context.GetSiblingArchive("khanpal.dat");

                    return khanpal is null ? null : NonEmpty(PaletteLookup.FromArchive(family, khanpal));
                });

            if (lookup is null)
            {
                var khanFallback = context.GetFallbackPalette(archiveName, archive);

                return khanFallback is null ? 0 : RenderEpfFrames(entry, destDir, (_, _) => khanFallback);
            }

            var partId = int.Parse(khanMatch.Groups[3].Value);

            var overrideType = char.ToLowerInvariant(khanMatch.Groups[1].Value[0]) == 'm'
                ? KhanPalOverrideType.Male
                : KhanPalOverrideType.Female;

            // palette numbers >= 1000 mean luminance-blended alpha; the render must not premultiply
            var alphaType = lookup.Table.GetPaletteNumber(partId, overrideType) >= 1000
                ? SKAlphaType.Unpremul
                : SKAlphaType.Premul;

            var palette = lookup.GetPaletteForId(partId, overrideType);

            return RenderEpfFrames(entry, destDir, (_, _) => palette, alphaType);
        }

        // no known family: in the single-palette era (legend.pal, no family tables) every EPF is
        // renderable with the global palette; modern archives leave unknown families raw
        var fallback = context.GetFallbackPalette(archiveName, archive);

        return fallback is null ? 0 : RenderEpfFrames(entry, destDir, (_, _) => fallback);
    }

    private static PaletteLookup? NonEmpty(PaletteLookup lookup) => lookup.Palettes.Count == 0 ? null : lookup.Freeze();

    private static int RenderEpfFrames(
        DataArchiveEntry entry,
        string destDir,
        Func<int, int, Palette> paletteForFrame,
        SKAlphaType alphaType = SKAlphaType.Premul,
        string? animationBaseName = null,
        int animationDelayMs = 100,
        AnimationFormats animationFormats = AnimationFormats.None)
    {
        var epf = EpfFile.FromEntry(entry);
        var written = 0;
        var animationFrames = (animationBaseName is not null) && (animationFormats != AnimationFormats.None)
            ? new List<SKImage>(epf.Count)
            : null;

        try
        {
            for (var i = 0; i < epf.Count; i++)
            {
                var frame = epf[i];

                if ((frame.PixelWidth <= 0) || (frame.PixelHeight <= 0))
                    continue;

                var image = Graphics.RenderImage(frame, paletteForFrame(i, epf.Count), alphaType);

                try
                {
                    WritePng(image, destDir, entry.EntryName, i, epf.Count);
                    written++;
                }
                finally
                {
                    if (animationFrames is not null)
                        animationFrames.Add(image);
                    else
                        image.Dispose();
                }
            }

            if (animationFrames is { Count: > 1 })
                written += AnimationWriter.Write(animationFrames, destDir, animationBaseName!, animationDelayMs, animationFormats);
        } finally
        {
            if (animationFrames is not null)
                foreach (var image in animationFrames)
                    image.Dispose();
        }

        return written;
    }

    // Single-frame assets become <name>.png; multi-frame become <name>.000.png, <name>.001.png, ...
    // The original entry name (extension included) is kept so a converted image never collides with
    // its raw source or with another format that shares the stem.
    private static void WritePng(SKImage image, string destDir, string entryName, int frame, int frameCount)
    {
        var suffix = frameCount > 1 ? $".{frame:D3}" : string.Empty;

        var destRoot = Path.GetFullPath(destDir);

        if (!Path.EndsInDirectorySeparator(destRoot))
            destRoot += Path.DirectorySeparatorChar;

        // entry names come from archive bytes; refuse any that would resolve outside the destination
        var path = Path.GetFullPath($"{entryName}{suffix}.png", destRoot);

        if (!path.StartsWith(destRoot, StringComparison.Ordinal))
            throw new InvalidDataException($"Entry name \"{entryName}\" resolves outside the output directory");

        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var fs = File.Create(path);

        data.SaveTo(fs);
    }
}
