using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x23 SetNotepad (C->S) - pins the [u8 Slot][string16 Message] layout (the
///     message uses a big-endian 16-bit length prefix).
/// </summary>
public class SetNotepadPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsSlotThenString16()
    {
        var packet = new SetNotepadPacket { Slot = 7, Message = "hi" };

        // [07 slot][00 02 msgLen BE][h i]
        packet.ToBody().Should().Equal(
            (byte)0x07,
            (byte)0x00, (byte)0x02, (byte)'h', (byte)'i');
    }

    [Fact]
    public void RoundTrip_PreservesAllFields()
    {
        var original = new SetNotepadPacket { Slot = 3, Message = "a longer note body" };

        var parsed = SetNotepadPacket.Parse(original.ToBody());

        parsed.Slot.Should().Be(3);
        parsed.Message.Should().Be("a longer note body");
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new SetNotepadPacket { Slot = 12, Message = "remember the milk" };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<SetNotepadPacket>().Subject;
        typed.Slot.Should().Be(12);
        typed.Message.Should().Be("remember the milk");
    }
}
