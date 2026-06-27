using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x13 HealthBar (S->C) - pins the <c>[u32 BE sourceId][u8 0][u8 percent][u8 sound]</c>
///     body and the codec round-trip; confirms the default "no sound" (0xFF).
/// </summary>
public class HealthBarPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new HealthBarPacket { SourceId = 0x11223344, HealthPercent = 75 };

        // [11 22 33 44] sourceId BE [00] pad [4B] percent=75 [FF] sound=none
        packet.ToBody().Should().Equal(
            (byte)0x11, (byte)0x22, (byte)0x33, (byte)0x44,
            (byte)0x00,
            (byte)0x4B,
            (byte)0xFF);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new HealthBarPacket { SourceId = 0xDEADBEEF, HealthPercent = 100, Sound = 0x12 };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<HealthBarPacket>().Subject;
        typed.SourceId.Should().Be(0xDEADBEEFu);
        typed.HealthPercent.Should().Be((byte)100);
        typed.Sound.Should().Be((byte)0x12);
    }
}
