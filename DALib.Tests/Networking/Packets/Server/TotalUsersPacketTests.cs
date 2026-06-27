using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x47 TotalUsers (S->C) - a single big-endian u16 count. Modeled for protocol
///     completeness; not emitted by typical servers.
/// </summary>
public class TotalUsersPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsBigEndianU16()
    {
        // 0x0102 -> big-endian [01][02]
        new TotalUsersPacket { Count = 0x0102 }.ToBody().Should().Equal((byte)0x01, (byte)0x02);
    }

    [Fact]
    public void Parse_ReadsBigEndianU16()
    {
        TotalUsersPacket.Parse([0x01, 0x02]).Count.Should().Be((ushort)0x0102);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(0xABCD)]
    public void RoundTrip_ThroughCodec_PreservesCount(ushort count)
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new TotalUsersPacket { Count = count };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        parsed.Should().BeOfType<TotalUsersPacket>().Subject.Count.Should().Be(count);
    }
}
