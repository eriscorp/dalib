using System.Net;
using System.Net.Sockets;
using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x03 Redirect (S->C) - verifies the reversed-IP byte order, the
///     computed inner-length byte, and the round-trip through all typed fields.
/// </summary>
public class RedirectPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    private static readonly byte[] NineByteKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09];

    [Fact]
    public void WriteBody_PinsKnownLayout_ReversedIp_InnerLength_AndAllFields()
    {
        var packet = new RedirectPacket
        {
            IpAddress = IPAddress.Parse("127.0.0.1"),
            Port = 0x0A41,           // 2625
            EncryptionSeed = 0x05,
            EncryptionKey = NineByteKey,
            Name = "socket",
            RedirectId = 0xDEADBEEF,
        };

        var body = packet.ToBody();

        // [01 00 00 7F]                       - IP reversed (127.0.0.1 -> 01.00.00.7F)
        // [0A 41]                              - port BE
        // [16]                                 - innerLength = 9 (key) + 6 (name) + 7 = 22 = 0x16
        // [05]                                 - seed
        // [09]                                 - keyLength
        // [01 02 03 04 05 06 07 08 09]        - key
        // [06] [73 6F 63 6B 65 74]            - name length + "socket"
        // [DE AD BE EF]                       - redirectId BE
        body.Should().Equal(
            (byte)0x01, (byte)0x00, (byte)0x00, (byte)0x7F,
            (byte)0x0A, (byte)0x41,
            (byte)0x16,
            (byte)0x05,
            (byte)0x09,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
            (byte)0x06, (byte)'s', (byte)'o', (byte)'c', (byte)'k', (byte)'e', (byte)'t',
            (byte)0xDE, (byte)0xAD, (byte)0xBE, (byte)0xEF);
    }

    [Fact]
    public void RoundTrip_PreservesAllFields()
    {
        var original = new RedirectPacket
        {
            IpAddress = IPAddress.Parse("203.0.113.10"),
            Port = 2611,
            EncryptionSeed = 0x42,
            EncryptionKey = NineByteKey,
            Name = "TestCharacter",
            RedirectId = 0x12345678,
        };

        var parsed = RedirectPacket.Parse(original.ToBody());

        parsed.IpAddress.ToString().Should().Be("203.0.113.10");
        parsed.IpAddress.AddressFamily.Should().Be(AddressFamily.InterNetwork);
        parsed.Port.Should().Be(2611);
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
        var original = new RedirectPacket
        {
            IpAddress = IPAddress.Parse("10.20.30.40"),
            Port = 2610,
            EncryptionSeed = 0xAB,
            EncryptionKey = NineByteKey,
            Name = "socket",
            RedirectId = 0xCAFEBABE,
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<RedirectPacket>().Subject;
        typed.IpAddress.ToString().Should().Be("10.20.30.40");
        typed.Port.Should().Be(2610);
        typed.EncryptionSeed.Should().Be(0xAB);
        typed.EncryptionKey.Should().Equal(NineByteKey);
        typed.Name.Should().Be("socket");
        typed.RedirectId.Should().Be(0xCAFEBABE);
    }

    [Fact]
    public void WriteBody_IPv6Address_Throws()
    {
        var packet = new RedirectPacket
        {
            IpAddress = IPAddress.IPv6Loopback,
            Port = 2610,
            EncryptionSeed = 0,
            EncryptionKey = [],
            Name = "x",
            RedirectId = 0,
        };

        var act = () => packet.ToBody();

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*non-IPv4*");
    }

    [Fact]
    public void InnerLengthByte_ReflectsKeyAndNameLengths()
    {
        // Inner length = key.Length + name.Length + 7 (seed=1 + keyLen=1 + nameLen=1 + redirectId=4)
        // For 4-byte key + 3-byte name: 4 + 3 + 7 = 14 = 0x0E
        var packet = new RedirectPacket
        {
            IpAddress = IPAddress.Loopback,
            Port = 1,
            EncryptionSeed = 0,
            EncryptionKey = [0x11, 0x22, 0x33, 0x44],
            Name = "abc",
            RedirectId = 0,
        };

        var body = packet.ToBody();

        // The inner-length byte sits right after [4 bytes IP][2 bytes port] = offset 6.
        body[6].Should().Be(0x0E);
    }

    [Fact]
    public void WriteBody_LongName_ThrowsAtWireLimit()
    {
        var packet = new RedirectPacket
        {
            IpAddress = IPAddress.Loopback,
            Port = 1,
            EncryptionSeed = 0,
            EncryptionKey = [],
            Name = new string('x', 250),  // 250 + 7 = 257, exceeds u8 inner-length
            RedirectId = 0,
        };

        var act = () => packet.ToBody();

        act.Should().Throw<InvalidOperationException>();
    }
}
