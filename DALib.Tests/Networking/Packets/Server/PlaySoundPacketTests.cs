using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x19 PlaySound (S->C) - pins both body shapes: the 1-byte sound-effect form and the
///     2-byte music form (<c>[0xFF][track]</c>).
/// </summary>
public class PlaySoundPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_SoundEffect_IsOneByte()
    {
        var packet = new PlaySoundPacket { Sound = 0x10 };

        packet.IsMusic.Should().BeFalse();
        packet.ToBody().Should().Equal((byte)0x10);
    }

    [Fact]
    public void WriteBody_Music_IsTwoBytes()
    {
        var packet = new PlaySoundPacket { Sound = PlaySoundPacket.MusicMarker, MusicTrack = 0x05 };

        // [FF] music marker [05] track - exactly two bytes
        packet.IsMusic.Should().BeTrue();
        packet.ToBody().Should().Equal((byte)0xFF, (byte)0x05);
    }

    [Fact]
    public void RoundTrip_SoundEffect_PreservesFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new PlaySoundPacket { Sound = 0x2A };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<PlaySoundPacket>().Subject;
        typed.Sound.Should().Be((byte)0x2A);
        typed.MusicTrack.Should().BeNull();
        typed.IsMusic.Should().BeFalse();
    }

    [Fact]
    public void RoundTrip_Music_PreservesTrack()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new PlaySoundPacket { Sound = PlaySoundPacket.MusicMarker, MusicTrack = 0x09 };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<PlaySoundPacket>().Subject;
        typed.IsMusic.Should().BeTrue();
        typed.MusicTrack.Should().Be((byte)0x09);
    }
}
