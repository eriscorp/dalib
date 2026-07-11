using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x54 PlayerShopAction (C->S) - the actions driven in an open player-run shop, the pair
///     for S->C 0x4F PlayerShop. Pins each action's wire layout (the shared <c>[u8 0x01 gate][u32 ShopId]
///     [u8 action]</c> prefix + tail), verifies action dispatch and the gate-byte guard, and round-trips
///     through the codec.
/// </summary>
public class PlayerShopActionPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    // ---- layout pins --------------------------------------------------------------------------

    [Fact]
    public void WithdrawGold_WriteBody_PinsLayout()
    {
        // [01 gate][u32 ShopId][00 action][u8 Selector][u32 Amount] - ShopId 0x0A0B0C0D, Amount 1000 = 0x3E8
        new WithdrawShopGoldPacket { ShopId = 0x0A0B0C0D, Selector = 0x07, Amount = 1000 }
            .ToBody().Should().Equal(
                0x01, 0x0A, 0x0B, 0x0C, 0x0D, 0x00, 0x07, 0x00, 0x00, 0x03, 0xE8);
    }

    [Fact]
    public void AddItem_WriteBody_PinsLayout_WithZeroReservedTail()
    {
        // [01 gate][u32 ShopId][01 action][u32 Operand1][u32 Operand2][u16 0][u16 0]
        new AddShopItemPacket { ShopId = 1, Operand1 = 2, Operand2 = 3 }
            .ToBody().Should().Equal(
                0x01, 0x00, 0x00, 0x00, 0x01, 0x01,
                0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00);
    }

    [Fact]
    public void UpdateListing_WriteBody_PinsLayout()
    {
        // [01 gate][u32 ShopId][02 action][u32 ListingId][u32 Price][u32 Count]
        new UpdateShopListingPacket { ShopId = 1, ListingId = 42, Price = 1000, Count = 5 }
            .ToBody().Should().Equal(
                0x01, 0x00, 0x00, 0x00, 0x01, 0x02,
                0x00, 0x00, 0x00, 0x2A, 0x00, 0x00, 0x03, 0xE8, 0x00, 0x00, 0x00, 0x05);
    }

    [Fact]
    public void RemoveListing_WriteBody_PinsLayout()
    {
        // [01 gate][u32 ShopId][03 action][u32 ListingId]
        new RemoveShopListingPacket { ShopId = 1, ListingId = 42 }
            .ToBody().Should().Equal(0x01, 0x00, 0x00, 0x00, 0x01, 0x03, 0x00, 0x00, 0x00, 0x2A);
    }

    [Fact]
    public void CloseShop_WriteBody_IsPrefixOnly()
    {
        new CloseShopPacket { ShopId = 1 }
            .ToBody().Should().Equal(0x01, 0x00, 0x00, 0x00, 0x01, 0x04);
    }

    [Fact]
    public void ShopOpened_WriteBody_IsPrefixOnly()
    {
        new ShopOpenedPacket { ShopId = 1 }
            .ToBody().Should().Equal(0x01, 0x00, 0x00, 0x00, 0x01, 0x05);
    }

    // ---- action dispatch ----------------------------------------------------------------------

    [Fact]
    public void Parse_Action0_IsWithdrawGold()
    {
        var parsed = PlayerShopActionPacket
            .Parse([0x01, 0x0A, 0x0B, 0x0C, 0x0D, 0x00, 0x07, 0x00, 0x00, 0x03, 0xE8])
            .Should().BeOfType<WithdrawShopGoldPacket>().Subject;

        parsed.ShopId.Should().Be(0x0A0B0C0Du);
        parsed.Selector.Should().Be((byte)0x07);
        parsed.Amount.Should().Be(1000u);
    }

    [Fact]
    public void Parse_Action1_IsAddItem()
    {
        var parsed = PlayerShopActionPacket
            .Parse([0x01, 0x00, 0x00, 0x00, 0x01, 0x01,
                0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00])
            .Should().BeOfType<AddShopItemPacket>().Subject;

        parsed.Operand1.Should().Be(2u);
        parsed.Operand2.Should().Be(3u);
        parsed.Reserved1.Should().Be((ushort)0);
        parsed.Reserved2.Should().Be((ushort)0);
    }

    [Fact]
    public void Parse_Action2_IsUpdateListing()
    {
        var parsed = PlayerShopActionPacket
            .Parse([0x01, 0x00, 0x00, 0x00, 0x01, 0x02,
                0x00, 0x00, 0x00, 0x2A, 0x00, 0x00, 0x03, 0xE8, 0x00, 0x00, 0x00, 0x05])
            .Should().BeOfType<UpdateShopListingPacket>().Subject;

        parsed.ListingId.Should().Be(42u);
        parsed.Price.Should().Be(1000u);
        parsed.Count.Should().Be(5u);
    }

    [Fact]
    public void Parse_Action3_IsRemoveListing()
    {
        var parsed = PlayerShopActionPacket
            .Parse([0x01, 0x00, 0x00, 0x00, 0x01, 0x03, 0x00, 0x00, 0x00, 0x2A])
            .Should().BeOfType<RemoveShopListingPacket>().Subject;

        parsed.ListingId.Should().Be(42u);
    }

    [Fact]
    public void Parse_BadGateByte_Throws()
    {
        // Second body byte is not the hard-coded 0x01 gate.
        var act = () => PlayerShopActionPacket.Parse([0x00, 0x00, 0x00, 0x00, 0x01, 0x04]);

        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Parse_UnknownAction_Throws()
    {
        // action 6 - past the known set
        var act = () => PlayerShopActionPacket.Parse([0x01, 0x00, 0x00, 0x00, 0x01, 0x06]);

        act.Should().Throw<InvalidDataException>();
    }

    // ---- round-trip through the codec ---------------------------------------------------------

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void RoundTrip_ThroughCodec_PreservesForm(PlayerShopActionPacket original)
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        parsed.Should().Be(original); // records: value equality across all fields + exact type
    }

    public static TheoryData<PlayerShopActionPacket> RoundTripCases() =>
    [
        new WithdrawShopGoldPacket { ShopId = 0x0A0B0C0D, Selector = 0x07, Amount = 50_000 },
        new AddShopItemPacket { ShopId = 42, Operand1 = 7, Operand2 = 900, Reserved1 = 0, Reserved2 = 0 },
        new UpdateShopListingPacket { ShopId = 42, ListingId = 3, Price = 1_000_000, Count = 250 },
        new RemoveShopListingPacket { ShopId = 42, ListingId = 3 },
        new CloseShopPacket { ShopId = 42 },
        new ShopOpenedPacket { ShopId = 42 },
    ];
}
