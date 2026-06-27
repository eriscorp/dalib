using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x55 Manufacture (C->S) - the crafting dialog request. Pins each subtype's wire
///     layout, verifies subtype dispatch over the shared <c>[u8 ManufactureType][u8 Slot][u8 subtype]</c>
///     prefix, and round-trips through the codec.
/// </summary>
public class ManufacturePacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    // ---- layout pins --------------------------------------------------------------------------

    [Fact]
    public void RequestPage_WriteBody_PinsLayout()
    {
        // [type][slot][00 subtype][u8 pageIndex]
        new RequestManufacturePagePacket { ManufactureType = 0x02, Slot = 0x03, PageIndex = 5 }
            .ToBody().Should().Equal(0x02, 0x03, 0x00, 0x05);
    }

    [Fact]
    public void Make_WriteBody_PinsLayout()
    {
        // [type][slot][01 subtype][string8 recipeName][u8 addSlotIndex]
        new MakeManufacturePacket
            { ManufactureType = 0x02, Slot = 0x03, RecipeName = "Sword", AddSlotIndex = 7 }
            .ToBody().Should().Equal(
                0x02, 0x03, 0x01, 0x05, 0x53, 0x77, 0x6F, 0x72, 0x64, 0x07); // "Sword"
    }

    // ---- subtype dispatch ---------------------------------------------------------------------

    [Fact]
    public void Parse_Subtype0_IsRequestPage()
    {
        var parsed = ManufacturePacket.Parse([0x02, 0x03, 0x00, 0x05])
            .Should().BeOfType<RequestManufacturePagePacket>().Subject;

        parsed.ManufactureType.Should().Be((byte)0x02);
        parsed.Slot.Should().Be((byte)0x03);
        parsed.PageIndex.Should().Be((byte)5);
    }

    [Fact]
    public void Parse_Subtype1_IsMake()
    {
        var parsed = ManufacturePacket.Parse([0x02, 0x03, 0x01, 0x05, 0x53, 0x77, 0x6F, 0x72, 0x64, 0x07])
            .Should().BeOfType<MakeManufacturePacket>().Subject;

        parsed.RecipeName.Should().Be("Sword");
        parsed.AddSlotIndex.Should().Be((byte)7);
    }

    [Fact]
    public void Parse_UnknownSubtype_Throws()
    {
        var act = () => ManufacturePacket.Parse([0x02, 0x03, 0x09]);

        act.Should().Throw<InvalidDataException>();
    }

    // ---- round-trip through the codec ---------------------------------------------------------

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void RoundTrip_ThroughCodec_PreservesForm(ManufacturePacket original)
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        parsed.Should().Be(original); // records: value equality across all fields + exact type
    }

    public static TheoryData<ManufacturePacket> RoundTripCases() =>
    [
        new RequestManufacturePagePacket { ManufactureType = 0x10, Slot = 0x02, PageIndex = 3 },
        new MakeManufacturePacket
            { ManufactureType = 0x10, Slot = 0x02, RecipeName = "Holy Water", AddSlotIndex = 0 },
    ];
}
