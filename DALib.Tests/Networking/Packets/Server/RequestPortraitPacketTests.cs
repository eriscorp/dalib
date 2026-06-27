using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x49 RequestPortrait (S->C) - the payload-free "upload your portrait" prompt. The
///     two trailing zeros are inert; modeled with a settable, length-tolerant Padding. Pins the default
///     body, confirms parse tolerance, and round-trips through the codec.
/// </summary>
public class RequestPortraitPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_DefaultsToHybrasylTwoZeros()
    {
        new RequestPortraitPacket().ToBody().Should().Equal(0x00, 0x00);
    }

    [Fact]
    public void WriteBody_EmitsPaddingVerbatim()
    {
        new RequestPortraitPacket { Padding = [] }.ToBody().Should().BeEmpty();
        new RequestPortraitPacket { Padding = [0xAA] }.ToBody().Should().Equal(0xAA);
    }

    [Fact]
    public void Parse_ToleratesAnyLength()
    {
        RequestPortraitPacket.Parse([]).Padding.Should().BeEmpty();
        RequestPortraitPacket.Parse([0x00, 0x00]).Padding.Should().Equal(0x00, 0x00);
        RequestPortraitPacket.Parse([0x01, 0x02, 0x03]).Padding.Should().Equal(0x01, 0x02, 0x03);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesPadding()
    {
        var original = new RequestPortraitPacket();

        var codec = new PacketCodec();
        var crypto = MakeCrypto();

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        parsed.Should().BeOfType<RequestPortraitPacket>();
        parsed.Should().BeEquivalentTo(original);
    }
}
