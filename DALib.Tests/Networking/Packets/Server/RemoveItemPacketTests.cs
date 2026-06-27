using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x10 RemoveItem (S->C) - pins the minimal <c>[u8 slot]</c> body, the
///     form with three trailing slack bytes, and the codec round-trip of both.
/// </summary>
public class RemoveItemPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_MinimalForm_PinsKnownLayout()
    {
        var packet = new RemoveItemPacket { Slot = 12 };

        // [0C] slot - nothing else (minimal form)
        packet.ToBody().Should().Equal((byte)0x0C);
    }

    [Fact]
    public void WriteBody_HybrasylSlack_AppendsVerbatim()
    {
        var packet = new RemoveItemPacket
        {
            Slot = 12,
            TrailingSlack = [0x00, 0x00, 0x00],
        };

        // [0C] slot [00 00 00] trailing slack
        packet.ToBody().Should().Equal((byte)0x0C, (byte)0x00, (byte)0x00, (byte)0x00);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new RemoveItemPacket { Slot = 59 };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<RemoveItemPacket>().Subject;
        typed.Slot.Should().Be((byte)59);
        typed.TrailingSlack.Should().BeNull();
    }

    [Fact]
    public void RoundTrip_HybrasylSlack_PreservedVerbatim()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new RemoveItemPacket
        {
            Slot = 1,
            TrailingSlack = [0x00, 0x00, 0x00],
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<RemoveItemPacket>().Subject;
        typed.Slot.Should().Be((byte)1);
        typed.TrailingSlack.Should().Equal(0x00, 0x00, 0x00);
    }
}
