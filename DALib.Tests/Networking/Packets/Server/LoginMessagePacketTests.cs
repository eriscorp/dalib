using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

public class LoginMessagePacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new LoginMessagePacket
        {
            Type = LoginMessagePacket.TypeIncorrectPassword,
            Message = "Incorrect password",
        };

        // [0F type][12 length=18][I n c o r r e c t   p a s s w o r d]
        packet.ToBody().Should().Equal(
            (byte)0x0F,
            (byte)0x12,
            (byte)'I', (byte)'n', (byte)'c', (byte)'o', (byte)'r', (byte)'r', (byte)'e', (byte)'c', (byte)'t',
            (byte)' ', (byte)'p', (byte)'a', (byte)'s', (byte)'s', (byte)'w', (byte)'o', (byte)'r', (byte)'d');
    }

    [Fact]
    public void WriteBody_SuccessConvention_NullMessage()
    {
        // Convention: on success the server sends a single-null-byte message.
        var packet = new LoginMessagePacket
        {
            Type = LoginMessagePacket.TypeSuccess,
            Message = "\0",
        };

        packet.ToBody().Should().Equal((byte)0x00, (byte)0x01, (byte)0x00);
    }

    [Fact]
    public void RoundTrip_PreservesAllFields()
    {
        var original = new LoginMessagePacket
        {
            Type = LoginMessagePacket.TypeError,
            Message = "Some error message",
        };

        var parsed = LoginMessagePacket.Parse(original.ToBody());

        parsed.Type.Should().Be(LoginMessagePacket.TypeError);
        parsed.Message.Should().Be("Some error message");
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new LoginMessagePacket
        {
            Type = LoginMessagePacket.TypePasswordTooSimple,
            Message = "Password too simple",
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<LoginMessagePacket>().Subject;
        typed.Type.Should().Be(LoginMessagePacket.TypePasswordTooSimple);
        typed.Message.Should().Be("Password too simple");
    }

    [Fact]
    public void RoundTrip_UndocumentedTypeCode_PreservedVerbatim()
    {
        // Undocumented type codes (0xFF, etc.) must round-trip as-is - we use a byte field
        // rather than an enum precisely so consumers aren't blocked by codes we haven't seen.
        var original = new LoginMessagePacket { Type = 0xFF, Message = "future" };

        var parsed = LoginMessagePacket.Parse(original.ToBody());

        parsed.Type.Should().Be(0xFF);
        parsed.Message.Should().Be("future");
    }
}
