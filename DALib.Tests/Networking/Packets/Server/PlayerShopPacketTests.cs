using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x4F PlayerShop (S->C) - the player-run-shop window. Pins each subtype's wire layout,
///     the gate byte, subtype dispatch, the filled/empty listing discrimination, and codec round-trips.
/// </summary>
public class PlayerShopPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    // ---- layout pins --------------------------------------------------------------------------

    [Fact]
    public void FullState_WriteBody_PinsLayout()
    {
        // [01 gate][u32 shopId][00 subtype][u32 gold][u8 capacity][u8 count]{listing}
        // listing: [u32 id][u16 sprite][u8 color][string8 name][u8 hasSub=0][u8 pad][u32 qty][u32 price][u32 unknown]
        new PlayerShopFullStatePacket
            {
                ShopId = 0x12345678,
                ShopGold = 1000, // 0x3E8
                Capacity = 0x64,
                Listings = [new PlayerShopItemListing
                {
                    ListingId = 1,
                    Sprite = 0x0102,
                    Color = 0x05,
                    Name = "Apple",
                    Quantity = 10,
                    Price = 100,
                    Unknown = 0
                }]
            }
            .ToBody().Should().Equal(
                0x01,                   // gate
                0x12, 0x34, 0x56, 0x78, // shopId
                0x00,                   // subtype FullState
                0x00, 0x00, 0x03, 0xE8, // gold = 1000
                0x64,                   // capacity = 100
                0x01,                   // count = 1
                0x00, 0x00, 0x00, 0x01, // listingId = 1
                0x01, 0x02,             // sprite
                0x05,                   // color
                0x05, 0x41, 0x70, 0x70, 0x6C, 0x65, // "Apple"
                0x00,                   // hasSubContent = 0
                0x00,                   // padding
                0x00, 0x00, 0x00, 0x0A, // quantity = 10
                0x00, 0x00, 0x00, 0x64, // price = 100
                0x00, 0x00, 0x00, 0x00);// unknown
    }

    [Fact]
    public void AddItem_WithSubContent_WriteBody_PinsLayout()
    {
        // pins the present-sub-content branch ([u8 1][string8]) and the item high-bit-set sprite round-trip
        new PlayerShopAddItemPacket
            {
                ShopId = 1,
                Listing = new PlayerShopItemListing
                {
                    ListingId = 7,
                    Sprite = 0x8001,
                    Color = 0x02,
                    Name = "Hat",
                    SubContent = "x",
                    Quantity = 1,
                    Price = 5,
                    Unknown = 0
                }
            }
            .ToBody().Should().Equal(
                0x01,                   // gate
                0x00, 0x00, 0x00, 0x01, // shopId
                0x01,                   // subtype AddItem
                0x00, 0x00, 0x00, 0x07, // listingId = 7
                0x80, 0x01,             // sprite (raw on-wire)
                0x02,                   // color
                0x03, 0x48, 0x61, 0x74, // "Hat"
                0x01,                   // hasSubContent = 1
                0x01, 0x78,             // sub-content string8 "x"
                0x00,                   // padding
                0x00, 0x00, 0x00, 0x01, // quantity = 1
                0x00, 0x00, 0x00, 0x05, // price = 5
                0x00, 0x00, 0x00, 0x00);// unknown
    }

    [Fact]
    public void RemoveItem_WriteBody_PinsLayout()
    {
        new PlayerShopRemoveItemPacket { ShopId = 1, ListingId = 0xFF }
            .ToBody().Should().Equal(
                0x01,                   // gate
                0x00, 0x00, 0x00, 0x01, // shopId
                0x02,                   // subtype RemoveItem
                0x00, 0x00, 0x00, 0xFF);// listingId
    }

    [Fact]
    public void UpdateItem_Extended_WriteBody_PinsLayout()
    {
        // extended record = standard layout + an extra u32 (NewListingId) right after the id; the 0x03
        // trailing carries price + an inert attribute + a discarded u32 (no quantity).
        new PlayerShopUpdateItemPacket
            {
                ShopId = 1,
                ListingId = 0x0A,
                NewListingId = 0xBB,
                Sprite = 0x0102,
                Color = 0x03,
                Name = "Gem",
                Price = 9,
                Unknown = 0,
                Reserved = 0
            }
            .ToBody().Should().Equal(
                0x01,                   // gate
                0x00, 0x00, 0x00, 0x01, // shopId
                0x03,                   // subtype UpdateItem
                0x00, 0x00, 0x00, 0x0A, // listingId (match key)
                0x00, 0x00, 0x00, 0xBB, // newListingId (re-key, extended-only)
                0x01, 0x02,             // sprite
                0x03,                   // color
                0x03, 0x47, 0x65, 0x6D, // "Gem"
                0x00,                   // hasSubContent = 0
                0x00,                   // padding byte
                0x00, 0x00, 0x00, 0x09, // price = 9
                0x00, 0x00, 0x00, 0x00, // unknown
                0x00, 0x00, 0x00, 0x00);// reserved (read & dropped)
    }

    [Fact]
    public void Rename_WriteBody_PinsLayout()
    {
        new PlayerShopRenamePacket { ShopId = 1, ShopName = "Bob" }
            .ToBody().Should().Equal(
                0x01,                   // gate
                0x00, 0x00, 0x00, 0x01, // shopId
                0x04,                   // subtype Rename
                0x03, 0x42, 0x6F, 0x62);// "Bob"
    }

    [Fact]
    public void EmptySlot_WriteBody_PinsLayout()
    {
        // a sparse empty-slot listing inside a full-state body: [u32 0][u32 sparse]
        new PlayerShopFullStatePacket
            {
                ShopId = 1,
                ShopGold = 0,
                Capacity = 1,
                Listings = [new PlayerShopEmptySlot { SparseField = 0xABCD }]
            }
            .ToBody().Should().Equal(
                0x01,                   // gate
                0x00, 0x00, 0x00, 0x01, // shopId
                0x00,                   // subtype FullState
                0x00, 0x00, 0x00, 0x00, // gold
                0x01,                   // capacity
                0x01,                   // count
                0x00, 0x00, 0x00, 0x00, // empty-slot marker (id 0)
                0x00, 0x00, 0xAB, 0xCD);// sparse field
    }

    // ---- subtype + listing dispatch ----------------------------------------------------------

    [Fact]
    public void Parse_Subtype0_IsFullState_WithFilledListing()
    {
        var parsed = PlayerShopPacket.Parse(
                [0x01, 0x12, 0x34, 0x56, 0x78, 0x00,
                    0x00, 0x00, 0x03, 0xE8, 0x64, 0x01,
                    0x00, 0x00, 0x00, 0x01, 0x01, 0x02, 0x05,
                    0x05, 0x41, 0x70, 0x70, 0x6C, 0x65,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x0A, 0x00, 0x00, 0x00, 0x64, 0x00, 0x00, 0x00, 0x00])
            .Should().BeOfType<PlayerShopFullStatePacket>().Subject;

        parsed.ShopId.Should().Be(0x12345678u);
        parsed.ShopGold.Should().Be(1000u);
        parsed.Capacity.Should().Be((byte)0x64);
        parsed.Listings.Should().ContainSingle();

        var listing = parsed.Listings[0].Should().BeOfType<PlayerShopItemListing>().Subject;
        listing.ListingId.Should().Be(1u);
        listing.Sprite.Should().Be((ushort)0x0102);
        listing.Color.Should().Be((byte)0x05);
        listing.Name.Should().Be("Apple");
        listing.SubContent.Should().BeNull();
        listing.Quantity.Should().Be(10u);
        listing.Price.Should().Be(100u);
    }

    [Fact]
    public void Parse_FullState_ZeroIdListing_IsEmptySlot()
    {
        var parsed = PlayerShopPacket.Parse(
                [0x01, 0x00, 0x00, 0x00, 0x01, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x01, 0x01,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xAB, 0xCD])
            .Should().BeOfType<PlayerShopFullStatePacket>().Subject;

        var slot = parsed.Listings.Should().ContainSingle().Subject
            .Should().BeOfType<PlayerShopEmptySlot>().Subject;

        slot.SparseField.Should().Be(0xABCDu);
    }

    [Fact]
    public void Parse_Subtype3_IsUpdateItem_ReadsRekeyId()
    {
        var parsed = PlayerShopPacket.Parse(
                [0x01, 0x00, 0x00, 0x00, 0x01, 0x03,
                    0x00, 0x00, 0x00, 0x0A, 0x00, 0x00, 0x00, 0xBB,
                    0x01, 0x02, 0x03, 0x03, 0x47, 0x65, 0x6D,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00])
            .Should().BeOfType<PlayerShopUpdateItemPacket>().Subject;

        parsed.ListingId.Should().Be(0x0Au);
        parsed.NewListingId.Should().Be(0xBBu);
        parsed.Name.Should().Be("Gem");
        parsed.Price.Should().Be(9u);
    }

    [Fact]
    public void Parse_BadGateByte_Throws()
    {
        var act = () => PlayerShopPacket.Parse([0x00, 0x00, 0x00, 0x00, 0x01, 0x00]);

        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Parse_UnknownSubtype_Throws()
    {
        var act = () => PlayerShopPacket.Parse([0x01, 0x00, 0x00, 0x00, 0x01, 0x09]);

        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void FullState_CapacityBelowCount_WriteBody_Throws()
    {
        var act = () => new PlayerShopFullStatePacket
        {
            ShopId = 1,
            ShopGold = 0,
            Capacity = 0,
            Listings = [new PlayerShopItemListing
                { ListingId = 1, Sprite = 1, Color = 0, Name = "x", Quantity = 1, Price = 1 }]
        }.ToBody();

        act.Should().Throw<InvalidOperationException>();
    }

    // ---- round-trip through the codec ---------------------------------------------------------

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void RoundTrip_ThroughCodec_PreservesForm(PlayerShopPacket original)
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        // record equality compares IList by reference and is shallow over the polymorphic listing union,
        // so assert structural equivalence (element-wise) and the exact variant type separately.
        parsed.Should().BeOfType(original.GetType());
        parsed.Should().BeEquivalentTo(original, opts => opts.RespectingRuntimeTypes());
    }

    public static TheoryData<PlayerShopPacket> RoundTripCases() =>
    [
        new PlayerShopFullStatePacket
        {
            ShopId = 0xDEADBEEF,
            ShopGold = 250_000,
            Capacity = 100,
            Listings =
            [
                new PlayerShopItemListing
                    { ListingId = 1, Sprite = 0x1234, Color = 3, Name = "Beag Cradh", Quantity = 5, Price = 1000, Unknown = 0 },
                new PlayerShopItemListing
                    { ListingId = 2, Sprite = 0x8055, Color = 1, Name = "Stick", SubContent = "rare", Quantity = 1, Price = 99, Unknown = 7 },
                new PlayerShopEmptySlot { SparseField = 0 }
            ]
        },
        new PlayerShopAddItemPacket
        {
            ShopId = 42,
            Listing = new PlayerShopItemListing
                { ListingId = 9, Sprite = 0x0301, Color = 2, Name = "Ribbon", Quantity = 3, Price = 50 }
        },
        new PlayerShopRemoveItemPacket { ShopId = 42, ListingId = 9 },
        new PlayerShopUpdateItemPacket
        {
            ShopId = 42,
            ListingId = 9,
            NewListingId = 0x1000,
            Sprite = 0x0301,
            Color = 2,
            Name = "Ribbon",
            SubContent = "desc",
            Price = 75,
            Unknown = 4,
            Reserved = 2
        },
        new PlayerShopRenamePacket { ShopId = 42, ShopName = "Aether's Emporium" },
    ];
}
