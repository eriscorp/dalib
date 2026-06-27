using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x24 DropGold (C->S) - pins the 8-byte body (u32 BE amount, u16 BE x,
///     u16 BE y), the round-trip, and the big-endian encodings.
/// </summary>
public class DropGoldPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new DropGoldPacket { Amount = 0xDEADBEEF, X = 0x1234, Y = 0xABCD };

        // [DE AD BE EF] u32-BE amount [12 34] u16-BE x [AB CD] u16-BE y
        packet.ToBody().Should().Equal(
            (byte)0xDE, (byte)0xAD, (byte)0xBE, (byte)0xEF,
            (byte)0x12, (byte)0x34,
            (byte)0xAB, (byte)0xCD);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new DropGoldPacket { Amount = 5000, X = 0x000A, Y = 0x000B };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<DropGoldPacket>().Subject;
        typed.Amount.Should().Be(5000u);
        typed.X.Should().Be(0x000A);
        typed.Y.Should().Be(0x000B);
    }
}
