using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x30 SwapSlot (C->S) - pins the 3-byte body (window, slot1, slot2) and the
///     round-trip.
/// </summary>
public class SwapSlotPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new SwapSlotPacket { Window = 0x01, Slot1 = 0x03, Slot2 = 0x07 };

        // [01] window [03] slot1 [07] slot2
        packet.ToBody().Should().Equal((byte)0x01, (byte)0x03, (byte)0x07);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new SwapSlotPacket { Window = 0, Slot1 = 12, Slot2 = 34 };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<SwapSlotPacket>().Subject;
        typed.Window.Should().Be((byte)0);
        typed.Slot1.Should().Be((byte)12);
        typed.Slot2.Should().Be((byte)34);
    }
}
