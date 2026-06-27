using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x44 Unequip (C->S) - pins the single equipment-slot byte and the round-trip.
/// </summary>
public class UnequipPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(17)]
    public void WriteBody_IsSingleSlotByte(byte slot)
    {
        var packet = new UnequipPacket { Slot = slot };

        packet.ToBody().Should().Equal(slot);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesSlot()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new UnequipPacket { Slot = 3 };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<UnequipPacket>().Subject;
        typed.Slot.Should().Be(3);
    }
}
