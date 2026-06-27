using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x0D PublicMessage (S->C) - pins the <c>[u8 type][u32 BE sourceId][string8
///     message]</c> body, the codec round-trip, and that an unverified render-type byte survives.
/// </summary>
public class PublicMessagePacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new PublicMessagePacket
        {
            Type = PublicMessagePacket.TypeShout,
            SourceId = 0x11223344,
            Message = "Hi!",
        };

        // [01] type=shout [11 22 33 44] sourceId BE [03] len [H i !]
        packet.ToBody().Should().Equal(
            (byte)0x01,
            (byte)0x11, (byte)0x22, (byte)0x33, (byte)0x44,
            (byte)0x03,
            (byte)'H', (byte)'i', (byte)'!');
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new PublicMessagePacket
        {
            Type = PublicMessagePacket.TypeChant,
            SourceId = 0xDEADBEEF,
            Message = "mor fas nadur",
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<PublicMessagePacket>().Subject;
        typed.Type.Should().Be(PublicMessagePacket.TypeChant);
        typed.SourceId.Should().Be(0xDEADBEEFu);
        typed.Message.Should().Be("mor fas nadur");
    }

    [Fact]
    public void RoundTrip_UnverifiedRenderType_PreservedVerbatim()
    {
        // The full set of render-type bytes is unverified (values >= 3),
        // so Type is a byte: any value must round-trip whether or not we have named it.
        var original = new PublicMessagePacket { Type = 0x07, SourceId = 1, Message = "future" };

        var parsed = PublicMessagePacket.Parse(original.ToBody());

        parsed.Type.Should().Be((byte)0x07);
        parsed.SourceId.Should().Be(1u);
        parsed.Message.Should().Be("future");
    }
}
