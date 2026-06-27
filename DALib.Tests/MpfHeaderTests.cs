using System.Text;
using DALib.Definitions;
using DALib.Drawing;

namespace DALib.Tests;

/// <summary>
///     Covers the MPF "Unknown" (0xFFFFFFFF magic) variable-length header introduced by the client at
///     0x50f490: when bit 2 of the flags field is set, a u32 count follows, then count * 4 bytes of data.
///     Regression coverage for issue #10 — the old parser did a direct <c>flags == 4</c> compare and
///     skipped a fixed 8 bytes, which only happened to be correct for flags == 4 and count == 1.
/// </summary>
public class MpfHeaderTests
{
    private const short PIXEL_WIDTH = 64;
    private const short PIXEL_HEIGHT = 48;

    /// <summary>
    ///     Builds the smallest valid MPF byte stream: an Unknown header with the given flags (and, when bit
    ///     2 is set, a count and count * 4 bytes of arbitrary data), zero real frames, and the trailing
    ///     palette "frame". Designed so a correct parse round-trips to an identical byte stream.
    /// </summary>
    private static byte[] BuildSyntheticMpf(int flags, int count)
    {
        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, Encoding.Default, true))
        {
            // magic: -1 (0xFFFFFFFF) => MpfHeaderType.Unknown
            writer.Write((int)MpfHeaderType.Unknown);

            // flags field
            writer.Write(flags);

            // when bit 2 is set, a u32 count then count * 4 bytes of (unknown) data follow
            if ((flags & 4) != 0)
            {
                writer.Write(count);

                for (var i = 0; i < count * 4; i++)
                    writer.Write((byte)(0x10 + i));
            }

            writer.Write((byte)1);      // frameCount (just the palette frame)
            writer.Write(PIXEL_WIDTH);  // PixelWidth  (Int16)
            writer.Write(PIXEL_HEIGHT); // PixelHeight (Int16)
            writer.Write(0);            // dataLength  (Int32) — no frame data

            writer.Write((byte)0);      // WalkFrameIndex
            writer.Write((byte)0);      // WalkFrameCount

            // FormatType block. FormatType == 0 (SingleAttack) => the parser rewinds the 2 format bytes and
            // re-reads 6 bytes (attack/standing/optional/ratio). All zero => StaticNoIdle.
            for (var i = 0; i < 6; i++)
                writer.Write((byte)0);

            // palette "frame": left/top/right/bottom/centerX/centerY all -1 (12 x 0xFF) + PaletteNumber 0
            for (var i = 0; i < 12; i++)
                writer.Write((byte)0xFF);

            writer.Write(0);            // PaletteNumber (Int32)
        }

        return ms.ToArray();
    }

    private static string WriteTempMpf(byte[] bytes)
    {
        var path = Path.Combine(Path.GetTempPath(), $"dalib-mpf-{Guid.NewGuid():N}.mpf");
        File.WriteAllBytes(path, bytes);

        return path;
    }

    [Theory]
    [InlineData(0x04, 1)] // legacy-correct case (the only one the old fixed-8 logic got right)
    [InlineData(0x04, 3)] // bit 2 set, count != 1 — old code under-read by 8 bytes
    [InlineData(0x06, 2)] // bit 2 set alongside another bit — old "flags == 4" compare missed it entirely
    [InlineData(0x14, 5)] // higher bits set too
    [InlineData(0x00, 0)] // bit 2 clear — no count/data follows
    [InlineData(0x02, 0)] // other bit set, bit 2 clear — must NOT read a count
    public void Unknown_Header_RoundTrips(int flags, int count)
    {
        var original = BuildSyntheticMpf(flags, count);
        var path = WriteTempMpf(original);

        try
        {
            var mpf = MpfFile.FromFile(path);

            // header consumed the right number of bytes => the following fields land where we put them
            mpf.HeaderType.Should().Be(MpfHeaderType.Unknown);
            mpf.PixelWidth.Should().Be(PIXEL_WIDTH);
            mpf.PixelHeight.Should().Be(PIXEL_HEIGHT);
            mpf.Count.Should().Be(0);

            var expectedHeaderLength = (flags & 4) != 0 ? 4 + 4 + (count * 4) : 4;
            mpf.UnknownHeaderBytes.Length.Should().Be(expectedHeaderLength);

            // verbatim passthrough => re-saving reproduces the input byte-for-byte
            using var saved = new MemoryStream();
            mpf.Save(saved);

            saved.ToArray().Should().BeEquivalentTo(original);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
