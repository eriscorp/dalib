using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x21 SelfSave (S->C) - a payload-free signal. Modeled for protocol completeness;
///     not emitted by typical servers. Pins the empty body and the codec round-trip.
/// </summary>
public class SelfSavePacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_IsEmpty()
    {
        new SelfSavePacket().ToBody().Should().BeEmpty();
    }

    [Fact]
    public void Parse_ToleratesTrailingBytes()
    {
        // The body carries no fields, so any trailing bytes are discarded.
        SelfSavePacket.Parse([0xDE, 0xAD]).Should().Be(new SelfSavePacket());
    }

    [Fact]
    public void RoundTrip_ThroughCodec_Succeeds()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new SelfSavePacket();

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        parsed.Should().BeOfType<SelfSavePacket>();
    }
}
