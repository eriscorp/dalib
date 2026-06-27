using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x45 ByteHeartbeat (C->S) - pins the two-byte body (wire order) and the
///     round-trip.
/// </summary>
public class ByteHeartbeatPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new ByteHeartbeatPacket { First = 0xAB, Second = 0xCD };

        // [AB] first [CD] second
        packet.ToBody().Should().Equal((byte)0xAB, (byte)0xCD);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesBothBytes()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new ByteHeartbeatPacket { First = 0x11, Second = 0x22 };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<ByteHeartbeatPacket>().Subject;
        typed.First.Should().Be((byte)0x11);
        typed.Second.Should().Be((byte)0x22);
    }
}
