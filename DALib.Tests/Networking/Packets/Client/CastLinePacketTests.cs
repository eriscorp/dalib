using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x4E CastLine (C->S) - pins the <c>[string8 line]</c> body and the round-trip.
/// </summary>
public class CastLinePacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsString8Layout()
    {
        // "please" encoded as a length-prefixed string8.
        var packet = new CastLinePacket { Line = "please" };

        // [06] string8 length ['p' 'l' 'e' 'a' 's' 'e']
        packet.ToBody().Should().Equal(
            (byte)0x06,
            (byte)0x70, (byte)0x6C, (byte)0x65, (byte)0x61, (byte)0x73, (byte)0x65);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesLine()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new CastLinePacket { Line = "mor fas nadur" };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<CastLinePacket>().Subject;
        typed.Line.Should().Be("mor fas nadur");
    }
}
