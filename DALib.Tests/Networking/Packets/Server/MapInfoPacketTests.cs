using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x15 MapInfo (S->C). Pins the
///     <c>[u16 mapId][u8 width][u8 height][u8 flags][u16 reserved][u16 checksum][string8 name]</c>
///     body and the codec round-trip.
/// </summary>
public class MapInfoPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new MapInfoPacket
        {
            MapId = 0x0102,
            Width = 0x11,
            Height = 0x22,
            Flags = 0x03,
            Checksum = 0x0405,
            Name = "ab",
        };

        // [01 02] mapId BE [11] width [22] height [03] flags [00 00] reserved
        // [04 05] checksum BE [02 61 62] string8 "ab"
        packet.ToBody().Should().Equal(
            (byte)0x01, (byte)0x02,
            (byte)0x11,
            (byte)0x22,
            (byte)0x03,
            (byte)0x00, (byte)0x00,
            (byte)0x04, (byte)0x05,
            (byte)0x02, (byte)0x61, (byte)0x62);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new MapInfoPacket
        {
            MapId = 500,
            Width = 80,
            Height = 80,
            Flags = 0x42,
            Checksum = 0xBEEF,
            Name = "Mileth",
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<MapInfoPacket>().Subject;
        typed.MapId.Should().Be((ushort)500);
        typed.Width.Should().Be((byte)80);
        typed.Height.Should().Be((byte)80);
        typed.Flags.Should().Be((byte)0x42);
        typed.Checksum.Should().Be((ushort)0xBEEF);
        typed.Name.Should().Be("Mileth");
        typed.Reserved.Should().Be((ushort)0);
    }
}
