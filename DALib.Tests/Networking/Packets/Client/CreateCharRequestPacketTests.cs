using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x02 CreateCharRequest (C->S) - pins the three-string8 layout (name,
///     password, email) and the round-trip.
/// </summary>
public class CreateCharRequestPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsThreeString8Layout()
    {
        var packet = new CreateCharRequestPacket
        {
            Name = "Alice",
            Password = "pw",
            Email = "a@b.co",
        };

        // [05 nameLen][Alice] [02 pwLen][pw] [06 emailLen][a@b.co]
        packet.ToBody().Should().Equal(
            (byte)0x05, (byte)'A', (byte)'l', (byte)'i', (byte)'c', (byte)'e',
            (byte)0x02, (byte)'p', (byte)'w',
            (byte)0x06, (byte)'a', (byte)'@', (byte)'b', (byte)'.', (byte)'c', (byte)'o');
    }

    [Fact]
    public void WriteBody_EmptyEmail_StillEmitsZeroLengthString8()
    {
        var packet = new CreateCharRequestPacket
        {
            Name = "Bob",
            Password = "x",
            Email = "",
        };

        // The empty email is still a string8 on the wire: a single 0x00 length byte.
        packet.ToBody().Should().Equal(
            (byte)0x03, (byte)'B', (byte)'o', (byte)'b',
            (byte)0x01, (byte)'x',
            (byte)0x00);
    }

    [Fact]
    public void RoundTrip_PreservesAllFields()
    {
        var original = new CreateCharRequestPacket
        {
            Name = "TestChar",
            Password = "secret",
            Email = "test@example.com",
        };

        var parsed = CreateCharRequestPacket.Parse(original.ToBody());

        parsed.Name.Should().Be("TestChar");
        parsed.Password.Should().Be("secret");
        parsed.Email.Should().Be("test@example.com");
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new CreateCharRequestPacket
        {
            Name = "Gamma",
            Password = "hunter2",
            Email = "g@host",
        };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<CreateCharRequestPacket>().Subject;
        typed.Name.Should().Be("Gamma");
        typed.Password.Should().Be("hunter2");
        typed.Email.Should().Be("g@host");
    }
}
