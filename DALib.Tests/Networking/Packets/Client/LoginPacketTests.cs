using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x03 Login (C->S) - verifies the XOR-masked integrity trailer encoding,
///     the reversed-mask wire byte order, the CRC computation over the XOR'd trailer bytes,
///     and the default install-fingerprint fallback.
/// </summary>
public class LoginPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void RoundTrip_PreservesAllFields()
    {
        var original = new LoginPacket
        {
            Name = "Alice",
            Password = "hunter2",
            Rand1 = 0x42,
            XorKey = 0x77,
            ServerHash = 0x11223344,
            ClientHash = 0xABCD,
            RandData = 0xDEADBEEF,
        };

        var parsed = LoginPacket.Parse(original.ToBody());

        parsed.Name.Should().Be("Alice");
        parsed.Password.Should().Be("hunter2");
        parsed.Rand1.Should().Be(0x42);
        parsed.XorKey.Should().Be(0x77);
        parsed.ServerHash.Should().Be(0x11223344);
        parsed.ClientHash.Should().Be(0xABCD);
        parsed.RandData.Should().Be(0xDEADBEEF);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new LoginPacket
        {
            Name = "Bob",
            Password = "p@ssw0rd",
            Rand1 = 0x10,
            XorKey = 0x55,
            ServerHash = LoginPacket.DefaultServerHash,
            ClientHash = 0x1234,
            RandData = 0xCAFEBABE,
        };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<LoginPacket>().Subject;
        typed.Name.Should().Be("Bob");
        typed.Password.Should().Be("p@ssw0rd");
        typed.Rand1.Should().Be(0x10);
        typed.XorKey.Should().Be(0x55);
        typed.ServerHash.Should().Be(LoginPacket.DefaultServerHash);
        typed.ClientHash.Should().Be(0x1234);
        typed.RandData.Should().Be(0xCAFEBABE);
    }

    [Fact]
    public void RoundTrip_WithGeneratedDefaults_PreservesName_Password_ServerHashFallback()
    {
        // Don't override the random/null fields; verify defaults are applied and roundtrip.
        var original = new LoginPacket { Name = "Charlie", Password = "secret" };

        var parsed = LoginPacket.Parse(original.ToBody());

        parsed.Name.Should().Be("Charlie");
        parsed.Password.Should().Be("secret");
        parsed.ServerHash.Should().Be(LoginPacket.DefaultServerHash);
    }

    [Fact]
    public void DefaultClientHash_IsCrc16OfServerHashLeBytes()
    {
        var packet = new LoginPacket
        {
            Name = "x",
            Password = "y",
            Rand1 = 0x00,
            XorKey = 0x00,
            ServerHash = LoginPacket.DefaultServerHash,
            // ClientHash null -> should default to CRC16 of [00, FF, 00, FF]
            RandData = 0x00000000,
        };

        var parsed = LoginPacket.Parse(packet.ToBody());

        // 0xFF00FF00 in LE memory order is [00, FF, 00, FF]. CRC-16-CCITT over those four
        // bytes is the documented well-known fallback value for a broken install fingerprint.
        byte[] leBytes = [0x00, 0xFF, 0x00, 0xFF];
        var expected = CrcCcitt.Compute(leBytes);
        parsed.ClientHash.Should().Be(expected);
    }

    [Fact]
    public void XorKey_DerivableFromWire()
    {
        // The internal xor key never appears on the wire directly - it's recovered as
        //   xorKey = ((rand1 + 0x3B) & 0xFF) ^ xorKey_wire
        // Build a packet with known rand1+xorKey, parse, and verify the parsed xorKey
        // matches what we set (proves the derivation is symmetric).
        var packet = new LoginPacket
        {
            Name = "x",
            Password = "y",
            Rand1 = 0xAA,
            XorKey = 0x99,
        };

        var parsed = LoginPacket.Parse(packet.ToBody());

        parsed.XorKey.Should().Be(0x99);
    }

    [Fact]
    public void Parse_TamperedCrc_Throws()
    {
        var packet = new LoginPacket
        {
            Name = "x",
            Password = "y",
            Rand1 = 0x00,
            XorKey = 0x00,
            ServerHash = 0,
            ClientHash = 0,
            RandData = 0,
        };

        var body = packet.ToBody();
        // The CRC trailer field lives at body offset (name + password) + 12..13. Flip a bit.
        body[^3] ^= 0xFF;

        var act = () => LoginPacket.Parse(body);

        act.Should().Throw<InvalidDataException>()
           .WithMessage("*CRC*");
    }

    [Fact]
    public void Parse_BadFixedMarker_Throws()
    {
        var packet = new LoginPacket
        {
            Name = "x",
            Password = "y",
            Rand1 = 0,
            XorKey = 0,
            ServerHash = 0,
            ClientHash = 0,
            RandData = 0,
        };

        var body = packet.ToBody();
        body[^1] = 0xFF; // last byte is the fixed marker

        var act = () => LoginPacket.Parse(body);

        act.Should().Throw<InvalidDataException>()
           .WithMessage("*marker*");
    }

    [Fact]
    public void WriteBody_PinsKnownLayoutWithAllOverridesZero()
    {
        // With all overrides set to zero, the XOR mask reduces to just the mask constants:
        //   wire[i] = field_byte ^ (0 + mask_base + reversed_offset)
        // So we can hand-compute the expected wire bytes.
        var packet = new LoginPacket
        {
            Name = "x",
            Password = "y",
            Rand1 = 0x00,
            XorKey = 0x00,
            ServerHash = 0x00000000,
            ClientHash = 0x0000,
            RandData = 0x00000000,
        };

        var body = packet.ToBody();

        // [01 'x' 01 'y']  -- name + password as string8
        // [00]              -- rand1
        // [00 ^ ((0 + 0x3B) & 0xFF ^ 0)] = [0x3B]  -- xorKey_wire
        // serverHash (0x00000000, mask base 0x8A, reversed):
        //   wire[0] = 0 ^ (0+0x8A+3) = 0x8D
        //   wire[1] = 0 ^ (0+0x8A+2) = 0x8C
        //   wire[2] = 0 ^ (0+0x8A+1) = 0x8B
        //   wire[3] = 0 ^ (0+0x8A+0) = 0x8A
        // clientHash (0x0000, mask base 0x5E, reversed):
        //   wire[0] = 0 ^ (0+0x5E+1) = 0x5F
        //   wire[1] = 0 ^ (0+0x5E+0) = 0x5E
        // randData (0x00000000, mask base 0x73, reversed):
        //   wire[0..3] = 0x76, 0x75, 0x74, 0x73
        // crc - computed over the 12 XOR'd bytes above, then mask base 0xA5 reversed.
        // Asserting the first 8 bytes of the trailer (everything pre-CRC) verifies the
        // mask layout; the CRC is what the round-trip tests already exercise.

        body[0].Should().Be(0x01); body[1].Should().Be((byte)'x');  // name string8
        body[2].Should().Be(0x01); body[3].Should().Be((byte)'y');  // password string8

        // Trailer starts at offset 4.
        body[4].Should().Be(0x00);    // rand1
        body[5].Should().Be(0x3B);    // xorKey_wire = (0+0x3B)&0xFF ^ 0

        // serverHash_xored
        body[6].Should().Be(0x8D);
        body[7].Should().Be(0x8C);
        body[8].Should().Be(0x8B);
        body[9].Should().Be(0x8A);

        // clientHash_xored
        body[10].Should().Be(0x5F);
        body[11].Should().Be(0x5E);

        // randData_xored
        body[12].Should().Be(0x76);
        body[13].Should().Be(0x75);
        body[14].Should().Be(0x74);
        body[15].Should().Be(0x73);

        // The fixed marker is the last body byte.
        body[^1].Should().Be(0x01);
    }

    [Fact]
    public void TrailerLength_IsExactly15Bytes()
    {
        var packet = new LoginPacket { Name = "x", Password = "y" };

        var body = packet.ToBody();
        // body = [string8 name][string8 password][15-byte trailer]
        // For "x"/"y" each is 2 bytes on the wire.
        body.Length.Should().Be(2 + 2 + 15);
    }
}
