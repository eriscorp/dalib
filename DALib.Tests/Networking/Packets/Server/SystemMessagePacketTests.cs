using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x0A SystemMessage (S->C) - pins the <c>[u8 type][string16 message]</c> body
///     (note the <c>u16</c> length prefix) and the codec round-trip.
/// </summary>
public class SystemMessagePacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new SystemMessagePacket
        {
            MessageType = SystemMessageType.ActiveMessage,
            Message = "Hi",
        };

        // [03] type [00 02] u16-BE len [H i]
        packet.ToBody().Should().Equal(
            (byte)0x03,
            (byte)0x00, (byte)0x02,
            (byte)'H', (byte)'i');
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new SystemMessagePacket
        {
            MessageType = SystemMessageType.ScrollWindow,
            Message = "the quick brown fox",
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<SystemMessagePacket>().Subject;
        typed.MessageType.Should().Be(SystemMessageType.ScrollWindow);
        typed.Message.Should().Be("the quick brown fox");
    }
}
