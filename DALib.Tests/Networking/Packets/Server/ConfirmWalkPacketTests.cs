using DALib.Enums;
using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x0B ConfirmWalk (S->C) - pins the <c>[u8 direction][u16 oldX][u16 oldY][u16
///     11][u16 11][u8 1]</c> body, the codec round-trip, and that the unknown-purpose tail
///     round-trips verbatim when it deviates from the default constants.
/// </summary>
public class ConfirmWalkPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new ConfirmWalkPacket
        {
            Direction = Direction.South,
            OldX = 0x0102,
            OldY = 0x0304,
        };

        // [02] south [01 02] oldX BE [03 04] oldY BE [00 0B][00 0B] unknown 11s [01] unknown
        packet.ToBody().Should().Equal(
            (byte)0x02,
            (byte)0x01, (byte)0x02,
            (byte)0x03, (byte)0x04,
            (byte)0x00, (byte)0x0B,
            (byte)0x00, (byte)0x0B,
            (byte)0x01);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new ConfirmWalkPacket
        {
            Direction = Direction.West,
            OldX = 40,
            OldY = 17,
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<ConfirmWalkPacket>().Subject;
        typed.Direction.Should().Be(Direction.West);
        typed.OldX.Should().Be((ushort)40);
        typed.OldY.Should().Be((ushort)17);
        typed.Unknown1.Should().Be((ushort)11);
        typed.Unknown2.Should().Be((ushort)11);
        typed.Unknown3.Should().Be((byte)1);
    }

    [Fact]
    public void RoundTrip_UnknownTailDeviation_PreservedVerbatim()
    {
        // The trailing [u16][u16][u8] purpose is unknown; a deviating value must
        // survive a parse/re-emit cycle rather than being normalized to the default constants.
        var original = new ConfirmWalkPacket
        {
            Direction = Direction.North,
            OldX = 1,
            OldY = 2,
            Unknown1 = 13,
            Unknown2 = 9,
            Unknown3 = 0,
        };

        var parsed = ConfirmWalkPacket.Parse(original.ToBody());

        parsed.Unknown1.Should().Be((ushort)13);
        parsed.Unknown2.Should().Be((ushort)9);
        parsed.Unknown3.Should().Be((byte)0);
    }
}
