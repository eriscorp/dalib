using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x3F MapPointClick (C->S) - pins the 8-byte body (four u16 BE handles:
///     CheckSum, MapId, X, Y) and the round-trip through the codec.
/// </summary>
public class MapPointClickPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new MapPointClickPacket
        {
            CheckSum = 0xABCD,
            MapId = 0x1234,
            X = 0x0005,
            Y = 0x0009,
        };

        // [AB CD] checksum [12 34] mapId [00 05] x [00 09] y - all u16 big-endian
        packet.ToBody().Should().Equal(
            (byte)0xAB, (byte)0xCD,
            (byte)0x12, (byte)0x34,
            (byte)0x00, (byte)0x05,
            (byte)0x00, (byte)0x09);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new MapPointClickPacket
        {
            CheckSum = 40000,
            MapId = 513,
            X = 17,
            Y = 42,
        };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<MapPointClickPacket>().Subject;
        typed.CheckSum.Should().Be((ushort)40000);
        typed.MapId.Should().Be((ushort)513);
        typed.X.Should().Be((ushort)17);
        typed.Y.Should().Be((ushort)42);
    }
}
