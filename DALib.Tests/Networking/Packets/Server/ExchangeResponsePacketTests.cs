using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x42 Exchange (S->C) - the server's player-to-player trade update stream. Pins each
///     action's wire layout, verifies action dispatch over the leading <c>[u8 action]</c> byte, and
///     round-trips through the codec.
/// </summary>
public class ExchangeResponsePacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    // ---- layout pins --------------------------------------------------------------------------

    [Fact]
    public void StartExchange_WriteBody_PinsLayout()
    {
        // [00][u32-BE otherUserId][string8 name]
        new StartExchangeResponsePacket { OtherUserId = 0x01020304, OtherUserName = "Bob" }
            .ToBody().Should().Equal(0x00, 0x01, 0x02, 0x03, 0x04, 0x03, 0x42, 0x6F, 0x62);
    }

    [Fact]
    public void RequestAmount_WriteBody_PinsLayout()
    {
        // [01][u8 sourceSlot]
        new RequestExchangeAmountPacket { SourceSlot = 0x05 }
            .ToBody().Should().Equal(0x01, 0x05);
    }

    [Fact]
    public void AddItem_WriteBody_PinsLayout()
    {
        // [02][bool rightSide][u8 exchangeIndex][u16-BE sprite][u8 color][string8 name]
        new AddExchangeItemResponsePacket
            { RightSide = true, ExchangeIndex = 0x02, Sprite = 0x8042, Color = 0x03, Name = "Gem" }
            .ToBody().Should().Equal(0x02, 0x01, 0x02, 0x80, 0x42, 0x03, 0x03, 0x47, 0x65, 0x6D);
    }

    [Fact]
    public void SetGold_WriteBody_PinsLayout()
    {
        // [03][bool rightSide][u32-BE gold]
        new SetExchangeGoldResponsePacket { RightSide = false, GoldAmount = 1000 }
            .ToBody().Should().Equal(0x03, 0x00, 0x00, 0x00, 0x03, 0xE8);
    }

    [Fact]
    public void Cancel_WriteBody_PinsLayout()
    {
        // [04][bool rightSide][string8 message]
        new CancelExchangeResponsePacket { RightSide = true, Message = "Bye" }
            .ToBody().Should().Equal(0x04, 0x01, 0x03, 0x42, 0x79, 0x65);
    }

    [Fact]
    public void Accept_WriteBody_PinsLayout()
    {
        // [05][bool rightSide][string8 message]
        new AcceptExchangeResponsePacket { RightSide = false, Message = "OK" }
            .ToBody().Should().Equal(0x05, 0x00, 0x02, 0x4F, 0x4B);
    }

    // ---- action dispatch ----------------------------------------------------------------------

    [Fact]
    public void Parse_Action0_IsStartExchange()
    {
        var parsed = ExchangeResponsePacket.Parse([0x00, 0x01, 0x02, 0x03, 0x04, 0x03, 0x42, 0x6F, 0x62])
            .Should().BeOfType<StartExchangeResponsePacket>().Subject;

        parsed.OtherUserId.Should().Be(0x01020304u);
        parsed.OtherUserName.Should().Be("Bob");
    }

    [Fact]
    public void Parse_Action2_IsAddItem()
    {
        var parsed = ExchangeResponsePacket.Parse([0x02, 0x01, 0x02, 0x80, 0x42, 0x03, 0x03, 0x47, 0x65, 0x6D])
            .Should().BeOfType<AddExchangeItemResponsePacket>().Subject;

        parsed.RightSide.Should().BeTrue();
        parsed.ExchangeIndex.Should().Be((byte)0x02);
        parsed.Sprite.Should().Be((ushort)0x8042);
        parsed.Color.Should().Be((byte)0x03);
        parsed.Name.Should().Be("Gem");
    }

    [Fact]
    public void Parse_Action3_IsSetGold()
    {
        var parsed = ExchangeResponsePacket.Parse([0x03, 0x00, 0x00, 0x00, 0x03, 0xE8])
            .Should().BeOfType<SetExchangeGoldResponsePacket>().Subject;

        parsed.RightSide.Should().BeFalse();
        parsed.GoldAmount.Should().Be(1000u);
    }

    [Fact]
    public void Parse_UnknownAction_Throws()
    {
        var act = () => ExchangeResponsePacket.Parse([0x09]);

        act.Should().Throw<InvalidDataException>();
    }

    // ---- round-trip through the codec ---------------------------------------------------------

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void RoundTrip_ThroughCodec_PreservesForm(ExchangeResponsePacket original)
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        parsed.Should().Be(original); // records: value equality across all fields + exact type
    }

    public static TheoryData<ExchangeResponsePacket> RoundTripCases() =>
    [
        new StartExchangeResponsePacket { OtherUserId = 42, OtherUserName = "Angelique" },
        new RequestExchangeAmountPacket { SourceSlot = 9 },
        new AddExchangeItemResponsePacket
            { RightSide = false, ExchangeIndex = 1, Sprite = 0x8123, Color = 7, Name = "Holy Water" },
        new SetExchangeGoldResponsePacket { RightSide = true, GoldAmount = 1_000_000 },
        new CancelExchangeResponsePacket { RightSide = false, Message = "Exchange was cancelled." },
        new AcceptExchangeResponsePacket { RightSide = true, Message = "You exchanged." },
    ];
}
