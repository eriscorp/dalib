using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x50 Manufacture (S->C), the crafting-window display. Pins each subtype's wire
///     layout, verifies subtype dispatch over the shared <c>[u8 ManufactureType][u8 Slot][u8 subtype]</c>
///     prefix, and round-trips through the codec.
/// </summary>
public class ManufactureResponsePacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    // ---- layout pins --------------------------------------------------------------------------

    [Fact]
    public void Open_WriteBody_PinsLayout()
    {
        // [type][slot][00 subtype][u8 recipeCount]
        new OpenManufacturePacket { ManufactureType = 0x02, Slot = 0x03, RecipeCount = 10 }
            .ToBody().Should().Equal(0x02, 0x03, 0x00, 0x0A);
    }

    [Fact]
    public void Page_WriteBody_PinsLayout()
    {
        // [type][slot][01 subtype][u8 pageIndex][u16-BE sprite][string8 name][string16-BE desc][string16-BE ingredients][u8 hasAddItem]
        new ManufacturePagePacket
            {
                ManufactureType = 0x02,
                Slot = 0x03,
                PageIndex = 5,
                Sprite = 0x8042,
                RecipeName = "Sword",
                Description = "Sharp",
                Ingredients = "Iron",
                HasAddItem = true
            }
            .ToBody().Should().Equal(
                0x02, 0x03, 0x01, 0x05,             // prefix + pageIndex
                0x80, 0x42,                         // sprite (BE, item high bit set)
                0x05, 0x53, 0x77, 0x6F, 0x72, 0x64, // "Sword"
                0x00, 0x05, 0x53, 0x68, 0x61, 0x72, 0x70, // string16 "Sharp"
                0x00, 0x04, 0x49, 0x72, 0x6F, 0x6E, // string16 "Iron"
                0x01);                              // hasAddItem
    }

    // ---- subtype dispatch ---------------------------------------------------------------------

    [Fact]
    public void Parse_Subtype0_IsOpen()
    {
        var parsed = ManufactureResponsePacket.Parse([0x02, 0x03, 0x00, 0x0A])
            .Should().BeOfType<OpenManufacturePacket>().Subject;

        parsed.ManufactureType.Should().Be((byte)0x02);
        parsed.Slot.Should().Be((byte)0x03);
        parsed.RecipeCount.Should().Be((byte)10);
    }

    [Fact]
    public void Parse_Subtype1_IsPage()
    {
        var parsed = ManufactureResponsePacket.Parse(
                [0x02, 0x03, 0x01, 0x05, 0x80, 0x42,
                    0x05, 0x53, 0x77, 0x6F, 0x72, 0x64,
                    0x00, 0x05, 0x53, 0x68, 0x61, 0x72, 0x70,
                    0x00, 0x04, 0x49, 0x72, 0x6F, 0x6E, 0x01])
            .Should().BeOfType<ManufacturePagePacket>().Subject;

        parsed.PageIndex.Should().Be((byte)5);
        parsed.Sprite.Should().Be((ushort)0x8042);
        parsed.RecipeName.Should().Be("Sword");
        parsed.Description.Should().Be("Sharp");
        parsed.Ingredients.Should().Be("Iron");
        parsed.HasAddItem.Should().BeTrue();
    }

    [Fact]
    public void Parse_UnknownSubtype_Throws()
    {
        var act = () => ManufactureResponsePacket.Parse([0x02, 0x03, 0x09]);

        act.Should().Throw<InvalidDataException>();
    }

    // ---- round-trip through the codec ---------------------------------------------------------

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void RoundTrip_ThroughCodec_PreservesForm(ManufactureResponsePacket original)
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        parsed.Should().Be(original); // records: value equality across all fields + exact type
    }

    public static TheoryData<ManufactureResponsePacket> RoundTripCases() =>
    [
        new OpenManufacturePacket { ManufactureType = 0x10, Slot = 0x02, RecipeCount = 3 },
        new ManufacturePagePacket
        {
            ManufactureType = 0x10,
            Slot = 0x02,
            PageIndex = 1,
            Sprite = 0x8123,
            RecipeName = "Holy Water",
            Description = "Blessed liquid.",
            Ingredients = "Water, Faith",
            HasAddItem = false
        },
    ];
}
