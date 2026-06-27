using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x1B EditablePaper (S->C) - the writable signpost. Pins the wire layout
///     <c>[Slot][Type][Width][Height][string16 Text]</c>, the Width-before-Height field order,
///     the PaperType byte, and the codec round-trip.
/// </summary>
public class EditablePaperPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_EmitsFieldsInClientOrder()
    {
        // Slot=5, Type=White(4), Width=3, Height=12, Text="Hi"
        // [05][04][03][0C][00 02 'H' 'i']
        new EditablePaperPacket
        {
            Slot = 5,
            Type = PaperType.White,
            Width = 3,
            Height = 12,
            Text = "Hi"
        }.ToBody().Should().Equal(0x05, 0x04, 0x03, 0x0C, 0x00, 0x02, 0x48, 0x69);
    }

    [Fact]
    public void WriteBody_WritesWidthBeforeHeight()
    {
        // Width (horizontal) precedes Height (vertical) on the wire.
        // Body offsets: [0]=Slot [1]=Type [2]=Width [3]=Height. Non-square so the order is observable.
        var body = new EditablePaperPacket
        {
            Slot = 0,
            Type = PaperType.Brown,
            Width = 3,
            Height = 12,
            Text = ""
        }.ToBody();

        body[2].Should().Be(3);  // Width
        body[3].Should().Be(12); // Height
    }

    [Fact]
    public void Parse_ReadsAllFields()
    {
        var parsed = EditablePaperPacket.Parse([0x07, 0x02, 0x05, 0x0A, 0x00, 0x02, 0x48, 0x69]);

        parsed.Slot.Should().Be((byte)7);
        parsed.Type.Should().Be(PaperType.GlitchedBlue2);
        parsed.Width.Should().Be((byte)5);
        parsed.Height.Should().Be((byte)10);
        parsed.Text.Should().Be("Hi");
    }

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void RoundTrip_ThroughCodec_PreservesValue(EditablePaperPacket original)
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        parsed.Should().Be(original);
    }

    public static TheoryData<EditablePaperPacket> RoundTripCases() =>
    [
        new EditablePaperPacket { Slot = 0, Type = PaperType.Brown, Width = 3, Height = 12, Text = "" },
        new EditablePaperPacket { Slot = 12, Type = PaperType.White, Width = 8, Height = 4, Text = "Sign here." },
        new EditablePaperPacket { Slot = 255, Type = PaperType.Orange, Width = 1, Height = 1, Text = "Line1\tLine2" },
    ];
}
