using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x1C UseItem (C->S) - pins the single inventory-slot byte and the round-trip.
/// </summary>
public class UseItemPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(255)]
    public void WriteBody_IsSingleSlotByte(byte slot)
    {
        var packet = new UseItemPacket { Slot = slot };

        packet.ToBody().Should().Equal(slot);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesSlot()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new UseItemPacket { Slot = 12 };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<UseItemPacket>().Subject;
        typed.Slot.Should().Be(12);
    }
}
