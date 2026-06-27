using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     End-to-end coverage for 0x00 Version round-tripping through the
///     <see cref="PacketCodec" />, with the encoded frame pinned to a known-good byte sequence.
/// </summary>
public class VersionPacketTests
{
    // None-mode packets never touch crypto, but EncodeClient null-checks the state.
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_EmitsVersionThenLkSignature()
    {
        var packet = new VersionPacket();

        // Body excludes the opcode and the codec's universal trailing framing null.
        packet.ToBody().Should().Equal((byte)0x02, (byte)0xE5, (byte)0x4C, (byte)0x4B);
    }

    [Fact]
    public void EncodeClient_DefaultVersion_MatchesVerifiedLiveCapture()
    {
        var codec = new PacketCodec(); // scans the DALib assembly
        var packet = new VersionPacket();

        var wire = codec.EncodeClient(packet, MakeCrypto()).ToArray();

        // [0xAA][u16 len=0x0006][0x00 op][0x02E5 version]['L''K'][0x00 framing null]
        wire.Should().Equal(
            (byte)0xAA,
            (byte)0x00, (byte)0x06,
            (byte)0x00,
            (byte)0x02, (byte)0xE5,
            (byte)0x4C, (byte)0x4B,
            (byte)0x00);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesVersion()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new VersionPacket { Version = 0x1234 };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        parsed.Should().BeOfType<VersionPacket>()
            .Which.Version.Should().Be(0x1234);
    }

    [Fact]
    public void Parse_WrongSignature_Throws()
    {
        // version(2) + wrong 2-byte signature
        byte[] body = [0x02, 0xE5, 0x00, 0x00];

        var act = () => VersionPacket.Parse(body);

        act.Should().Throw<InvalidDataException>();
    }
}
