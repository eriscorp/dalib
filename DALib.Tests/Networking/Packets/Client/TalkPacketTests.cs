using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x0E Talk (C->S) - pins the body (chatType, string8 message), the chat-type
///     encoding, the empty-message-is-valid default, and the round-trip.
/// </summary>
public class TalkPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new TalkPacket { ChatType = ChatType.Shout, Message = "hi" };

        // [01] chatType (Shout) [02] len [h i]
        packet.ToBody().Should().Equal((byte)0x01, (byte)0x02, (byte)'h', (byte)'i');
    }

    [Fact]
    public void Defaults_AreSayAndEmptyMessage()
    {
        var packet = new TalkPacket();

        packet.ChatType.Should().Be(ChatType.Say);
        packet.Message.Should().Be(string.Empty);

        // [00] chatType (Say) [00] zero-length message - a blank line is valid protocol
        packet.ToBody().Should().Equal((byte)0x00, (byte)0x00);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new TalkPacket { ChatType = ChatType.Say, Message = "hello world" };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<TalkPacket>().Subject;
        typed.ChatType.Should().Be(ChatType.Say);
        typed.Message.Should().Be("hello world");
    }
}
