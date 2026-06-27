using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x0C RequestObject (C->S) - pins the 4-byte body (u32 BE object id) and
///     the round-trip.
/// </summary>
public class RequestObjectPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new RequestObjectPacket { ObjectId = 0xDEADBEEF };

        // [DE AD BE EF] u32-BE object id
        packet.ToBody().Should().Equal((byte)0xDE, (byte)0xAD, (byte)0xBE, (byte)0xEF);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesObjectId()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new RequestObjectPacket { ObjectId = 0x12345678 };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<RequestObjectPacket>().Subject;
        typed.ObjectId.Should().Be(0x12345678);
    }
}
