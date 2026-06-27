using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x58 MapLoadComplete (S->C). Pins the default <c>[u16 0]</c> payload, the
///     <c>[u8 0]</c> variant, and the codec round-trip of both.
/// </summary>
public class MapLoadCompletePacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_DefaultHybrasylForm_PinsKnownLayout()
    {
        var packet = new MapLoadCompletePacket();

        // [00 00] - the default u16 zero form
        packet.ToBody().Should().Equal((byte)0x00, (byte)0x00);
    }

    [Fact]
    public void WriteBody_ChaosForm_PinsKnownLayout()
    {
        var packet = new MapLoadCompletePacket { Padding = [0x00] };

        // [00] - the single-byte variant
        packet.ToBody().Should().Equal((byte)0x00);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesPadding()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new MapLoadCompletePacket { Padding = [0x00] };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<MapLoadCompletePacket>().Subject;
        typed.Padding.Should().Equal(0x00);
    }
}
