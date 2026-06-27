using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x19 Whisper (C->S) - pins the <c>[string8 target][string8 message]</c> body
///     and the round-trip. Target "!" denotes guild chat and "!!" denotes group chat.
/// </summary>
public class WhisperPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsCapturedGuildLayout()
    {
        // guild chat = whisper to "!"; body [01 21] "!" then [0F] length and "how's it going?".
        var packet = new WhisperPacket { Target = "!", Message = "how's it going?" };

        packet.ToBody().Should().Equal(
            (byte)0x01, (byte)0x21,                                     // [01] "!"
            (byte)0x0F,                                                 // [0F] message length 15
            (byte)0x68, (byte)0x6F, (byte)0x77, (byte)0x27, (byte)0x73, // "how's"
            (byte)0x20, (byte)0x69, (byte)0x74,                         // " it"
            (byte)0x20, (byte)0x67, (byte)0x6F, (byte)0x69, (byte)0x6E, (byte)0x67, // " going"
            (byte)0x3F);                                                // "?"
    }

    [Fact]
    public void RoundTrip_GroupTarget_PreservesTargetAndMessage()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new WhisperPacket { Target = "!!", Message = "hi" };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<WhisperPacket>().Subject;
        typed.Target.Should().Be("!!");
        typed.Message.Should().Be("hi");
    }

    [Fact]
    public void RoundTrip_PlayerTarget_PreservesTargetAndMessage()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new WhisperPacket { Target = "Kedian", Message = "hello there" };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<WhisperPacket>().Subject;
        typed.Target.Should().Be("Kedian");
        typed.Message.Should().Be("hello there");
    }
}
