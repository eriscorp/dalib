using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x51 BlockInput (S->C) - the input lock/release toggle. Modeled for protocol
///     completeness; not emitted by typical servers. Pins both wire forms - the bare 1-byte block form
///     and the 2-byte release form - and the codec round-trip.
/// </summary>
public class BlockInputPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void Block_WriteBody_IsBareStateByte()
    {
        // State == 1 (block): bare [01], no trailing byte
        new BlockInputPacket { State = 1 }.ToBody().Should().Equal((byte)0x01);
    }

    [Fact]
    public void Release_WriteBody_CarriesTrailingByte()
    {
        // State == 0 (release): [00][trailing]
        new BlockInputPacket { State = 0, ReleaseArgument = 0x07 }
            .ToBody().Should().Equal((byte)0x00, (byte)0x07);
    }

    [Fact]
    public void Release_WithoutArgument_Throws()
    {
        var act = () => new BlockInputPacket { State = 0 }.ToBody();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Parse_BlockForm_HasNoTrailing()
    {
        var parsed = BlockInputPacket.Parse([0x01]);

        parsed.State.Should().Be((byte)1);
        parsed.ReleaseArgument.Should().BeNull();
    }

    [Fact]
    public void Parse_ReleaseForm_ReadsTrailing()
    {
        var parsed = BlockInputPacket.Parse([0x00, 0x07]);

        parsed.State.Should().Be((byte)0);
        parsed.ReleaseArgument.Should().Be((byte)0x07);
    }

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void RoundTrip_ThroughCodec_PreservesForm(BlockInputPacket original)
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        parsed.Should().Be(original); // records: value equality (no collections here)
    }

    public static TheoryData<BlockInputPacket> RoundTripCases() =>
    [
        new BlockInputPacket { State = 1 },
        new BlockInputPacket { State = 0, ReleaseArgument = 0 },
        new BlockInputPacket { State = 0, ReleaseArgument = 0xAB },
    ];
}
