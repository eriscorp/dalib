using System.Text;
using DALib.Drawing;
using SkiaSharp;

namespace Foscail.Tests;

/// <summary>Builders for synthetic archives and assets used across the test classes.</summary>
internal static class SyntheticData
{
    // legacy index layout: [count+1:i32] then per entry [start:i32][name:13 ascii, NUL padded],
    // a final [end:i32], then the concatenated entry data
    public static byte[] BuildArchive(params (string Name, byte[] Data)[] entries)
    {
        const int NAME_LENGTH = 13;

        using var ms = new MemoryStream();

        using (var writer = new BinaryWriter(ms, Encoding.Default, true))
        {
            writer.Write(entries.Length + 1);

            var address = 4 + entries.Length * (4 + NAME_LENGTH) + 4;

            foreach (var (name, data) in entries)
            {
                writer.Write(address);
                writer.Write(Encoding.ASCII.GetBytes(name.PadRight(NAME_LENGTH, '\0')));

                address += data.Length;
            }

            writer.Write(address);

            foreach (var (_, data) in entries)
                writer.Write(data);
        }

        return ms.ToArray();
    }

    /// <summary>A single-color 2x2 single-frame EPF whose pixels are all palette index 1.</summary>
    public static byte[] BuildEpf(int frameCount = 1)
    {
        var epf = new EpfFile(2, 2);

        for (var i = 0; i < frameCount; i++)
            epf.Add(
                new EpfFrame
                {
                    Left = 0,
                    Top = 0,
                    Right = 2,
                    Bottom = 2,
                    Data = [1, 1, 1, 1]
                });

        using var ms = new MemoryStream();
        epf.Save(ms);

        return ms.ToArray();
    }

    /// <summary>A 256-color palette with the given color at index 1 (index 0 stays transparent-key black).</summary>
    public static byte[] BuildPalette(SKColor colorAtIndex1)
    {
        var palette = new Palette
        {
            [1] = colorAtIndex1
        };

        using var ms = new MemoryStream();
        palette.Save(ms);

        return ms.ToArray();
    }

    public static SKColor ReadPixel(string pngPath, int x, int y)
    {
        using var bitmap = SKBitmap.Decode(pngPath);

        return bitmap.GetPixel(x, y);
    }
}
