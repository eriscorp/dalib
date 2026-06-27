using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x3D LevelPoint (S->C) - a two-byte body. Modeled for protocol completeness;
///     not emitted by typical servers.
/// </summary>
public class LevelPointPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsTwoBytes()
    {
        new LevelPointPacket { First = 0x11, Second = 0x22 }.ToBody().Should().Equal((byte)0x11, (byte)0x22);
    }

    [Fact]
    public void Parse_ReadsTwoBytes()
    {
        var parsed = LevelPointPacket.Parse([0x11, 0x22]);

        parsed.First.Should().Be((byte)0x11);
        parsed.Second.Should().Be((byte)0x22);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new LevelPointPacket { First = 0xAB, Second = 0xCD };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        parsed.Should().Be(original);
    }
}
