using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x35 ReadonlyPaper (S->C) - the non-writable signpost slate. Pins the wire layout
///     <c>[Type][Width][Height][Centered][string16 Text]</c>, the Width-before-Height order, the
///     PaperType byte, and the codec round-trip.
/// </summary>
public class ReadonlyPaperPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_EmitsFieldsInClientOrder()
    {
        // Type=GlitchedBlue2(2), Width=3, Height=12, Centered=true, Text="Hi"
        // [02][03][0C][01][00 02 'H' 'i']
        new ReadonlyPaperPacket
        {
            Type = PaperType.GlitchedBlue2,
            Width = 3,
            Height = 12,
            Centered = true,
            Text = "Hi"
        }.ToBody().Should().Equal(0x02, 0x03, 0x0C, 0x01, 0x00, 0x02, 0x48, 0x69);
    }

    [Fact]
    public void WriteBody_WritesWidthBeforeHeight()
    {
        // The horizontal extent (Width) precedes the vertical extent (Height).
        // Non-square so the order is observable.
        var body = new ReadonlyPaperPacket
        {
            Type = PaperType.Brown,
            Width = 3,
            Height = 12,
            Centered = false,
            Text = ""
        }.ToBody();

        body[1].Should().Be(3);  // Width
        body[2].Should().Be(12); // Height
    }

    [Fact]
    public void WriteBody_CenteredFalse_WritesZeroByte()
    {
        new ReadonlyPaperPacket
        {
            Type = PaperType.White,
            Width = 1,
            Height = 1,
            Centered = false,
            Text = ""
        }.ToBody().Should().Equal(0x04, 0x01, 0x01, 0x00, 0x00, 0x00);
    }

    [Fact]
    public void Parse_ReadsAllFields()
    {
        var parsed = ReadonlyPaperPacket.Parse([0x03, 0x05, 0x0A, 0x01, 0x00, 0x02, 0x48, 0x69]);

        parsed.Type.Should().Be(PaperType.Orange);
        parsed.Width.Should().Be((byte)5);
        parsed.Height.Should().Be((byte)10);
        parsed.Centered.Should().BeTrue();
        parsed.Text.Should().Be("Hi");
    }

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void RoundTrip_ThroughCodec_PreservesValue(ReadonlyPaperPacket original)
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        parsed.Should().Be(original);
    }

    public static TheoryData<ReadonlyPaperPacket> RoundTripCases() =>
    [
        new ReadonlyPaperPacket { Type = PaperType.Brown, Width = 3, Height = 12, Centered = false, Text = "" },
        new ReadonlyPaperPacket { Type = PaperType.White, Width = 8, Height = 4, Centered = true, Text = "A read-only sign." },
        new ReadonlyPaperPacket { Type = PaperType.GlitchedBlue1, Width = 1, Height = 1, Centered = true, Text = "Line1\tLine2" },
    ];
}
