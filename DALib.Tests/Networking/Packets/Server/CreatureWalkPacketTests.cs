using DALib.Enums;
using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x0C CreatureWalk (S->C) - pins the <c>[u32 BE sourceId][u16 oldX][u16 oldY][u8
///     direction][u8 0]</c> body and the codec round-trip.
/// </summary>
public class CreatureWalkPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new CreatureWalkPacket
        {
            SourceId = 0x11223344,
            OldX = 0x0102,
            OldY = 0x0304,
            Direction = Direction.East,
        };

        // [11 22 33 44] sourceId BE [01 02] oldX [03 04] oldY [01] east [00] unknown
        packet.ToBody().Should().Equal(
            (byte)0x11, (byte)0x22, (byte)0x33, (byte)0x44,
            (byte)0x01, (byte)0x02,
            (byte)0x03, (byte)0x04,
            (byte)0x01,
            (byte)0x00);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new CreatureWalkPacket
        {
            SourceId = 0xDEADBEEF,
            OldX = 88,
            OldY = 254,
            Direction = Direction.North,
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<CreatureWalkPacket>().Subject;
        typed.SourceId.Should().Be(0xDEADBEEFu);
        typed.OldX.Should().Be((ushort)88);
        typed.OldY.Should().Be((ushort)254);
        typed.Direction.Should().Be(Direction.North);
        typed.Unknown.Should().Be((byte)0);
    }

    [Fact]
    public void RoundTrip_UnknownTailDeviation_PreservedVerbatim()
    {
        // The trailing byte's purpose is unknown; a deviating value must survive re-emit.
        var original = new CreatureWalkPacket
        {
            SourceId = 1,
            OldX = 2,
            OldY = 3,
            Direction = Direction.South,
            Unknown = 0x7F,
        };

        var parsed = CreatureWalkPacket.Parse(original.ToBody());

        parsed.Unknown.Should().Be((byte)0x7F);
    }
}
