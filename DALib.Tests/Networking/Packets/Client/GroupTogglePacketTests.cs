using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x2F GroupToggle (C->S) - pins the empty body and the round-trip through the
///     MD5Key codec path.
/// </summary>
public class GroupTogglePacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_IsEmpty()
    {
        var packet = new GroupTogglePacket();

        packet.ToBody().Should().BeEmpty();
    }

    [Fact]
    public void RoundTrip_ThroughCodec_Parses()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new GroupTogglePacket();

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        parsed.Should().BeOfType<GroupTogglePacket>();
    }
}
