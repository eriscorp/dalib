using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x48 CancelCast (S->C) - a payload-free signal. Pins the default single-zero emit
///     and that parsing tolerates any trailing length.
/// </summary>
public class CancelCastPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_DefaultsToHybrasylSingleZero()
    {
        var packet = new CancelCastPacket();

        packet.ToBody().Should().Equal((byte)0x00);
    }

    [Fact]
    public void Parse_ToleratesEmptyBody()
    {
        var parsed = CancelCastPacket.Parse([]);

        parsed.Padding.Should().BeEmpty();
    }

    [Fact]
    public void RoundTrip_ThroughCodec_Succeeds()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new CancelCastPacket();

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        parsed.Should().BeOfType<CancelCastPacket>();
    }
}
