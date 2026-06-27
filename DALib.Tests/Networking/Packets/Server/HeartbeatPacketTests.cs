using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for the S->C heartbeat challenges - 0x3B ByteHeartbeat and 0x68 TickHeartbeat. Pins each
///     body and round-trips through the codec.
/// </summary>
public class HeartbeatPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void ByteHeartbeat_WriteBody_PinsLayout()
        => new ByteHeartbeatPacket { First = 0x2A, Second = 0xC7 }
            .ToBody().Should().Equal(0x2A, 0xC7);

    [Fact]
    public void TickHeartbeat_WriteBody_PinsLayout()
        => new TickHeartbeatPacket { ServerTick = 0x0102_0304 }
            .ToBody().Should().Equal(0x01, 0x02, 0x03, 0x04); // u32 BE

    [Fact]
    public void Parse_ByteHeartbeat()
    {
        var parsed = ByteHeartbeatPacket.Parse([0x2A, 0xC7]);

        parsed.First.Should().Be((byte)0x2A);
        parsed.Second.Should().Be((byte)0xC7);
    }

    [Fact]
    public void Parse_TickHeartbeat()
        => TickHeartbeatPacket.Parse([0x01, 0x02, 0x03, 0x04]).ServerTick.Should().Be(0x0102_0304u);

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void RoundTrip_ThroughCodec_PreservesForm(ServerPacket original)
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        parsed.Should().Be(original);
    }

    public static TheoryData<ServerPacket> RoundTripCases() =>
    [
        new ByteHeartbeatPacket { First = 0, Second = 253 },
        new ByteHeartbeatPacket { First = 200, Second = 1 },
        new TickHeartbeatPacket { ServerTick = 0 },
        new TickHeartbeatPacket { ServerTick = int.MaxValue },
    ];
}
