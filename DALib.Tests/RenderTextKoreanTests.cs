using DALib.Drawing;
using FluentAssertions;
using SkiaSharp;

namespace DALib.Tests;

/// <summary>
///     Pins which glyph <see cref="Graphics.RenderText" /> draws for Korean text. The shipped han*.fnt face
///     stores the 2,350 Wansung syllables at glyphs 0-2349 and the 51 Jamo at 2350-2400; the expected indexes
///     below were read off the retail face by rendering it, not computed from the mapping under test.
/// </summary>
public sealed class RenderTextKoreanTests : IDisposable
{
    private const int GLYPH_WIDTH = 16;
    private const int GLYPH_HEIGHT = 12;
    private const int GLYPH_COUNT = 2401;

    private readonly string _fontPath = Path.GetTempFileName();

    public RenderTextKoreanTests() => File.WriteAllBytes(_fontPath, BuildIndexStampedFont());

    public void Dispose() => File.Delete(_fontPath);

    [Theory]
    [InlineData("가", 0)]
    [InlineData("한", 2210)]
    [InlineData("힝", 2349)]
    public void RenderText_DrawsASyllableFromTheSyllableBlock(string text, int expectedGlyph)
        => RenderedGlyphIndex(text).Should().Be(expectedGlyph);

    [Theory]
    [InlineData("ㄱ", 2350)]
    [InlineData("ㅣ", 2400)]
    public void RenderText_DrawsAJamoFromAfterTheSyllableBlock(string text, int expectedGlyph)
        => RenderedGlyphIndex(text).Should().Be(expectedGlyph);

    /// <summary>
    ///     Each glyph carries its own index: the high byte in the first eight pixels of row 0, the low byte in
    ///     row 1. The first eight columns stay inside the 14-pixel advance RenderText allots per character.
    /// </summary>
    private static byte[] BuildIndexStampedFont()
    {
        var bytesPerRow = (GLYPH_WIDTH + 7) / 8;
        var data = new byte[GLYPH_COUNT * bytesPerRow * GLYPH_HEIGHT];

        for (var glyph = 0; glyph < GLYPH_COUNT; glyph++)
        {
            var offset = glyph * bytesPerRow * GLYPH_HEIGHT;
            data[offset] = (byte)(glyph >> 8);
            data[offset + bytesPerRow] = (byte)glyph;
        }

        return data;
    }

    private int RenderedGlyphIndex(string text)
    {
        var font = FntFile.FromFile(_fontPath, GLYPH_WIDTH, GLYPH_HEIGHT);

        using var image = Graphics.RenderText(font, text, SKColors.White);
        using var bitmap = SKBitmap.FromImage(image);

        return (ReadByte(bitmap, 0) << 8) | ReadByte(bitmap, 1);
    }

    private static int ReadByte(SKBitmap bitmap, int row)
    {
        var value = 0;

        for (var x = 0; x < 8; x++)
            if (bitmap.GetPixel(x, row).Alpha != 0)
                value |= 1 << (7 - x);

        return value;
    }
}
