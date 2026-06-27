using System;
using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x62 WebBoard (S->C) - discriminated on [u8 Type]: type 3 is [string8 url][string8],
///     otherwise [string8][string8][string8]. Modeled for protocol completeness; not emitted by typical servers.
/// </summary>
public class WebBoardPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_UrlForm_TwoStrings()
    {
        new WebBoardPacket { Type = 3, Url = "A", Trailing = "B" }
            .ToBody().Should().Equal(
                (byte)0x03,
                (byte)0x01, (byte)'A',
                (byte)0x01, (byte)'B');
    }

    [Fact]
    public void WriteBody_GeneralForm_ThreeStrings()
    {
        new WebBoardPacket { Type = 1, First = "A", Second = "B", Trailing = "C" }
            .ToBody().Should().Equal(
                (byte)0x01,
                (byte)0x01, (byte)'A',
                (byte)0x01, (byte)'B',
                (byte)0x01, (byte)'C');
    }

    [Fact]
    public void WriteBody_UrlForm_MissingUrl_Throws()
    {
        var act = () => new WebBoardPacket { Type = 3, Trailing = "B" }.ToBody();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void WriteBody_GeneralForm_MissingStrings_Throws()
    {
        var act = () => new WebBoardPacket { Type = 1, First = "A", Trailing = "C" }.ToBody();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Parse_UrlForm()
    {
        var parsed = WebBoardPacket.Parse([0x03, 0x01, (byte)'A', 0x01, (byte)'B']);

        parsed.Type.Should().Be((byte)3);
        parsed.Url.Should().Be("A");
        parsed.First.Should().BeNull();
        parsed.Second.Should().BeNull();
        parsed.Trailing.Should().Be("B");
    }

    [Fact]
    public void Parse_GeneralForm()
    {
        var parsed = WebBoardPacket.Parse([0x01, 0x01, (byte)'A', 0x01, (byte)'B', 0x01, (byte)'C']);

        parsed.Type.Should().Be((byte)1);
        parsed.Url.Should().BeNull();
        parsed.First.Should().Be("A");
        parsed.Second.Should().Be("B");
        parsed.Trailing.Should().Be("C");
    }

    [Fact]
    public void RoundTrip_UrlForm_ThroughCodec()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new WebBoardPacket { Type = 3, Url = "http://example.com/", Trailing = "title" };

        codec.ParseServerPacket(codec.EncodeServer(original, crypto), crypto).Should().Be(original);
    }

    [Fact]
    public void RoundTrip_GeneralForm_ThroughCodec()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new WebBoardPacket { Type = 0, First = "one", Second = "two", Trailing = "three" };

        codec.ParseServerPacket(codec.EncodeServer(original, crypto), crypto).Should().Be(original);
    }
}
