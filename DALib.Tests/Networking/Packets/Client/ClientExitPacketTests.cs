using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x0B ClientExit (C->S) - pins the 1-byte body, the signal encoding
///     (Confirm = 0, Request = 1), and the round-trip.
/// </summary>
public class ClientExitPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Theory]
    [InlineData(ExitSignal.Confirm, (byte)0x00)]
    [InlineData(ExitSignal.Request, (byte)0x01)]
    public void WriteBody_PinsSignalByte(ExitSignal signal, byte expected)
    {
        var packet = new ClientExitPacket { Signal = signal };

        packet.ToBody().Should().Equal(expected);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesSignal()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new ClientExitPacket { Signal = ExitSignal.Confirm };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<ClientExitPacket>().Subject;
        typed.Signal.Should().Be(ExitSignal.Confirm);
    }
}
