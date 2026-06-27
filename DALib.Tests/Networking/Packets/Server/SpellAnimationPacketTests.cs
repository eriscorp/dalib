using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x29 SpellAnimation (S->C) - pins both discriminated forms (targeted, leading serial
///     != 0; area, leading serial == 0), plus codec round-trips for each.
/// </summary>
public class SpellAnimationPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_Targeted_PinsKnownLayout()
    {
        var packet = new SpellAnimationPacket
        {
            TargetId = 0x0A,
            SourceId = 0x14,
            TargetAnimation = 100,
            SourceAnimation = 101,
            Speed = 10,
        };

        packet.IsAreaEffect.Should().BeFalse();

        // [00 00 00 0A] targetId [00 00 00 14] sourceId [00 64] tgtAnim [00 65] srcAnim [00 0A] speed
        packet.ToBody().Should().Equal(
            (byte)0x00, (byte)0x00, (byte)0x00, (byte)0x0A,
            (byte)0x00, (byte)0x00, (byte)0x00, (byte)0x14,
            (byte)0x00, (byte)0x64,
            (byte)0x00, (byte)0x65,
            (byte)0x00, (byte)0x0A);
    }

    [Fact]
    public void WriteBody_Area_PinsKnownLayout()
    {
        var packet = new SpellAnimationPacket
        {
            TargetId = 0,
            TargetAnimation = 100,
            Speed = 10,
            X = 5,
            Y = 6,
        };

        packet.IsAreaEffect.Should().BeTrue();

        // [00 00 00 00] marker [00 64] anim [00 0A] speed [00 05] X [00 06] Y
        packet.ToBody().Should().Equal(
            (byte)0x00, (byte)0x00, (byte)0x00, (byte)0x00,
            (byte)0x00, (byte)0x64,
            (byte)0x00, (byte)0x0A,
            (byte)0x00, (byte)0x05,
            (byte)0x00, (byte)0x06);
    }

    [Fact]
    public void RoundTrip_Targeted_PreservesFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new SpellAnimationPacket
        {
            TargetId = 0xDEADBEEF,
            SourceId = 0x12345678,
            TargetAnimation = 4321,
            SourceAnimation = 1234,
            Speed = 200,
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<SpellAnimationPacket>().Subject;
        typed.IsAreaEffect.Should().BeFalse();
        typed.TargetId.Should().Be(0xDEADBEEFu);
        typed.SourceId.Should().Be(0x12345678u);
        typed.TargetAnimation.Should().Be((ushort)4321);
        typed.SourceAnimation.Should().Be((ushort)1234);
        typed.Speed.Should().Be((ushort)200);
    }

    [Fact]
    public void RoundTrip_Area_PreservesFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new SpellAnimationPacket
        {
            TargetId = 0,
            TargetAnimation = 555,
            Speed = 150,
            X = 42,
            Y = 99,
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<SpellAnimationPacket>().Subject;
        typed.IsAreaEffect.Should().BeTrue();
        typed.TargetAnimation.Should().Be((ushort)555);
        typed.Speed.Should().Be((ushort)150);
        typed.X.Should().Be((ushort)42);
        typed.Y.Should().Be((ushort)99);
    }
}
