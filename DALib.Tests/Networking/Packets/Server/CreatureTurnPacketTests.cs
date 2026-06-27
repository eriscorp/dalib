using DALib.Definitions;
using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x11 CreatureTurn (S->C) - pins the <c>[u32 BE sourceId][u8 direction]</c> body
///     and the codec round-trip.
/// </summary>
public class CreatureTurnPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new CreatureTurnPacket
        {
            SourceId = 0x11223344,
            Direction = Direction.West,
        };

        // [11 22 33 44] sourceId BE [03] west
        packet.ToBody().Should().Equal(
            (byte)0x11, (byte)0x22, (byte)0x33, (byte)0x44,
            (byte)0x03);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new CreatureTurnPacket
        {
            SourceId = 0xDEADBEEF,
            Direction = Direction.East,
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<CreatureTurnPacket>().Subject;
        typed.SourceId.Should().Be(0xDEADBEEFu);
        typed.Direction.Should().Be(Direction.East);
    }
}
