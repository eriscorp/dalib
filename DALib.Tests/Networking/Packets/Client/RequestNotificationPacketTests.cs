using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x4B RequestNotification (C->S) - a bodyless trigger that asks for the full login
///     notice (answered by S->C 0x60). Pins the empty body and the round-trip.
/// </summary>
public class RequestNotificationPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_IsEmpty()
    {
        var packet = new RequestNotificationPacket();

        packet.ToBody().Should().BeEmpty();
    }

    [Fact]
    public void RoundTrip_ThroughCodec_ParsesAsRequestNotification()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new RequestNotificationPacket();

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        parsed.Should().BeOfType<RequestNotificationPacket>();
    }
}
