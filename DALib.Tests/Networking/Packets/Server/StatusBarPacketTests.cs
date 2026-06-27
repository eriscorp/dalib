using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x3A StatusBar (S->C) - pins the <c>[u16 icon][u8 color]</c> body and the codec
///     round-trip; confirms a color byte above the named set survives verbatim.
/// </summary>
public class StatusBarPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new StatusBarPacket { Icon = 0x0102, Color = StatusBarColor.Green };

        // [01 02] icon BE [02] color=green
        packet.ToBody().Should().Equal((byte)0x01, (byte)0x02, (byte)0x02);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new StatusBarPacket { Icon = 0xABCD, Color = StatusBarColor.White };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<StatusBarPacket>().Subject;
        typed.Icon.Should().Be((ushort)0xABCD);
        typed.Color.Should().Be(StatusBarColor.White);
    }

    [Fact]
    public void RoundTrip_NoneColor_RemovesIcon()
    {
        // Color 0 (None) is the "remove this icon" signal; it must round-trip.
        var original = new StatusBarPacket { Icon = 0x0102, Color = StatusBarColor.None };

        var parsed = StatusBarPacket.Parse(original.ToBody());

        parsed.Icon.Should().Be((ushort)0x0102);
        parsed.Color.Should().Be(StatusBarColor.None);
    }

    [Fact]
    public void RoundTrip_UnknownColor_PreservedVerbatim()
    {
        // StatusBarColor is byte-backed: a value outside the named 0-6 set still round-trips.
        var original = new StatusBarPacket { Icon = 1, Color = (StatusBarColor)0x09 };

        var parsed = StatusBarPacket.Parse(original.ToBody());

        parsed.Icon.Should().Be((ushort)1);
        parsed.Color.Should().Be((StatusBarColor)0x09);
    }
}
