using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x3C MapData (S->C). Pins the <c>[u16 rowIndex][raw row bytes]</c> body (the
///     row data is read to end of packet, no length prefix) and the codec round-trip.
/// </summary>
public class MapDataPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new MapDataPacket
        {
            RowIndex = 0x0007,
            RowData = [0xAA, 0xBB, 0xCC, 0xDD],
        };

        // [00 07] rowIndex BE [AA BB CC DD] raw row bytes
        packet.ToBody().Should().Equal(
            (byte)0x00, (byte)0x07,
            (byte)0xAA, (byte)0xBB, (byte)0xCC, (byte)0xDD);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new MapDataPacket
        {
            RowIndex = 42,
            RowData = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06],
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<MapDataPacket>().Subject;
        typed.RowIndex.Should().Be((ushort)42);
        typed.RowData.Should().Equal(0x01, 0x02, 0x03, 0x04, 0x05, 0x06);
    }
}
