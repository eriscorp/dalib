using System.Buffers.Binary;
using Foscail.Conversion;
using SkiaSharp;

namespace Foscail.Tests;

public class ApngEncoderTests : IDisposable
{
    private readonly string ScratchDir = Directory.CreateTempSubdirectory("foscail-apng-").FullName;

    public void Dispose() => Directory.Delete(ScratchDir, true);

    private static SKImage SolidImage(SKColor color, int size = 4)
    {
        using var bitmap = new SKBitmap(size, size, SKColorType.Bgra8888, SKAlphaType.Premul);
        bitmap.Erase(color);

        return SKImage.FromBitmap(bitmap);
    }

    [Fact]
    public void Writes_A_Valid_Looping_Apng()
    {
        var path = Path.Combine(ScratchDir, "anim.png");

        using var red = SolidImage(SKColors.Red);
        using var blue = SolidImage(SKColors.Blue);

        ApngEncoder.Write(path, [red, blue], 80);

        var bytes = File.ReadAllBytes(path);

        // PNG signature intact; animation chunks present in order
        bytes.AsSpan(0, 8).ToArray().Should().Equal(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A);

        var chunks = ListChunks(bytes);

        chunks.Should().ContainInOrder("IHDR", "acTL", "fcTL", "IDAT", "fcTL", "fdAT", "IEND");
        chunks.Count(static c => c == "fcTL").Should().Be(2);

        // acTL declares 2 frames, infinite loop
        var actlPos = FindChunk(bytes, "acTL");
        BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(actlPos + 8)).Should().Be(2);
        BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(actlPos + 12)).Should().Be(0);

        // a non-APNG-aware decoder (Skia) still reads the first frame as a plain PNG
        using var decoded = SKBitmap.Decode(path);
        decoded.Width.Should().Be(4);
        decoded.GetPixel(1, 1).Should().Be(SKColors.Red);
    }

    private static List<string> ListChunks(byte[] png)
    {
        var chunks = new List<string>();
        var pos = 8;

        while (pos + 8 <= png.Length)
        {
            var length = (int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(pos));
            chunks.Add(System.Text.Encoding.ASCII.GetString(png, pos + 4, 4));
            pos += 12 + length;
        }

        return chunks;
    }

    private static int FindChunk(byte[] png, string type)
    {
        var pos = 8;

        while (pos + 8 <= png.Length)
        {
            var length = (int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(pos));

            if (System.Text.Encoding.ASCII.GetString(png, pos + 4, 4) == type)
                return pos;

            pos += 12 + length;
        }

        throw new InvalidOperationException($"chunk {type} not found");
    }
}
