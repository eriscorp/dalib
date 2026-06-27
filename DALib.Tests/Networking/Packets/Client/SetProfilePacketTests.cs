using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x4F SetProfile (C->S) - pins the
///     [u16 totalLength][u16 portraitLength][portrait][string16 text] layout
///     and the computed totalLength.
/// </summary>
public class SetProfilePacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsLayout_AndComputesTotalLength()
    {
        var packet = new SetProfilePacket
        {
            PortraitData = [0xAA, 0xBB],
            ProfileText = "hi",
        };

        // totalLength = [u16 portraitLen] 2 + portrait 2 + [u16 msgLen] 2 + msg 2 = 8.
        // [00 08 total][00 02 portraitLen][AA BB][00 02 msgLen][h i]   (all BE)
        packet.ToBody().Should().Equal(
            (byte)0x00, (byte)0x08,
            (byte)0x00, (byte)0x02,
            (byte)0xAA, (byte)0xBB,
            (byte)0x00, (byte)0x02, (byte)'h', (byte)'i');
    }

    [Fact]
    public void WriteBody_EmptyPortrait_StillWellFormed()
    {
        var packet = new SetProfilePacket { PortraitData = [], ProfileText = "x" };

        // totalLength = 2 + 0 + 2 + 1 = 5. [00 05][00 00][00 01][x]
        packet.ToBody().Should().Equal(
            (byte)0x00, (byte)0x05,
            (byte)0x00, (byte)0x00,
            (byte)0x00, (byte)0x01, (byte)'x');
    }

    [Fact]
    public void RoundTrip_PreservesPortraitAndText()
    {
        var original = new SetProfilePacket
        {
            PortraitData = [1, 2, 3, 4, 5],
            ProfileText = "A profile of some length.",
        };

        var parsed = SetProfilePacket.Parse(original.ToBody());

        parsed.PortraitData.Should().Equal(1, 2, 3, 4, 5);
        parsed.ProfileText.Should().Be("A profile of some length.");
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesPortraitAndText()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new SetProfilePacket
        {
            PortraitData = [0xDE, 0xAD, 0xBE, 0xEF],
            ProfileText = "codec round trip",
        };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<SetProfilePacket>().Subject;
        typed.PortraitData.Should().Equal(0xDE, 0xAD, 0xBE, 0xEF);
        typed.ProfileText.Should().Be("codec round trip");
    }
}
