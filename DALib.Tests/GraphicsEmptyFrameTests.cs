using DALib.Drawing;
using FluentAssertions;

namespace DALib.Tests;

/// <summary>
///     Regression coverage for the empty-frame guard in <see cref="Graphics" />'s shared
///     <c>SimpleRender</c> helper. SPF (and MPF/HPF/Tile) frames can be empty placeholders encoded with
///     non-positive — often negative — dimensions (<c>Right &lt; Left</c> ⇒ <c>PixelWidth &lt; 0</c>) and
///     no pixel data. Before the guard, rendering such a frame built <c>new SKBitmap(&lt;=0, …)</c>, whose
///     <c>PeekPixels()</c> is null, and threw a NullReferenceException — hit the moment any caller iterated
///     every frame of a real SPF (e.g. the setoa.dat UI sprites). The EPF/EFA overloads already guarded
///     this; the fix moved the guard into SimpleRender so every caller is covered.
/// </summary>
public class GraphicsEmptyFrameTests
{
    // Right < Left and Bottom < Top => negative PixelWidth/PixelHeight: the empty-frame encoding.
    private static SpfFrame EmptyFrame() => new()
    {
        Left = 45,
        Right = 0,
        Top = 16,
        Bottom = 0
    };

    [Fact]
    public void RenderImage_PalettizedSpfFrame_WithEmptyFrame_ReturnsPlaceholderInsteadOfThrowing()
    {
        var frame = EmptyFrame();
        frame.Data = [];

        var render = () => Graphics.RenderImage(frame, new Palette());

        using var image = render.Should().NotThrow().Subject;
        image.Width.Should().Be(1);
        image.Height.Should().Be(1);
    }

    [Fact]
    public void RenderImage_ColorizedSpfFrame_WithEmptyFrame_ReturnsPlaceholderInsteadOfThrowing()
    {
        var frame = EmptyFrame();
        frame.ColorData = [];

        var render = () => Graphics.RenderImage(frame);

        using var image = render.Should().NotThrow().Subject;
        image.Width.Should().Be(1);
        image.Height.Should().Be(1);
    }
}
