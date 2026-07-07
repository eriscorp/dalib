using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x56 ServerTableData (S->C) - the zlib-compressed server-table push.
///     Verifies the inflated wire layout, round-trips, and the IPv6-rejection guard.
/// </summary>
public class ServerTableDataPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    private static ServerEntry MakeServer(byte id, string ip, ushort port, string name) => new()
    {
        Id = id,
        IpAddress = IPAddress.Parse(ip),
        Port = port,
        Name = name,
    };

    private static byte[] InflateBody(byte[] body)
    {
        // The first 2 bytes are u16 BE compressed length; the rest is the zlib stream.
        var compressedLength = (body[0] << 8) | body[1];
        var compressed = body.AsSpan(2, compressedLength).ToArray();

        using var input = new MemoryStream(compressed);
        using var inflater = new ZLibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        inflater.CopyTo(output);

        return output.ToArray();
    }

    [Fact]
    public void WriteBody_EmptyTable_InflatesToSingleZeroCountByte()
    {
        var packet = new ServerTableDataPacket { Servers = [] };

        var body = packet.ToBody();
        var inflated = InflateBody(body);

        inflated.Should().Equal((byte)0x00);
    }

    [Fact]
    public void WriteBody_SingleServer_InflatesToExpectedLayout()
    {
        var packet = new ServerTableDataPacket
        {
            Servers = [MakeServer(0x01, "10.20.30.40", 0x1234, "Test")],
        };

        var body = packet.ToBody();
        var inflated = InflateBody(body);

        // [01 count][01 id][0a 14 1e 28 ip][12 34 port BE][T e s t 00]
        inflated.Should().Equal(
            (byte)0x01,
            (byte)0x01,
            (byte)0x0a, (byte)0x14, (byte)0x1e, (byte)0x28,
            (byte)0x12, (byte)0x34,
            (byte)'T', (byte)'e', (byte)'s', (byte)'t', (byte)0x00);
    }

    [Fact]
    public void RoundTrip_MultiServer_PreservesAllFields()
    {
        var original = new ServerTableDataPacket
        {
            Servers =
            [
                MakeServer(1, "10.0.0.1", 2610, "Alpha"),
                MakeServer(2, "10.0.0.2", 2611, "Beta"),
                MakeServer(3, "192.168.1.100", 2612, "Gamma"),
            ],
        };

        var body = original.ToBody();
        var parsed = ServerTableDataPacket.Parse(body);

        parsed.Servers.Should().HaveCount(3);
        parsed.Servers[0].Should().BeEquivalentTo(original.Servers[0]);
        parsed.Servers[1].Should().BeEquivalentTo(original.Servers[1]);
        parsed.Servers[2].Should().BeEquivalentTo(original.Servers[2]);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new ServerTableDataPacket
        {
            Servers = [MakeServer(7, "203.0.113.55", 2611, "Production")],
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<ServerTableDataPacket>().Subject;
        typed.Servers.Should().HaveCount(1);
        typed.Servers[0].Should().BeEquivalentTo(original.Servers[0]);
    }

    [Fact]
    public void RoundTrip_LongName_Preserved()
    {
        // Stress the cstring read; the wire format has no length prefix on the name.
        var longName = new string('x', 200);

        var original = new ServerTableDataPacket
        {
            Servers = [MakeServer(1, "10.0.0.1", 2610, longName)],
        };

        var parsed = ServerTableDataPacket.Parse(original.ToBody());

        parsed.Servers[0].Name.Should().Be(longName);
    }

    [Fact]
    public void WriteBody_IPv6Address_Throws()
    {
        var packet = new ServerTableDataPacket
        {
            Servers =
            [
                new ServerEntry
                {
                    Id = 1,
                    IpAddress = IPAddress.IPv6Loopback,
                    Port = 2610,
                    Name = "BadAddr",
                },
            ],
        };

        var act = () => packet.ToBody();

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*non-IPv4*");
    }

    [Fact]
    public void WriteBody_ProducesValidZlibStream()
    {
        // The first 2 bytes are u16 BE length; bytes 2-3 are zlib magic (0x78 followed by a
        // flag byte). For the default compression level (Optimal), the flag is typically 0xDA.
        // We don't pin the exact flag - different runtime versions may pick differently -
        // but the 0x78 magic is mandatory for a valid zlib stream.
        var packet = new ServerTableDataPacket
        {
            Servers = [MakeServer(1, "10.0.0.1", 2610, "Test")],
        };

        var body = packet.ToBody();

        body.Length.Should().BeGreaterThan(2);
        body[2].Should().Be(0x78, because: "zlib streams begin with 0x78 (CMF byte for deflate, window size 32K)");
    }

    [Fact]
    public void Parse_ToleratesBrokenAdler32Trailer()
    {
        // Regression: Hybrasyl's server-table compressor writes the Adler-32 over an empty buffer
        // (it checksums before seeking the stream to 0), so the trailer is always 0x00000001 - the
        // Adler-32 of no bytes - regardless of the real payload. The DEFLATE body is correct. The
        // retail client and Hybrasyl's own decompressor never validate the trailer; Parse must not
        // either. Bytes below are the decrypted 0x56 body captured live from production.hybrasyl.com.
        byte[] body =
        [
            0x00, 0x26,                                     // u16 BE compressed length = 38
            0x78, 0x9c,                                     // zlib header
            0x63, 0x64, 0x64, 0x60, 0x60, 0x60, 0xe0, 0x32, // DEFLATE body ...
            0xf6, 0xa8, 0x4c, 0x2a, 0x4a, 0x2c, 0xae, 0xcc,
            0xb1, 0x86, 0x31, 0x14, 0x02, 0x8a, 0xf2, 0x53,
            0x4a, 0x93, 0x4b, 0x32, 0xf3, 0xf3, 0x18, 0x00,
            0x00, 0x00, 0x00, 0x01,                         // Adler-32 trailer = 1 (broken: adler of "")
        ];

        var parsed = ServerTableDataPacket.Parse(body);

        parsed.Servers.Should().ContainSingle();
        parsed.Servers[0].Id.Should().Be(1);
        parsed.Servers[0].Port.Should().Be(2611);
        parsed.Servers[0].Name.Should().Be("Hybrasyl;Hybrasyl Production");
    }

    [Fact]
    public void Parse_PreservesIPv4AddressFamily()
    {
        var original = new ServerTableDataPacket
        {
            Servers = [MakeServer(1, "192.168.1.1", 2610, "Test")],
        };

        var parsed = ServerTableDataPacket.Parse(original.ToBody());

        parsed.Servers[0].IpAddress.AddressFamily.Should().Be(AddressFamily.InterNetwork);
        parsed.Servers[0].IpAddress.ToString().Should().Be("192.168.1.1");
    }
}
