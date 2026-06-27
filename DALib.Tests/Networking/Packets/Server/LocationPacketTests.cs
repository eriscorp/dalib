using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x04 Location (S->C) - pins the minimal <c>[u16 X][u16 Y]</c> body, the form with the
///     optional <c>[u16 11][u16 11]</c> trailing pair, and the codec round-trip of both.
/// </summary>
public class LocationPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_MinimalForm_PinsKnownLayout()
    {
        var packet = new LocationPacket { X = 0x0102, Y = 0x0304 };

        // [01 02] X BE [03 04] Y BE - no tail (minimal form)
        packet.ToBody().Should().Equal((byte)0x01, (byte)0x02, (byte)0x03, (byte)0x04);
    }

    [Fact]
    public void WriteBody_HybrasylTail_PinsKnownLayout()
    {
        var packet = new LocationPacket
        {
            X = 0x0102,
            Y = 0x0304,
            Unknown1 = 11,
            Unknown2 = 11,
        };

        // [01 02] X [03 04] Y [00 0B][00 0B] optional trailing pair
        packet.ToBody().Should().Equal(
            (byte)0x01, (byte)0x02,
            (byte)0x03, (byte)0x04,
            (byte)0x00, (byte)0x0B,
            (byte)0x00, (byte)0x0B);
    }

    [Fact]
    public void RoundTrip_MinimalForm_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new LocationPacket { X = 40, Y = 17 };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<LocationPacket>().Subject;
        typed.X.Should().Be((ushort)40);
        typed.Y.Should().Be((ushort)17);
        typed.Unknown1.Should().BeNull();
        typed.Unknown2.Should().BeNull();
    }

    [Fact]
    public void RoundTrip_HybrasylTail_PreservedVerbatim()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new LocationPacket
        {
            X = 3,
            Y = 7,
            Unknown1 = 11,
            Unknown2 = 11,
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<LocationPacket>().Subject;
        typed.X.Should().Be((ushort)3);
        typed.Y.Should().Be((ushort)7);
        typed.Unknown1.Should().Be((ushort)11);
        typed.Unknown2.Should().Be((ushort)11);
    }
}
