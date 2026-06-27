using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x4A BadGuy (S->C) - body [u8 Type][u8 Payload][u32 BE Magic]. Modeled for protocol
///     completeness; not emitted by typical servers.
/// </summary>
public class BadGuyPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsLayout()
    {
        // [00][2A][u32 BE 0x7D3AFF99]
        new BadGuyPacket { Type = 0, Payload = 0x2A, Magic = BadGuyPacket.MagicValue }
            .ToBody().Should().Equal((byte)0x00, (byte)0x2A, (byte)0x7D, (byte)0x3A, (byte)0xFF, (byte)0x99);
    }

    [Fact]
    public void Parse_ReadsLayout()
    {
        var parsed = BadGuyPacket.Parse([0x00, 0x2A, 0x7D, 0x3A, 0xFF, 0x99]);

        parsed.Type.Should().Be((byte)0x00);
        parsed.Payload.Should().Be((byte)0x2A);
        parsed.Magic.Should().Be(BadGuyPacket.MagicValue);
    }

    [Fact]
    public void MagicValue_IsTheGateConstant()
    {
        BadGuyPacket.MagicValue.Should().Be(0x7D3AFF99u);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new BadGuyPacket { Type = 0, Payload = 0xAB, Magic = BadGuyPacket.MagicValue };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        parsed.Should().Be(original);
    }
}
