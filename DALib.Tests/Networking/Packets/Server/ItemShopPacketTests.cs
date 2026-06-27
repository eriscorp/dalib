using System;
using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x45 ItemShop (S->C) - discriminated on [u8 Flag]: non-zero is bare, 0 carries
///     [u8][tail]. Modeled for protocol completeness; not emitted by typical servers.
/// </summary>
public class ItemShopPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_BareForm_IsSingleFlagByte()
    {
        new ItemShopPacket { Flag = 0x05 }.ToBody().Should().Equal((byte)0x05);
    }

    [Fact]
    public void WriteBody_ContentForm_CarriesByteAndTail()
    {
        new ItemShopPacket { Flag = 0, ContentByte = 0x09, Content = [0xAA, 0xBB] }
            .ToBody().Should().Equal((byte)0x00, (byte)0x09, (byte)0xAA, (byte)0xBB);
    }

    [Fact]
    public void WriteBody_ContentForm_MissingContent_Throws()
    {
        var act = () => new ItemShopPacket { Flag = 0 }.ToBody();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Parse_BareForm_HasNoContent()
    {
        var parsed = ItemShopPacket.Parse([0x05]);

        parsed.Flag.Should().Be((byte)0x05);
        parsed.ContentByte.Should().BeNull();
        parsed.Content.Should().BeNull();
    }

    [Fact]
    public void Parse_ContentForm_ReadsByteAndTail()
    {
        var parsed = ItemShopPacket.Parse([0x00, 0x09, 0xAA, 0xBB]);

        parsed.Flag.Should().Be((byte)0x00);
        parsed.ContentByte.Should().Be((byte)0x09);
        parsed.Content.Should().Equal((byte)0xAA, (byte)0xBB);
    }

    [Fact]
    public void RoundTrip_BareForm_ThroughCodec()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var parsed = codec.ParseServerPacket(codec.EncodeServer(new ItemShopPacket { Flag = 0x07 }, crypto), crypto)
            .Should().BeOfType<ItemShopPacket>().Subject;

        parsed.Flag.Should().Be((byte)0x07);
        parsed.ContentByte.Should().BeNull();
        parsed.Content.Should().BeNull();
    }

    [Fact]
    public void RoundTrip_ContentForm_ThroughCodec()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new ItemShopPacket { Flag = 0, ContentByte = 0x42, Content = [0x10, 0x20] };

        var parsed = codec.ParseServerPacket(codec.EncodeServer(original, crypto), crypto)
            .Should().BeOfType<ItemShopPacket>().Subject;

        parsed.Flag.Should().Be((byte)0);
        parsed.ContentByte.Should().Be((byte)0x42);
        parsed.Content.Should().Equal((byte)0x10, (byte)0x20);
    }
}
