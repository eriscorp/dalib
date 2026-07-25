using DALib.Definitions;
using DALib.Drawing;
using SkiaSharp;

namespace DALib.Tests;

/// <summary>
///     Coverage for <see cref="Graphics.RenderTile" />. Ground tiles are isometric diamonds
///     (darkages-741 map-tile-banks.md): only the diamond interior is visible, palette index 0 is an ordinary
///     opaque color there (not transparent as it is for sprites), and the 784 diamond pixels sit inside a
///     56x27 record whose corners are padding. The old path routed the tile through the sprite SimpleRender,
///     which dropped every index-0 pixel (holes) and applied no diamond mask.
/// </summary>
public class RenderTileTests
{
    private const int DIAMOND_PIXELS = 784;

    private static Tile SolidIndexZeroTile() => new()
    {
        Data = new byte[CONSTANTS.TILE_SIZE] // all palette index 0
    };

    private static Palette PaletteWithRedZero()
    {
        var palette = new Palette();
        palette[0] = new SKColor(255, 0, 0); // opaque red

        return palette;
    }

    [Fact]
    public void IndexZero_InsideDiamond_IsOpaque_AndCornersAreMasked()
    {
        using var image = Graphics.RenderTile(SolidIndexZeroTile(), PaletteWithRedZero());
        using var bitmap = SKBitmap.FromImage(image);

        image.Width.Should().Be(CONSTANTS.TILE_WIDTH);
        image.Height.Should().Be(CONSTANTS.TILE_HEIGHT);

        // diamond center: index 0 renders opaque, not dropped
        bitmap.GetPixel(CONSTANTS.TILE_WIDTH / 2, CONSTANTS.TILE_HEIGHT / 2)
              .Should()
              .Be(new SKColor(255, 0, 0));

        // top-left corner is padding outside the diamond => transparent
        bitmap.GetPixel(0, 0)
              .Alpha.Should()
              .Be(0);
    }

    [Fact]
    public void ExactlyTheDiamond_IsRendered()
    {
        using var image = Graphics.RenderTile(SolidIndexZeroTile(), PaletteWithRedZero());
        using var bitmap = SKBitmap.FromImage(image);

        var opaque = 0;

        for (var y = 0; y < CONSTANTS.TILE_HEIGHT; y++)
            for (var x = 0; x < CONSTANTS.TILE_WIDTH; x++)
                if (bitmap.GetPixel(x, y).Alpha == 255)
                    opaque++;

        opaque.Should().Be(DIAMOND_PIXELS);
    }
}
