using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x4A Exchange (C->S) - the player-to-player trade cluster. Pins each stage's
///     wire layout, verifies stage dispatch and the shared <c>[u8 stage][u32 OtherUserId]</c>
///     prefix, and round-trips through the codec.
/// </summary>
public class ExchangePacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    // ---- layout pins --------------------------------------------------------------------------

    [Fact]
    public void Start_WriteBody_IsPrefixOnly()
    {
        // [00][u32 OtherUserId]
        new StartExchangePacket { OtherUserId = 0x0A0B0C0D }
            .ToBody().Should().Equal(0x00, 0x0A, 0x0B, 0x0C, 0x0D);
    }

    [Fact]
    public void AddItem_WriteBody_PinsLayout()
    {
        // [01][u32 OtherUserId][u8 slot]
        new AddExchangeItemPacket { OtherUserId = 1, SourceSlot = 5 }
            .ToBody().Should().Equal(0x01, 0x00, 0x00, 0x00, 0x01, 0x05);
    }

    [Fact]
    public void AddStackable_WriteBody_PinsLayout()
    {
        // [02][u32 OtherUserId][u8 slot][u8 count]
        new AddExchangeStackableItemPacket { OtherUserId = 1, SourceSlot = 5, ItemCount = 20 }
            .ToBody().Should().Equal(0x02, 0x00, 0x00, 0x00, 0x01, 0x05, 0x14);
    }

    [Fact]
    public void SetGold_WriteBody_PinsLayout()
    {
        // [03][u32 OtherUserId][u32 gold] - gold 1000 = 0x000003E8
        new SetExchangeGoldPacket { OtherUserId = 1, GoldAmount = 1000 }
            .ToBody().Should().Equal(0x03, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x03, 0xE8);
    }

    [Fact]
    public void Cancel_WriteBody_IsPrefixOnly()
    {
        new CancelExchangePacket { OtherUserId = 1 }
            .ToBody().Should().Equal(0x04, 0x00, 0x00, 0x00, 0x01);
    }

    [Fact]
    public void Accept_WriteBody_IsPrefixOnly()
    {
        new AcceptExchangePacket { OtherUserId = 1 }
            .ToBody().Should().Equal(0x05, 0x00, 0x00, 0x00, 0x01);
    }

    // ---- stage dispatch -----------------------------------------------------------------------

    [Fact]
    public void Parse_Stage0_IsStart()
    {
        var parsed = ExchangePacket.Parse([0x00, 0x0A, 0x0B, 0x0C, 0x0D])
            .Should().BeOfType<StartExchangePacket>().Subject;

        parsed.OtherUserId.Should().Be(0x0A0B0C0Du);
    }

    [Fact]
    public void Parse_Stage1_IsAddItem()
    {
        var parsed = ExchangePacket.Parse([0x01, 0x00, 0x00, 0x00, 0x01, 0x05])
            .Should().BeOfType<AddExchangeItemPacket>().Subject;

        parsed.OtherUserId.Should().Be(1u);
        parsed.SourceSlot.Should().Be((byte)5);
    }

    [Fact]
    public void Parse_Stage2_IsAddStackable()
    {
        var parsed = ExchangePacket.Parse([0x02, 0x00, 0x00, 0x00, 0x01, 0x05, 0x14])
            .Should().BeOfType<AddExchangeStackableItemPacket>().Subject;

        parsed.SourceSlot.Should().Be((byte)5);
        parsed.ItemCount.Should().Be((byte)20);
    }

    [Fact]
    public void Parse_Stage3_IsSetGold()
    {
        var parsed = ExchangePacket.Parse([0x03, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x03, 0xE8])
            .Should().BeOfType<SetExchangeGoldPacket>().Subject;

        parsed.GoldAmount.Should().Be(1000u);
    }

    [Fact]
    public void Parse_UnknownStage_Throws()
    {
        // stage 6 + a u32 (the prefix is always read before the stage switch)
        var act = () => ExchangePacket.Parse([0x06, 0x00, 0x00, 0x00, 0x01]);

        act.Should().Throw<InvalidDataException>();
    }

    // ---- round-trip through the codec ---------------------------------------------------------

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void RoundTrip_ThroughCodec_PreservesForm(ExchangePacket original)
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        parsed.Should().Be(original); // records: value equality across all fields + exact type
    }

    public static TheoryData<ExchangePacket> RoundTripCases() =>
    [
        new StartExchangePacket { OtherUserId = 0x0A0B0C0D },
        new AddExchangeItemPacket { OtherUserId = 42, SourceSlot = 3 },
        new AddExchangeStackableItemPacket { OtherUserId = 42, SourceSlot = 3, ItemCount = 99 },
        new SetExchangeGoldPacket { OtherUserId = 42, GoldAmount = 1_000_000 },
        new CancelExchangePacket { OtherUserId = 42 },
        new AcceptExchangePacket { OtherUserId = 42 },
    ];
}
