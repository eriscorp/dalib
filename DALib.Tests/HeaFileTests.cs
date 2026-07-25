using System.Text;
using DALib.Drawing;

namespace DALib.Tests;

/// <summary>
///     Regression coverage for <see cref="HeaFile" /> RLE decode. Per the 7.41 light-mask format
///     (darkages-741 <c>hea.md</c> / <c>render_hea_decode_mask</c>), a run's first byte is
///     <c>intensity_and_flags</c> where only the low 6 bits are the light intensity; the top two bits are
///     flags the client masks off. The old decoder used the whole byte, so a run byte with a flag bit set
///     produced a corrupt intensity far above <see cref="HeaFile.MAX_LIGHT_VALUE" />.
/// </summary>
public class HeaFileTests
{
    private const int SCANLINE_WIDTH = 4;

    // minimal single-layer, single-scanline .hea whose one run is (0x82, 4): flag bit 7 set, intensity 2
    private static byte[] BuildSyntheticHea(byte runValue, byte runCount)
    {
        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, Encoding.Default, true))
        {
            writer.Write(0);             // padding
            writer.Write(640);           // screen width
            writer.Write(480);           // screen height
            writer.Write(640);           // repeat
            writer.Write(480);           // repeat
            writer.Write(1);             // tile width
            writer.Write(1);             // tile height
            writer.Write(SCANLINE_WIDTH); // scanline width
            writer.Write(1);             // scanline count
            writer.Write(1);             // layer count

            writer.Write(0);             // Thresholds[0]
            writer.Write(0);             // ScanlineOffsets[0] (word offset into RleData)

            writer.Write(runValue);
            writer.Write(runCount);
        }

        return ms.ToArray();
    }

    private static string WriteTempHea(byte[] bytes)
    {
        var path = Path.Combine(Path.GetTempPath(), $"dalib-hea-{Guid.NewGuid():N}.hea");
        File.WriteAllBytes(path, bytes);

        return path;
    }

    [Fact]
    public void RunIntensity_IsMaskedToLow6Bits()
    {
        // 0x82 = flag bit 7 set + intensity 2. Masked value must be 2, not 130.
        var path = WriteTempHea(BuildSyntheticHea(0x82, (byte)SCANLINE_WIDTH));

        try
        {
            var hea = HeaFile.FromFile(path);
            var pixels = hea.DecodeScanline(0, 0);

            pixels.Should().OnlyContain(v => v == 0x02);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
