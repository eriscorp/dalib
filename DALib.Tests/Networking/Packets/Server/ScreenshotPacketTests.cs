using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x6B Screenshot (S->C) - a one-byte body. Modeled for protocol completeness;
///     not emitted by typical servers.
/// </summary>
public class ScreenshotPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsSingleByte()
    {
        new ScreenshotPacket { Value = 0x01 }.ToBody().Should().Equal((byte)0x01);
    }

    [Fact]
    public void Parse_ReadsSingleByte()
    {
        ScreenshotPacket.Parse([0x01]).Value.Should().Be((byte)0x01);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0xFF)]
    public void RoundTrip_ThroughCodec_PreservesValue(byte value)
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new ScreenshotPacket { Value = value };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        parsed.Should().BeOfType<ScreenshotPacket>().Subject.Value.Should().Be(value);
    }
}
