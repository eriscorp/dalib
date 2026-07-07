using DALib.Enums;
using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x06 Walk (C->S) - pins the 2-byte body (direction, sequence), the
///     round-trip, the default sequence, and the direction enum encoding.
/// </summary>
public class WalkPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new WalkPacket { Direction = Direction.South, Sequence = 0x2A };

        // [02] direction (South) [2A] sequence
        packet.ToBody().Should().Equal((byte)0x02, (byte)0x2A);
    }

    [Theory]
    [InlineData(Direction.North, (byte)0x00)]
    [InlineData(Direction.East, (byte)0x01)]
    [InlineData(Direction.South, (byte)0x02)]
    [InlineData(Direction.West, (byte)0x03)]
    public void WriteBody_EncodesDirectionByte(Direction direction, byte expected)
    {
        var packet = new WalkPacket { Direction = direction };

        packet.ToBody()[0].Should().Be(expected);
    }

    [Fact]
    public void Sequence_DefaultsToZero()
    {
        var packet = new WalkPacket { Direction = Direction.North };

        packet.Sequence.Should().Be(0);
        packet.ToBody().Should().Equal((byte)0x00, (byte)0x00);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new WalkPacket { Direction = Direction.West, Sequence = 0x7F };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<WalkPacket>().Subject;
        typed.Direction.Should().Be(Direction.West);
        typed.Sequence.Should().Be(0x7F);
    }
}
