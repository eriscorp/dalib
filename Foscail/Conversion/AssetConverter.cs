using DALib.Data;
using DALib.Definitions;
using DALib.Drawing;
using SkiaSharp;

namespace Foscail.Conversion;

/// <summary>
///     Converts self-contained image assets to PNG. v1 handles the formats that carry everything
///     they need to render (SPF embeds its palette or is colorized; EFA is colorized with its own
///     blend mode). Palette-dependent formats (EPF/MPF/HPF/tiles) need external TBL/PAL resolution
///     and are left as raw extractions for a later milestone.
/// </summary>
internal static class AssetConverter
{
    /// <summary>Writes a PNG per renderable frame. Returns the number of images written (0 if unsupported).</summary>
    public static int ConvertEntry(DataArchiveEntry entry, string destDir)
    {
        var ext = Path.GetExtension(entry.EntryName).ToLowerInvariant();

        return ext switch
        {
            ".spf" => ConvertSpf(entry, destDir),
            ".efa" => ConvertEfa(entry, destDir),
            _      => 0
        };
    }

    private static int ConvertSpf(DataArchiveEntry entry, string destDir)
    {
        var spf = SpfFile.FromEntry(entry);
        var written = 0;

        for (var i = 0; i < spf.Count; i++)
        {
            var frame = spf[i];

            // Empty placeholder frames carry non-positive (often negative) dimensions and no pixel
            // data. DALib's EPF/EFA renderers guard this; the SPF overloads don't, so skip here.
            // Real frames keep their source index in the filename, so gaps are faithful.
            if (frame.PixelWidth <= 0 || frame.PixelHeight <= 0)
                continue;

            using var image = spf.Format == SpfFormatType.Colorized
                ? Graphics.RenderImage(frame)
                : Graphics.RenderImage(frame, spf.PrimaryColors!);

            WritePng(image, destDir, entry.EntryName, i, spf.Count);
            written++;
        }

        return written;
    }

    private static int ConvertEfa(DataArchiveEntry entry, string destDir)
    {
        var efa = EfaFile.FromEntry(entry);
        var written = 0;

        for (var i = 0; i < efa.Count; i++)
        {
            using var image = Graphics.RenderImage(efa[i], efa.BlendingType);

            WritePng(image, destDir, entry.EntryName, i, efa.Count);
            written++;
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
