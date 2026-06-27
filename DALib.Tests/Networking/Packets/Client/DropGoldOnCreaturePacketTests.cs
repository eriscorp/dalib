using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x2A DropGoldOnCreature (C->S) - pins the 8-byte body (u32 BE amount, u32 BE
///     target id), the round-trip, and the big-endian encodings.
/// </summary>
public class DropGoldOnCreaturePacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new DropGoldOnCreaturePacket { Amount = 0x12345678, TargetId = 0xDEADBEEF };

        // [12 34 56 78] u32-BE amount [DE AD BE EF] u32-BE target id
        packet.ToBody().Should().Equal(
            (byte)0x12, (byte)0x34, (byte)0x56, (byte)0x78,
            (byte)0xDE, (byte)0xAD, (byte)0xBE, (byte)0xEF);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new DropGoldOnCreaturePacket { Amount = 5000, TargetId = 0x000A_BCDE };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<DropGoldOnCreaturePacket>().Subject;
        typed.Amount.Should().Be(5000u);
        typed.TargetId.Should().Be(0x000A_BCDEu);
    }
}
