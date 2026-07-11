using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for the eight C->S opcodes that close the 7.41 client-to-server surface: 0x31 Confirm,
///     0x42 Exception, 0x46 GroupView, 0x6A MiniGame, 0x6C CashShop, 0x71 SendAlive, 0x73 BrowserAction,
///     0x7A RequestLoverName. Each is client-emittable (binary-verified) but unhandled by Hybrasyl/Chaos.
///     Pins each wire layout and round-trips every form through the codec (which also proves opcode
///     auto-discovery).
/// </summary>
public class Gap741ClientPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    // ---- no-payload triggers -------------------------------------------------------------------

    [Fact]
    public void GroupView_IsOpcodeOnly() => new GroupViewPacket().ToBody().Should().BeEmpty();

    [Fact]
    public void SendAlive_IsOpcodeOnly() => new SendAlivePacket().ToBody().Should().BeEmpty();

    [Fact]
    public void RequestLoverName_IsOpcodeOnly() => new RequestLoverNamePacket().ToBody().Should().BeEmpty();

    // ---- 0x31 Confirm --------------------------------------------------------------------------

    [Fact]
    public void Confirm_WriteBody_PinsLayout()
    {
        // [u8 Arg0][u8 Arg1][u8 Arg2][u8 len][payload]
        new ConfirmPacket { Arg0 = 1, Arg1 = 2, Arg2 = 3, Payload = [0xAA, 0xBB] }
            .ToBody().Should().Equal(0x01, 0x02, 0x03, 0x02, 0xAA, 0xBB);
    }

    [Fact]
    public void Confirm_RoundTrips()
    {
        var parsed = ConfirmPacket.Parse([0x01, 0x02, 0x03, 0x02, 0xAA, 0xBB]);
        parsed.Arg0.Should().Be((byte)1);
        parsed.Arg1.Should().Be((byte)2);
        parsed.Arg2.Should().Be((byte)3);
        parsed.Payload.Should().Equal(0xAA, 0xBB);
    }

    // ---- 0x42 Exception ------------------------------------------------------------------------

    [Fact]
    public void Exception_WriteBody_PinsLayout()
    {
        // [01 gate][u16 len][data][00 terminator]
        new ExceptionPacket { Data = [0xDE, 0xAD, 0xBE, 0xEF] }
            .ToBody().Should().Equal(0x01, 0x00, 0x04, 0xDE, 0xAD, 0xBE, 0xEF, 0x00);
    }

    [Fact]
    public void Exception_Parse_ReadsData()
    {
        var parsed = ExceptionPacket.Parse([0x01, 0x00, 0x04, 0xDE, 0xAD, 0xBE, 0xEF, 0x00]);
        parsed.Data.Should().Equal(0xDE, 0xAD, 0xBE, 0xEF);
    }

    [Fact]
    public void Exception_Parse_BadGate_Throws()
    {
        var act = () => ExceptionPacket.Parse([0x00, 0x00, 0x00, 0x00]);
        act.Should().Throw<InvalidDataException>();
    }

    // ---- 0x6C CashShop -------------------------------------------------------------------------

    [Fact]
    public void CashShop_Purchase_PinsLayout()
    {
        // [01][u8 slot][u32 itemId] - itemId 0x0A0B0C0D
        new PurchaseCashShopItemPacket { Slot = 5, ItemId = 0x0A0B0C0D }
            .ToBody().Should().Equal(0x01, 0x05, 0x0A, 0x0B, 0x0C, 0x0D);
    }

    [Fact]
    public void CashShop_Open_And_Close_ArePrefixOnly()
    {
        new OpenCashShopPacket().ToBody().Should().Equal(0x00);
        new CloseCashShopPacket().ToBody().Should().Equal(0x02);
    }

    [Fact]
    public void CashShop_Parse_DispatchesPurchase()
    {
        var parsed = CashShopPacket.Parse([0x01, 0x05, 0x0A, 0x0B, 0x0C, 0x0D])
            .Should().BeOfType<PurchaseCashShopItemPacket>().Subject;
        parsed.Slot.Should().Be((byte)5);
        parsed.ItemId.Should().Be(0x0A0B0C0Du);
    }

    // ---- 0x6A MiniGame -------------------------------------------------------------------------

    [Fact]
    public void MiniGame_PinsLayouts()
    {
        new OpenMiniGamePacket().ToBody().Should().Equal(0x05);
        new SubmitMiniGamePacket { Id = 9, Data = [0x11, 0x22] }
            .ToBody().Should().Equal(0x06, 0x09, 0x02, 0x11, 0x22);
        new SyncMiniGamePacket { Sequence = 0x01020304 }
            .ToBody().Should().Equal(0x07, 0x01, 0x02, 0x03, 0x04);
        new ResultMiniGamePacket { Flag = 1 }
            .ToBody().Should().Equal(0x08, 0x02, 0x01, 0x00);
    }

    [Fact]
    public void MiniGame_Parse_DispatchesResult()
    {
        var parsed = MiniGamePacket.Parse([0x08, 0x02, 0x01, 0x00])
            .Should().BeOfType<ResultMiniGamePacket>().Subject;
        parsed.Flag.Should().Be((byte)1);
    }

    // ---- 0x73 BrowserAction --------------------------------------------------------------------

    [Fact]
    public void BrowserAction_PinsLayouts()
    {
        new BrowserOpenedPacket().ToBody().Should().Equal(0x00);
        new BrowserNavigatePacket { Index = 4, Arg = 7 }
            .ToBody().Should().Equal(0x03, 0x04, 0x07);
    }

    // ---- round-trip through the codec (also proves auto-discovery) -----------------------------

    // Scalar/string forms have full record value equality, so a single Be(original) covers them.
    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void RoundTrip_ThroughCodec_PreservesForm(ClientPacket original)
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        parsed.Should().Be(original);
    }

    public static TheoryData<ClientPacket> RoundTripCases() =>
    [
        new GroupViewPacket(),
        new SendAlivePacket(),
        new RequestLoverNamePacket(),
        new OpenCashShopPacket(),
        new PurchaseCashShopItemPacket { Slot = 3, ItemId = 12345 },
        new CloseCashShopPacket(),
        new OpenMiniGamePacket(),
        new SyncMiniGamePacket { Sequence = 42 },
        new ResultMiniGamePacket { Flag = 1 },
        new BrowserOpenedPacket(),
        new BrowserNavigatePacket { Index = 1, Arg = 2 },
    ];

    // byte[]-carrying forms have reference-equal arrays under record equality, so assert field-wise.

    [Fact]
    public void Confirm_RoundTrips_ThroughCodec()
    {
        var original = new ConfirmPacket { Arg0 = 9, Arg1 = 8, Arg2 = 7, Payload = [0x01, 0x02, 0x03] };
        var codec = new PacketCodec();
        var crypto = MakeCrypto();

        var parsed = codec.ParseClientPacket(codec.EncodeClient(original, crypto), crypto)
            .Should().BeOfType<ConfirmPacket>().Subject;

        parsed.Arg0.Should().Be((byte)9);
        parsed.Arg1.Should().Be((byte)8);
        parsed.Arg2.Should().Be((byte)7);
        parsed.Payload.Should().Equal(0x01, 0x02, 0x03);
    }

    [Fact]
    public void Exception_RoundTrips_ThroughCodec()
    {
        var original = new ExceptionPacket { Data = [0xDE, 0xAD, 0xBE, 0xEF] };
        var codec = new PacketCodec();
        var crypto = MakeCrypto();

        var parsed = codec.ParseClientPacket(codec.EncodeClient(original, crypto), crypto)
            .Should().BeOfType<ExceptionPacket>().Subject;

        parsed.Data.Should().Equal(0xDE, 0xAD, 0xBE, 0xEF);
    }

    [Fact]
    public void SubmitMiniGame_RoundTrips_ThroughCodec()
    {
        var original = new SubmitMiniGamePacket { Id = 2, Data = [0xAB, 0xCD] };
        var codec = new PacketCodec();
        var crypto = MakeCrypto();

        var parsed = codec.ParseClientPacket(codec.EncodeClient(original, crypto), crypto)
            .Should().BeOfType<SubmitMiniGamePacket>().Subject;

        parsed.Id.Should().Be((byte)2);
        parsed.Data.Should().Equal(0xAB, 0xCD);
    }
}
