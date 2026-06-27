using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x1F ChangeWeather (S->C) - pins the single-byte <c>[u8 weatherType]</c> body and
///     the codec round-trip.
/// </summary>
public class ChangeWeatherPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new ChangeWeatherPacket { WeatherType = 0x02 };

        // [02] weatherType - nothing else
        packet.ToBody().Should().Equal((byte)0x02);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new ChangeWeatherPacket { WeatherType = 5 };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<ChangeWeatherPacket>().Subject;
        typed.WeatherType.Should().Be((byte)5);
    }
}
