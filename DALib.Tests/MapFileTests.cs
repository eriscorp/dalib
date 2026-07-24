using System.Text;
using DALib.Data;

namespace DALib.Tests;

/// <summary>
///     Regression coverage for <see cref="MapFile" /> against the 7.41 client's map reader
///     (darkages-741 <c>file_read_map_cells</c>). Two facts the old code got wrong: map cells are
///     <c>u16le</c> (unsigned) — a foreground id above 32767 (stc ids are five digits) read as a negative
///     <c>short</c>; and the client accepts a longer-than-expected file, ignoring the trailing bytes, while
///     the old constructor rejected any length that was not exactly <c>width * height * 6</c>.
/// </summary>
public class MapFileTests
{
    private static string WriteTempMap(byte[] bytes)
    {
        var path = Path.Combine(Path.GetTempPath(), $"dalib-map-{Guid.NewGuid():N}.map");
        File.WriteAllBytes(path, bytes);

        return path;
    }

    // one 2x1 map: cell = background(u16), leftForeground(u16), rightForeground(u16), little-endian
    private static byte[] BuildMap(ushort bg0, ushort lfg0, ushort rfg0, ushort bg1, ushort lfg1, ushort rfg1, int trailing = 0)
    {
        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, Encoding.Default, true))
        {
            writer.Write(bg0);
            writer.Write(lfg0);
            writer.Write(rfg0);
            writer.Write(bg1);
            writer.Write(lfg1);
            writer.Write(rfg1);

            for (var i = 0; i < trailing; i++)
                writer.Write((byte)0xAB);
        }

        return ms.ToArray();
    }

    [Fact]
    public void ForegroundId_AboveInt16Max_ReadsAsUnsigned()
    {
        // 40000 > 32767: a signed read would yield -25536
        var path = WriteTempMap(BuildMap(1, 40000, 50000, 2, 3, 4));

        try
        {
            var map = MapFile.FromFile(path, 2, 1);

            map[0, 0].LeftForeground.Should().Be(40000);
            map[0, 0].RightForeground.Should().Be(50000);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void TrailingBytes_AreAccepted_AndIgnored()
    {
        var path = WriteTempMap(BuildMap(1, 2, 3, 4, 5, 6, trailing: 8));

        try
        {
            var load = () => MapFile.FromFile(path, 2, 1);

            var map = load.Should().NotThrow().Subject;
            map[0, 0].Background.Should().Be(1);
            map[1, 0].RightForeground.Should().Be(6);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ShortFile_IsRejected()
    {
        // only one cell's worth of bytes for a two-cell map
        var path = WriteTempMap([0, 0, 0, 0, 0, 0]);

        try
        {
            var load = () => MapFile.FromFile(path, 2, 1);

            load.Should().Throw<InvalidDataException>();
        }
        finally
        {
            File.Delete(path);
        }
    }
}
