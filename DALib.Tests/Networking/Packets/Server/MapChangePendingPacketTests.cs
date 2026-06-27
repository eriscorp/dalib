using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x67 MapChangePending (S->C), the map-transition signal. Pins the default 6-byte
///     payload, byte-faithful round-trips, and the codec round-trip.
/// </summary>
public class MapChangePendingPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_Default_PinsReferenceServerPayload()
    {
        var packet = new MapChangePendingPacket();

        // [03 00 00 00 00 00] - the fixed default body
        packet.ToBody().Should().Equal(
            (byte)0x03, (byte)0x00, (byte)0x00, (byte)0x00, (byte)0x00, (byte)0x00);
    }

    [Fact]
    public void RoundTrip_PreservesPayload()
    {
        var packet = new MapChangePendingPacket { Payload = [0x03, 0x11, 0x22] };

        var parsed = MapChangePendingPacket.Parse(packet.ToBody());

        parsed.Payload.Should().Equal((byte)0x03, (byte)0x11, (byte)0x22);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesPayload()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new MapChangePendingPacket();

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<MapChangePendingPacket>().Subject;
        typed.Payload.Should().Equal((byte)0x03, (byte)0x00, (byte)0x00, (byte)0x00, (byte)0x00, (byte)0x00);
    }
}
