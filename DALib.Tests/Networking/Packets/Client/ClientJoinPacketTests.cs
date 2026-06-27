using System.Net;
using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x10 ClientJoin (C->S) - verifies the wire layout, the round-trip, and
///     the protocol invariant that the 0x10 body equals the inner payload of the
///     corresponding <see cref="RedirectPacket" />.
/// </summary>
public class ClientJoinPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    private static readonly byte[] NineByteKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09];

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new ClientJoinPacket
        {
            EncryptionSeed = 0x05,
            EncryptionKey = NineByteKey,
            Name = "socket",
            RedirectId = 0xDEADBEEF,
        };

        // [05] [09 keylen][9-byte key] [06 nameLen][s o c k e t] [DE AD BE EF]
        packet.ToBody().Should().Equal(
            (byte)0x05,
            (byte)0x09,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
            (byte)0x06, (byte)'s', (byte)'o', (byte)'c', (byte)'k', (byte)'e', (byte)'t',
            (byte)0xDE, (byte)0xAD, (byte)0xBE, (byte)0xEF);
    }

    [Fact]
    public void RoundTrip_PreservesAllFields()
    {
        var original = new ClientJoinPacket
        {
            EncryptionSeed = 0x42,
            EncryptionKey = NineByteKey,
            Name = "TestCharacter",
            RedirectId = 0x12345678,
        };

        var parsed = ClientJoinPacket.Parse(original.ToBody());

        parsed.EncryptionSeed.Should().Be(0x42);
        parsed.EncryptionKey.Should().Equal(NineByteKey);
        parsed.Name.Should().Be("TestCharacter");
        parsed.RedirectId.Should().Be(0x12345678);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new ClientJoinPacket
        {
            EncryptionSeed = 0xAB,
            EncryptionKey = NineByteKey,
            Name = "socket",
            RedirectId = 0xCAFEBABE,
        };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<ClientJoinPacket>().Subject;
        typed.EncryptionSeed.Should().Be(0xAB);
        typed.EncryptionKey.Should().Equal(NineByteKey);
        typed.Name.Should().Be("socket");
        typed.RedirectId.Should().Be(0xCAFEBABE);
    }

    [Fact]
    public void FromRedirect_CopiesAllCredentialFields()
    {
        var redirect = new RedirectPacket
        {
            IpAddress = IPAddress.Parse("10.0.0.1"),
            Port = 2611,
            EncryptionSeed = 0x77,
            EncryptionKey = NineByteKey,
            Name = "Alpha",
            RedirectId = 0xABCDEF01,
        };

        var join = ClientJoinPacket.FromRedirect(redirect);

        join.EncryptionSeed.Should().Be(0x77);
        join.EncryptionKey.Should().Equal(NineByteKey);
        join.Name.Should().Be("Alpha");
        join.RedirectId.Should().Be(0xABCDEF01);
    }

    [Fact]
    public void FromRedirect_BodyEqualsRedirectsInnerPayload()
    {
        // The protocol invariant: the 0x10 body is the redirect's inner data (everything
        // after the address+port+innerLength prefix) copied verbatim. So our
        // ClientJoinPacket body must equal that slice byte-for-byte.
        var redirect = new RedirectPacket
        {
            IpAddress = IPAddress.Parse("127.0.0.1"),
            Port = 0x0A41,
            EncryptionSeed = 0x05,
            EncryptionKey = NineByteKey,
            Name = "socket",
            RedirectId = 0xDEADBEEF,
        };

        var redirectBody = redirect.ToBody();
        // Skip [4-byte IP][u16 port][u8 innerLength] = 7 bytes of header.
        var redirectInner = redirectBody.AsSpan(7).ToArray();

        var joinBody = ClientJoinPacket.FromRedirect(redirect).ToBody();

        joinBody.Should().Equal(redirectInner);
    }
}
