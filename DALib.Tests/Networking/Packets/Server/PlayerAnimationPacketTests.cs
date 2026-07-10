using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x1A PlayerAnimation (S->C) - pins the <c>[u32 BE sourceId][u8 animation][u16 speed]</c>
///     body and Parse tolerance of emitter slack / pad bytes after Speed.
/// </summary>
public class PlayerAnimationPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new PlayerAnimationPacket { SourceId = 0x11223344, Animation = 0x06, Speed = 100 };

        // [11 22 33 44] sourceId BE [06] animation [00 64] speed=100 - no trailing byte
        packet.ToBody().Should().Equal(
            (byte)0x11, (byte)0x22, (byte)0x33, (byte)0x44,
            (byte)0x06,
            (byte)0x00, (byte)0x64);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new PlayerAnimationPacket
        {
            SourceId = 0xDEADBEEF, Animation = 0x80, Speed = 250,
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<PlayerAnimationPacket>().Subject;
        typed.SourceId.Should().Be(0xDEADBEEFu);
        typed.Animation.Should().Be((byte)0x80);
        typed.Speed.Should().Be((ushort)250);
    }

    [Fact]
    public void Parse_ToleratesEmitterSlackByte()
    {
        // Hybrasyl appends a 0xFF slack byte after Speed; the client never reads it and neither do we.
        var parsed = PlayerAnimationPacket.Parse(
            [0x11, 0x22, 0x33, 0x44, 0x06, 0x00, 0x64, 0xFF]);

        parsed.SourceId.Should().Be(0x11223344u);
        parsed.Animation.Should().Be((byte)0x06);
        parsed.Speed.Should().Be((ushort)100);
    }

    [Fact]
    public void Parse_ToleratesSlackPlusInnerPad()
    {
        // Emitter slack plus a DOOMVAS inner-pad byte: both tolerated.
        var parsed = PlayerAnimationPacket.Parse(
            [0x11, 0x22, 0x33, 0x44, 0x06, 0x00, 0x64, 0xFF, 0x00]);

        parsed.Speed.Should().Be((ushort)100);
    }
}
