using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x7E AcceptConnection (S->C) - the unencrypted lobby connection greeting. Pins the
///     <c>[u8 Marker]["CONNECTED SERVER"]</c> body, the standard defaults, and the
///     codec round-trip (which must leave 0x7E unencrypted).
/// </summary>
public class AcceptConnectionPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_Defaults_PinKnownGreeting()
    {
        var packet = new AcceptConnectionPacket();

        // [1B] marker, then the raw "CONNECTED SERVER" banner (no terminator)
        packet.ToBody().Should().Equal(
            (byte)0x1B,
            (byte)'C', (byte)'O', (byte)'N', (byte)'N', (byte)'E', (byte)'C', (byte)'T', (byte)'E', (byte)'D',
            (byte)' ',
            (byte)'S', (byte)'E', (byte)'R', (byte)'V', (byte)'E', (byte)'R');
    }

    [Fact]
    public void RoundTrip_PreservesMarkerAndMessage()
    {
        var packet = new AcceptConnectionPacket { Marker = 0x1B, Message = "CONNECTED SERVER" };

        var parsed = AcceptConnectionPacket.Parse(packet.ToBody());

        parsed.Marker.Should().Be((byte)0x1B);
        parsed.Message.Should().Be("CONNECTED SERVER");
    }

    [Fact]
    public void RoundTrip_ThroughCodec_StaysUnencryptedAndPreservesFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new AcceptConnectionPacket();

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<AcceptConnectionPacket>().Subject;
        typed.Marker.Should().Be((byte)0x1B);
        typed.Message.Should().Be("CONNECTED SERVER");
    }
}
