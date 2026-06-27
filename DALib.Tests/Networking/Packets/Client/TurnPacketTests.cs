using DALib.Definitions;
using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x11 Turn (C->S) - pins the single direction byte (no sequence byte, unlike
///     Walk) and the round-trip.
/// </summary>
public class TurnPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Theory]
    [InlineData(Direction.North, 0x00)]
    [InlineData(Direction.East, 0x01)]
    [InlineData(Direction.South, 0x02)]
    [InlineData(Direction.West, 0x03)]
    public void WriteBody_IsSingleDirectionByte(Direction direction, byte expected)
    {
        var packet = new TurnPacket { Direction = direction };

        packet.ToBody().Should().Equal(expected);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesDirection()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new TurnPacket { Direction = Direction.South };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<TurnPacket>().Subject;
        typed.Direction.Should().Be(Direction.South);
    }
}
