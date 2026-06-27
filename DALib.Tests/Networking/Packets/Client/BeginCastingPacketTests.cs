using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x4D BeginCasting (C->S) - pins the single-byte chant-line-count body and the
///     round-trip.
/// </summary>
public class BeginCastingPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Theory]
    [InlineData(0x01)] // Deoch Prayer, a one-line spell
    [InlineData(0x02)] // beag srad, a two-line spell
    public void WriteBody_IsSingleLineCountByte(byte lines)
    {
        var packet = new BeginCastingPacket { Lines = lines };

        packet.ToBody().Should().Equal(lines);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesLines()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new BeginCastingPacket { Lines = 0x04 };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<BeginCastingPacket>().Subject;
        typed.Lines.Should().Be((byte)0x04);
    }
}
