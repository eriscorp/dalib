using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x22 Refresh (S->C) - the payload-free "redraw the view" signal. The single byte is
///     ceremonial; modeled with a settable, length-tolerant Padding for byte-faithful round-tripping.
/// </summary>
public class RefreshPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_DefaultsToHybrasylSingleZero()
        => new RefreshPacket().ToBody().Should().Equal(0x00);

    [Fact]
    public void WriteBody_EmitsPaddingVerbatim()
    {
        new RefreshPacket { Padding = [] }.ToBody().Should().BeEmpty();
        new RefreshPacket { Padding = [0xAA] }.ToBody().Should().Equal(0xAA);
    }

    [Fact]
    public void Parse_ToleratesAnyLength()
    {
        RefreshPacket.Parse([]).Padding.Should().BeEmpty();
        RefreshPacket.Parse([0x00]).Padding.Should().Equal(0x00);
        RefreshPacket.Parse([0x01, 0x02]).Padding.Should().Equal(0x01, 0x02);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesPadding()
    {
        var original = new RefreshPacket();

        var codec = new PacketCodec();
        var crypto = MakeCrypto();

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        parsed.Should().BeOfType<RefreshPacket>();
        parsed.Should().BeEquivalentTo(original);
    }
}
