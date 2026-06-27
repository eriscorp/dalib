using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x4C ConfirmExit (S->C) - pins the single-byte <c>[u8 ExitConfirmed]</c> body and
///     the codec round-trip.
/// </summary>
public class ConfirmExitPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_Confirmed_PinsKnownLayout()
    {
        var packet = new ConfirmExitPacket { ExitConfirmed = true };

        // [01] ExitConfirmed - nothing else
        packet.ToBody().Should().Equal((byte)0x01);
    }

    [Fact]
    public void WriteBody_NotConfirmed_PinsKnownLayout()
    {
        var packet = new ConfirmExitPacket { ExitConfirmed = false };

        packet.ToBody().Should().Equal((byte)0x00);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RoundTrip_ThroughCodec_PreservesField(bool confirmed)
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new ConfirmExitPacket { ExitConfirmed = confirmed };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<ConfirmExitPacket>().Subject;
        typed.ExitConfirmed.Should().Be(confirmed);
    }
}
