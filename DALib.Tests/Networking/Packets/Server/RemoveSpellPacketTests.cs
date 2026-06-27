using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x18 RemoveSpell (S->C) - pins the <c>[u8 slot]</c> body and the codec
///     round-trip.
/// </summary>
public class RemoveSpellPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new RemoveSpellPacket { Slot = 36 };

        // [24] slot - nothing else
        packet.ToBody().Should().Equal((byte)0x24);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new RemoveSpellPacket { Slot = 90 };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<RemoveSpellPacket>().Subject;
        typed.Slot.Should().Be((byte)90);
    }
}
