using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x2E WorldMap (S->C) - pins the
///     <c>[string8 fieldName][u8 nodeCount][u8 imageIndex]</c> header plus per-node
///     <c>[u16 x][u16 y][string8 text][u16 checkSum][u16 mapId][u16 destX][u16 destY]</c> layout and
///     the codec round-trip.
/// </summary>
public class WorldMapPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new WorldMapPacket
        {
            FieldName = "ab",
            ImageIndex = 0x05,
            Nodes =
            [
                new WorldMapNode
                {
                    X = 0x0102,
                    Y = 0x0304,
                    Text = "c",
                    CheckSum = 0x1112,
                    MapId = 0x1314,
                    DestinationX = 0x1516,
                    DestinationY = 0x1718,
                },
            ],
        };

        // [02 61 62] string8 "ab" [01] nodeCount [05] imageIndex
        // node: [01 02] x [03 04] y [01 63] "c" [11 12] checksum [13 14] mapId [15 16] destX [17 18] destY
        packet.ToBody().Should().Equal(
            (byte)0x02, (byte)0x61, (byte)0x62,
            (byte)0x01,
            (byte)0x05,
            (byte)0x01, (byte)0x02,
            (byte)0x03, (byte)0x04,
            (byte)0x01, (byte)0x63,
            (byte)0x11, (byte)0x12,
            (byte)0x13, (byte)0x14,
            (byte)0x15, (byte)0x16,
            (byte)0x17, (byte)0x18);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllNodes()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new WorldMapPacket
        {
            FieldName = "Temuair",
            ImageIndex = 1,
            Nodes =
            [
                new WorldMapNode
                {
                    X = 100, Y = 200, Text = "Mileth",
                    CheckSum = 0xABCD, MapId = 500, DestinationX = 40, DestinationY = 17,
                },
                new WorldMapNode
                {
                    X = 300, Y = 50, Text = "Rucesion",
                    CheckSum = 0x1234, MapId = 501, DestinationX = 10, DestinationY = 11,
                },
            ],
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<WorldMapPacket>().Subject;
        typed.FieldName.Should().Be("Temuair");
        typed.ImageIndex.Should().Be((byte)1);
        typed.Nodes.Should().HaveCount(2);

        typed.Nodes[0].Should().BeEquivalentTo(original.Nodes[0]);
        typed.Nodes[1].Should().BeEquivalentTo(original.Nodes[1]);
    }

    [Fact]
    public void RoundTrip_EmptyNodeList_PreservesHeader()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new WorldMapPacket { FieldName = "Empty", ImageIndex = 0 };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<WorldMapPacket>().Subject;
        typed.FieldName.Should().Be("Empty");
        typed.Nodes.Should().BeEmpty();
    }
}
