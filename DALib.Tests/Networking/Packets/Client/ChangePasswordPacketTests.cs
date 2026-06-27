using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x26 ChangePassword (C->S) - pins the [name][current][new] three-string8
///     layout and the round-trip.
/// </summary>
public class ChangePasswordPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsThreeString8Layout()
    {
        var packet = new ChangePasswordPacket
        {
            Name = "Eve",
            CurrentPassword = "old",
            NewPassword = "new",
        };

        // [03 nameLen][Eve] [03 curLen][old] [03 newLen][new]
        packet.ToBody().Should().Equal(
            (byte)0x03, (byte)'E', (byte)'v', (byte)'e',
            (byte)0x03, (byte)'o', (byte)'l', (byte)'d',
            (byte)0x03, (byte)'n', (byte)'e', (byte)'w');
    }

    [Fact]
    public void RoundTrip_PreservesAllFields()
    {
        var original = new ChangePasswordPacket
        {
            Name = "TestChar",
            CurrentPassword = "oldsecret",
            NewPassword = "newsecret",
        };

        var parsed = ChangePasswordPacket.Parse(original.ToBody());

        parsed.Name.Should().Be("TestChar");
        parsed.CurrentPassword.Should().Be("oldsecret");
        parsed.NewPassword.Should().Be("newsecret");
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new ChangePasswordPacket
        {
            Name = "Delta",
            CurrentPassword = "before",
            NewPassword = "after",
        };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<ChangePasswordPacket>().Subject;
        typed.Name.Should().Be("Delta");
        typed.CurrentPassword.Should().Be("before");
        typed.NewPassword.Should().Be("after");
    }
}
