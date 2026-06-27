using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x00 CryptoKey (S->C). Verifies the subtype-0 wire layout, that the
///     subtype discriminator is enforced, and that the typed parse agrees with the raw
///     decoder <see cref="CryptoState.UpdateFromLobbyResponse" />.
/// </summary>
public class CryptoKeyPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    private static readonly byte[] NineByteKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09];

    [Fact]
    public void WriteBody_EmitsSubtypeCrcSeedKeyLenKey()
    {
        var packet = new CryptoKeyPacket { ServerTableCrc = 0x12345678, Seed = 0x05, Key = NineByteKey };

        // [00 subtype][12 34 56 78 crc BE][05 seed][09 keyLen][key x9]
        packet.ToBody().Should().Equal(
            (byte)0x00,
            (byte)0x12, (byte)0x34, (byte)0x56, (byte)0x78,
            (byte)0x05,
            (byte)0x09,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09);
    }

    [Fact]
    public void EncodeServer_ProducesFramedBytes_NoTrailingNull()
    {
        var codec = new PacketCodec(); // scans DALib assembly; CryptoKeyPacket is real
        var packet = new CryptoKeyPacket { ServerTableCrc = 0x12345678, Seed = 0x05, Key = NineByteKey };

        var wire = codec.EncodeServer(packet, MakeCrypto()).ToArray();

        // [0xAA][u16 len=0x0011][0x00 op][0x00 subtype][crc][seed][keyLen][key] - S->C has no trailing null
        wire.Should().Equal(
            (byte)0xAA,
            (byte)0x00, (byte)0x11,
            (byte)0x00,
            (byte)0x00,
            (byte)0x12, (byte)0x34, (byte)0x56, (byte)0x78,
            (byte)0x05,
            (byte)0x09,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new CryptoKeyPacket { ServerTableCrc = 0xDEADBEEF, Seed = 0x07, Key = NineByteKey };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<CryptoKeyPacket>().Subject;
        typed.ServerTableCrc.Should().Be(0xDEADBEEF);
        typed.Seed.Should().Be(0x07);
        typed.Key.Should().Equal(NineByteKey);
    }

    [Theory]
    [InlineData((byte)0x01)] // Notice
    [InlineData((byte)0x02)] // Patch
    public void Parse_NonCryptoKeySubtype_Throws(byte subtype)
    {
        // [subtype][crc][seed][keyLen=0]
        byte[] body = [subtype, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

        var act = () => CryptoKeyPacket.Parse(body);

        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Parse_AgreesWith_UpdateFromLobbyResponse()
    {
        // The typed parse must extract the same seed/key as the raw decoder.
        var packet = new CryptoKeyPacket { ServerTableCrc = 0x12345678, Seed = 0x05, Key = NineByteKey };
        var body = packet.ToBody();

        // UpdateFromLobbyResponse operates on the payload WITH the opcode at [0].
        var payload = new byte[1 + body.Length];
        payload[0] = 0x00;
        body.CopyTo(payload, 1);

        var crypto = new CryptoState();
        crypto.UpdateFromLobbyResponse(payload);

        var parsed = CryptoKeyPacket.Parse(body);
        parsed.Seed.Should().Be(crypto.EncryptionSeed);
        parsed.Key.Should().Equal(crypto.EncryptionKey);
    }
}
