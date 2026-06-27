using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x47 StatPoint (C->S) - pins the single stat-selector byte (0x01 Str, 0x02 Dex,
///     0x04 Int, 0x08 Wis, 0x10 Con) and the round-trip.
/// </summary>
public class StatPointPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Theory]
    [InlineData(0x01)] // Str
    [InlineData(0x02)] // Dex
    [InlineData(0x04)] // Int
    [InlineData(0x08)] // Wis
    [InlineData(0x10)] // Con
    public void WriteBody_IsSingleStatByte(byte stat)
    {
        var packet = new StatPointPacket { Stat = stat };

        packet.ToBody().Should().Equal(stat);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesStat()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new StatPointPacket { Stat = 0x08 };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<StatPointPacket>().Subject;
        typed.Stat.Should().Be((byte)0x08);
    }
}
