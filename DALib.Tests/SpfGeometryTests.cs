using System.Text;
using DALib.Definitions;
using DALib.Drawing;
using SkiaSharp;

namespace DALib.Tests;

/// <summary>
///     Coverage for SPF frame geometry decode. Per the 7.41 format (darkages-741 spf.md), a frame's size is
///     the visible rectangle <c>Right - Left</c> by <c>Bottom - Top</c>, and source rows advance by
///     <c>ByteWidth</c> (pitch). The old decoder used the absolute <c>Right</c>/<c>Bottom</c> as dimensions
///     and never honored the pitch, so frames with a nonzero origin or a padded row decoded wrong. No shipped
///     7.41 asset has such a frame (all are origin 0 with <c>pitch == width</c>), so these use synthetic input.
/// </summary>
public class SpfGeometryTests
{
    private static string WriteTempSpf(byte[] bytes)
    {
        var path = Path.Combine(Path.GetTempPath(), $"dalib-spf-{Guid.NewGuid():N}.spf");
        File.WriteAllBytes(path, bytes);

        return path;
    }

    private static void WriteFrameToc(
        BinaryWriter w,
        ushort left,
        ushort top,
        ushort right,
        ushort bottom,
        uint byteWidth,
        uint byteCount,
        uint imageByteCount,
        uint startAddress = 0)
    {
        w.Write(left);
        w.Write(top);
        w.Write(right);
        w.Write(bottom);
        w.Write((short)0); // centerX
        w.Write((short)0); // centerY
        w.Write(0u);       // flags
        w.Write(startAddress);
        w.Write(byteWidth);
        w.Write(byteCount);
        w.Write(imageByteCount);
    }

    [Fact]
    public void Palettized_PaddedRows_AreCompactedToVisibleWidth()
    {
        // 3x2 visible frame with a pitch of 5 (2 padding bytes per row)
        using var ms = new MemoryStream();
        using (var w = new BinaryWriter(ms, Encoding.Default, true))
        {
            w.Write(0u); // Unknown1
            w.Write(0u); // Unknown2
            w.Write((uint)SpfFormatType.Palettized);

            for (var i = 0; i < 256; i++) w.Write((ushort)0); // rgb565 palette
            for (var i = 0; i < 256; i++) w.Write((ushort)0); // rgb555 palette

            w.Write(1u); // frameCount
            WriteFrameToc(w, left: 0, top: 0, right: 3, bottom: 2, byteWidth: 5, byteCount: 10, imageByteCount: 6);

            w.Write(10u); // totalByteCount
            w.Write(new byte[] { 1, 2, 3, 99, 99, 4, 5, 6, 99, 99 }); // rows [1,2,3|pad] [4,5,6|pad]
        }

        var path = WriteTempSpf(ms.ToArray());

        try
        {
            var spf = SpfFile.FromFile(path);
            var frame = spf[0];

            frame.PixelWidth.Should().Be(3);
            frame.PixelHeight.Should().Be(2);
            frame.ByteWidth.Should().Be(3);              // normalized to the compact pitch
            frame.Data.Should().Equal(1, 2, 3, 4, 5, 6); // padding stripped, rows tight
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Colorized_NonzeroOrigin_UsesRelativeDimensions()
    {
        // Left=1,Right=4,Top=0,Bottom=2 => visible 3x2 = 6 pixels. The old code used Right*Bottom = 8 and
        // would over-run the 6-color data section; the fix reads exactly 6.
        using var ms = new MemoryStream();
        using (var w = new BinaryWriter(ms, Encoding.Default, true))
        {
            w.Write(0u); // Unknown1
            w.Write(0u); // Unknown2
            w.Write((uint)SpfFormatType.Colorized);

            w.Write(1u); // frameCount
            WriteFrameToc(w, left: 1, top: 0, right: 4, bottom: 2, byteWidth: 6, byteCount: 12, imageByteCount: 6);

            w.Write(12u); // totalByteCount
            for (var i = 0; i < 6; i++) w.Write((ushort)0xF800); // exactly 6 rgb565 pixels
        }

        var path = WriteTempSpf(ms.ToArray());

        try
        {
            var load = () => SpfFile.FromFile(path);

            var spf = load.Should().NotThrow().Subject;
            var frame = spf[0];

            frame.PixelWidth.Should().Be(3);
            frame.PixelHeight.Should().Be(2);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Colorized_MultiFrame_DecodesEachFrameFromItsStartAddress()
    {
        // Two 1x1 colorized frames; each stores two pixel copies (RGB565 then RGB555). Frame 0 is red,
        // frame 1 is blue. The old sequential read consumed frame 0's 565 copy then read frame 0's 555
        // copy as frame 1, decoding frame 1 as red. Seeking per-frame via StartAddress fixes it.
        const ushort RED_565 = 0xF800;
        const ushort RED_555 = 0x7C00;
        const ushort BLUE_565 = 0x001F;
        const ushort BLUE_555 = 0x001F;

        using var ms = new MemoryStream();
        using (var w = new BinaryWriter(ms, Encoding.Default, true))
        {
            w.Write(0u); // Unknown1
            w.Write(0u); // Unknown2
            w.Write((uint)SpfFormatType.Colorized);

            w.Write(2u); // frameCount
            WriteFrameToc(w, left: 0, top: 0, right: 1, bottom: 1, byteWidth: 2, byteCount: 4, imageByteCount: 2, startAddress: 0);
            WriteFrameToc(w, left: 0, top: 0, right: 1, bottom: 1, byteWidth: 2, byteCount: 4, imageByteCount: 2, startAddress: 4);

            w.Write(8u); // totalByteCount
            w.Write(RED_565);
            w.Write(RED_555);
            w.Write(BLUE_565);
            w.Write(BLUE_555);
        }

        var path = WriteTempSpf(ms.ToArray());

        try
        {
            var spf = SpfFile.FromFile(path);

            var frame0 = spf[0].ColorData![0];
            var frame1 = spf[1].ColorData![0];

            frame0.Red.Should().BeGreaterThan(frame0.Blue);   // red
            frame1.Blue.Should().BeGreaterThan(frame1.Red);   // blue — would be red under the old bug
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Colorized_MultiFrame_SaveLoadRoundTrips()
    {
        using var red = SolidImage(2, 2, new SKColor(255, 0, 0));
        using var blue = SolidImage(2, 2, new SKColor(0, 0, 255));

        var spf = SpfFile.FromImages(red, blue); // colorized
        var path = Path.Combine(Path.GetTempPath(), $"dalib-spf-{Guid.NewGuid():N}.spf");

        try
        {
            spf.Save(path);

            var reloaded = SpfFile.FromFile(path);

            reloaded.Count.Should().Be(2);

            var f0 = reloaded[0].ColorData![0];
            var f1 = reloaded[1].ColorData![0];

            f0.Red.Should().BeGreaterThan(200);
            f0.Blue.Should().BeLessThan(80);
            f1.Blue.Should().BeGreaterThan(200);
            f1.Red.Should().BeLessThan(80);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static SKImage SolidImage(int width, int height, SKColor color)
    {
        var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        bitmap.Erase(color);

        return SKImage.FromBitmap(bitmap);
    }
}
