using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x75 TickHeartbeat (C->S) - pins the 8-byte body (u32 BE server tick, u32 BE
///     client tick), the round-trip, and the big-endian encodings.
/// </summary>
public class TickHeartbeatPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new TickHeartbeatPacket { ServerTick = 0x12345678, ClientTick = 0xDEADBEEF };

        // [12 34 56 78] u32-BE server tick [DE AD BE EF] u32-BE client tick
        packet.ToBody().Should().Equal(
            (byte)0x12, (byte)0x34, (byte)0x56, (byte)0x78,
            (byte)0xDE, (byte)0xAD, (byte)0xBE, (byte)0xEF);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesBothTicks()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new TickHeartbeatPacket { ServerTick = 100000, ClientTick = 250000 };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<TickHeartbeatPacket>().Subject;
        typed.ServerTick.Should().Be(100000u);
        typed.ClientTick.Should().Be(250000u);
    }
}
