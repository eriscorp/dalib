using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x7B RequestMetafile (C->S) - pins both variants (all-checksums vs by-name), the
///     discriminator byte, the round-trip, and the by-name guard.
/// </summary>
public class RequestMetafilePacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_AllCheckSums_IsSingleTrueByte()
    {
        var packet = RequestMetafilePacket.AllCheckSums();

        // [01] all=true, no name follows
        packet.ToBody().Should().Equal((byte)0x01);
    }

    [Fact]
    public void WriteBody_ForName_IsZeroByteThenString8()
    {
        var packet = RequestMetafilePacket.ForName("abc");

        // [00] all=false [03] string8 length ['a' 'b' 'c']
        packet.ToBody().Should().Equal(
            (byte)0x00,
            (byte)0x03,
            (byte)'a', (byte)'b', (byte)'c');
    }

    [Fact]
    public void WriteBody_ForName_WithNullName_Throws()
    {
        var packet = new RequestMetafilePacket { All = false, Name = null };

        var act = () => packet.ToBody();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RoundTrip_AllCheckSums_PreservesState()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = RequestMetafilePacket.AllCheckSums();

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<RequestMetafilePacket>().Subject;
        typed.All.Should().BeTrue();
        typed.Name.Should().BeNull();
    }

    [Fact]
    public void RoundTrip_ForName_PreservesName()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = RequestMetafilePacket.ForName("nation");

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<RequestMetafilePacket>().Subject;
        typed.All.Should().BeFalse();
        typed.Name.Should().Be("nation");
    }
}
