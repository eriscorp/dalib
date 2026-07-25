using KGySoft.Drawing.Imaging;
using KGySoft.Drawing.SkiaSharp;
using SkiaSharp;

namespace Foscail.Conversion;

[Flags]
internal enum AnimationFormats
{
    None = 0,
    Apng = 1,
    Gif = 2
}

/// <summary>
///     Writes one animation in the requested format(s): APNG via the in-repo chunk splicer
///     (lossless RGBA), GIF via KGySoft's encoder (256-color quantized, binary transparency —
///     universally supported). Frames of one animation share a top-left origin (offsets are baked
///     in by the render) but not a canvas size, so they are composed onto the union canvas first.
/// </summary>
internal static class AnimationWriter
{
    /// <summary>Returns the number of animation files written.</summary>
    public static int Write(
        IReadOnlyList<SKImage> frames,
        string destDir,
        string baseName,
        int delayMs,
        AnimationFormats formats)
    {
        if (formats == AnimationFormats.None)
            return 0;

        var destRoot = Path.GetFullPath(destDir);

        if (!Path.EndsInDirectorySeparator(destRoot))
            destRoot += Path.DirectorySeparatorChar;

        // baseName derives from archive entry names; keep the containment guard
        var basePath = Path.GetFullPath(baseName, destRoot);

        if (!basePath.StartsWith(destRoot, StringComparison.Ordinal))
            throw new InvalidDataException($"Entry name \"{baseName}\" resolves outside the output directory");

        var width = frames.Max(static f => f.Width);
        var height = frames.Max(static f => f.Height);

        var composed = new List<SKImage>(frames.Count);
        var written = 0;

        try
        {
            foreach (var frame in frames)
            {
                using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));

                surface.Canvas.Clear(SKColors.Transparent);
                surface.Canvas.DrawImage(frame, 0, 0);
                composed.Add(surface.Snapshot());
            }

            if (formats.HasFlag(AnimationFormats.Apng))
            {
                ApngEncoder.Write($"{basePath}.png", composed, delayMs);
                written++;
            }

            if (formats.HasFlag(AnimationFormats.Gif))
            {
                WriteGif($"{basePath}.gif", composed, delayMs);
                written++;
            }
        } finally
        {
            foreach (var image in composed)
                image.Dispose();
        }

        return written;
    }

    private static void WriteGif(string path, List<SKImage> composed, int delayMs)
    {
        var bitmapDatas = composed.ConvertAll(static image => image.GetReadableBitmapData());

        try
        {
            var configuration = new AnimatedGifConfiguration(bitmapDatas, TimeSpan.FromMilliseconds(delayMs))
            {
                AnimationMode = AnimationMode.Repeat
            };

            using var fs = File.Create(path);

            GifEncoder.EncodeAnimation(configuration, fs);
        } finally
        {
            foreach (var data in bitmapDatas)
                data.Dispose();
        }
    }
}
