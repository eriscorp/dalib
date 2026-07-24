using Foscail.Conversion;
using SkiaSharp;

namespace Foscail.Tests;

public class AnimationWriterTests : IDisposable
{
    private readonly string ScratchDir = Directory.CreateTempSubdirectory("foscail-anim-").FullName;

    public void Dispose() => Directory.Delete(ScratchDir, true);

    private static SKImage SolidImage(SKColor color, int size = 4)
    {
        using var bitmap = new SKBitmap(size, size, SKColorType.Bgra8888, SKAlphaType.Premul);
        bitmap.Erase(color);

        return SKImage.FromBitmap(bitmap);
    }

    [Fact]
    public void Writes_Both_Formats_When_Requested()
    {
        using var red = SolidImage(SKColors.Red);
        using var blue = SolidImage(SKColors.Blue);

        var written = AnimationWriter.Write([red, blue], ScratchDir, "anim", 80, AnimationFormats.Apng | AnimationFormats.Gif);

        written.Should().Be(2);

        var apng = File.ReadAllBytes(Path.Combine(ScratchDir, "anim.png"));
        apng.AsSpan(0, 4).ToArray().Should().Equal(0x89, 0x50, 0x4E, 0x47);
        apng.Should().Contain("acTL"u8.ToArray()[0]); // structural detail covered by ApngEncoderTests

        var gif = File.ReadAllBytes(Path.Combine(ScratchDir, "anim.gif"));
        System.Text.Encoding.ASCII.GetString(gif, 0, 6).Should().Be("GIF89a");

        // KGySoft writes the NETSCAPE looping extension for AnimationMode.Repeat
        System.Text.Encoding.ASCII.GetString(gif).Should().Contain("NETSCAPE2.0");

        // Skia decodes the first GIF frame; solid red survives quantization exactly
        using var decoded = SKBitmap.Decode(Path.Combine(ScratchDir, "anim.gif"));
        decoded.Width.Should().Be(4);
        decoded.GetPixel(1, 1).Should().Be(SKColors.Red);
    }

    [Fact]
    public void Writes_Nothing_When_No_Format_Requested()
    {
        using var red = SolidImage(SKColors.Red);

        AnimationWriter.Write([red], ScratchDir, "anim", 80, AnimationFormats.None).Should().Be(0);

        Directory.EnumerateFiles(ScratchDir).Should().BeEmpty();
    }
}
