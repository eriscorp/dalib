using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x32 Door (S->C) - pins the <c>[u8 count]</c> + <c>[u8 x][u8 y][bool][bool]</c>
///     layout, the empty (count-0) post-walk case, the codec round-trip, and a multi-door batch.
/// </summary>
public class DoorPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_SingleDoor_PinsKnownLayout()
    {
        var packet = new DoorPacket
        {
            Doors = [new Door { X = 10, Y = 20, Closed = true, OpenRight = false }],
        };

        // [01] count [0A] x [14] y [01] closed [00] openRight
        packet.ToBody().Should().Equal(
            (byte)0x01,
            (byte)0x0A, (byte)0x14, (byte)0x01, (byte)0x00);
    }

    [Fact]
    public void WriteBody_EmptyBatch_IsBareCountZero()
    {
        // The empty post-walk form: count 0, no door records.
        var packet = new DoorPacket();

        packet.ToBody().Should().Equal((byte)0x00);
    }

    [Fact]
    public void RoundTrip_MultiDoor_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new DoorPacket
        {
            Doors =
            [
                new Door { X = 1, Y = 2, Closed = false, OpenRight = true },
                new Door { X = 3, Y = 4, Closed = true, OpenRight = false },
                new Door { X = 5, Y = 6, Closed = true, OpenRight = true },
            ],
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<DoorPacket>().Subject;
        typed.Doors.Should().HaveCount(3);
        typed.Doors[0].Should().Be(new Door { X = 1, Y = 2, Closed = false, OpenRight = true });
        typed.Doors[1].Should().Be(new Door { X = 3, Y = 4, Closed = true, OpenRight = false });
        typed.Doors[2].Should().Be(new Door { X = 5, Y = 6, Closed = true, OpenRight = true });
    }
}
