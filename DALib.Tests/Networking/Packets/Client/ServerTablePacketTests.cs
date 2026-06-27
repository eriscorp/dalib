using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x57 ServerTable (C->S) - verifies the abstract-base dispatch on the
///     mismatch flag and the wire shape of both variants. The hardcoded padding byte at the
///     tail of each variant is preserved exactly for byte-faithful round-tripping.
/// </summary>
public class ServerTablePacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void Parse_MismatchFlag01_RoutesToRequestVariant()
    {
        // [0x01 flag][0x00 padding]
        byte[] body = [0x01, 0x00];

        var parsed = ServerTablePacket.Parse(body);

        parsed.Should().BeOfType<ServerTableRequestPacket>();
    }

    [Fact]
    public void Parse_MismatchFlag00_RoutesToSelectVariant()
    {
        // [0x00 flag][0x07 serverId][0x00 padding]
        byte[] body = [0x00, 0x07, 0x00];

        var parsed = ServerTablePacket.Parse(body);

        var select = parsed.Should().BeOfType<ServerTableSelectPacket>().Subject;
        select.ServerId.Should().Be(0x07);
    }

    [Fact]
    public void Parse_EmptyBody_Throws()
    {
        var act = () => ServerTablePacket.Parse(ReadOnlySpan<byte>.Empty);

        act.Should().Throw<InvalidDataException>();
    }

    [Theory]
    [InlineData((byte)0x02)]
    [InlineData((byte)0x42)]
    [InlineData((byte)0xFF)]
    public void Parse_UnknownMismatchFlag_Throws(byte flag)
    {
        byte[] body = [flag, 0x00];

        var act = () => ServerTablePacket.Parse(body);

        act.Should().Throw<InvalidDataException>();
    }

    public class Request
    {
        [Fact]
        public void WriteBody_EmitsFlagAndPadding()
        {
            var packet = new ServerTableRequestPacket();

            packet.ToBody().Should().Equal((byte)0x01, (byte)0x00);
        }

        [Fact]
        public void WriteBody_HonorsPaddingOverride()
        {
            var packet = new ServerTableRequestPacket { Padding = 0xAB };

            packet.ToBody().Should().Equal((byte)0x01, (byte)0xAB);
        }

        [Fact]
        public void RoundTrip_ThroughCodec_PreservesPadding()
        {
            var codec = new PacketCodec();
            var crypto = MakeCrypto();
            var original = new ServerTableRequestPacket { Padding = 0x5A };

            var wire = codec.EncodeClient(original, crypto);
            var parsed = codec.ParseClientPacket(wire, crypto);

            var typed = parsed.Should().BeOfType<ServerTableRequestPacket>().Subject;
            typed.Padding.Should().Be(0x5A);
        }
    }

    public class Select
    {
        [Fact]
        public void WriteBody_EmitsFlagServerIdAndPadding()
        {
            var packet = new ServerTableSelectPacket { ServerId = 0x42 };

            packet.ToBody().Should().Equal((byte)0x00, (byte)0x42, (byte)0x00);
        }

        [Fact]
        public void WriteBody_HonorsPaddingOverride()
        {
            var packet = new ServerTableSelectPacket { ServerId = 0x42, Padding = 0xCD };

            packet.ToBody().Should().Equal((byte)0x00, (byte)0x42, (byte)0xCD);
        }

        [Fact]
        public void RoundTrip_ThroughCodec_PreservesAllFields()
        {
            var codec = new PacketCodec();
            var crypto = MakeCrypto();
            var original = new ServerTableSelectPacket { ServerId = 0x11, Padding = 0x22 };

            var wire = codec.EncodeClient(original, crypto);
            var parsed = codec.ParseClientPacket(wire, crypto);

            var typed = parsed.Should().BeOfType<ServerTableSelectPacket>().Subject;
            typed.ServerId.Should().Be(0x11);
            typed.Padding.Should().Be(0x22);
        }
    }
}
