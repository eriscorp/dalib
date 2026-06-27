using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x29 DropItemOnCreature (C->S) - pins the 6-byte body (slot, u32 BE target id,
///     u8 count), the round-trip, and the big-endian target-id encoding.
/// </summary>
public class DropItemOnCreaturePacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new DropItemOnCreaturePacket { Slot = 0x05, TargetId = 0xDEADBEEF, Count = 0x2A };

        // [05] slot [DE AD BE EF] u32-BE target id [2A] count
        packet.ToBody().Should().Equal(
            (byte)0x05,
            (byte)0xDE, (byte)0xAD, (byte)0xBE, (byte)0xEF,
            (byte)0x2A);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new DropItemOnCreaturePacket { Slot = 3, TargetId = 0x000A_BCDE, Count = 7 };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<DropItemOnCreaturePacket>().Subject;
        typed.Slot.Should().Be(3);
        typed.TargetId.Should().Be(0x000A_BCDEu);
        typed.Count.Should().Be((byte)7);
    }
}
