using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x1D Emote (C->S) - pins the single raw emote-index byte (0-35; 9 is added to
///     reach the body animation) and the round-trip.
/// </summary>
public class EmotePacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(35)]
    public void WriteBody_IsSingleEmoteIndexByte(byte index)
    {
        var packet = new EmotePacket { EmoteIndex = index };

        packet.ToBody().Should().Equal(index);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesEmoteIndex()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new EmotePacket { EmoteIndex = 22 };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<EmotePacket>().Subject;
        typed.EmoteIndex.Should().Be(22);
    }
}
