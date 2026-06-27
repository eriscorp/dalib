using System.IO;
using DALib.Networking.Packets.Server;

namespace DALib.Tests.Networking.Packets.Server;

public class UrlPacketTests
{
    [Fact]
    public void SetUrl_WriteBody_MatchesGroundedLayout()
    {
        // [u8 subtype=3][string8 url].
        var packet = new UrlPacket { Form = new SetUrlForm { Url = "http://www.hybrasyl.com" } };

        packet.ToBody().Should().Equal(
            (byte)0x03,                                                     // subtype = SetUrl
            (byte)0x17,                                                     // string8 length (23)
            (byte)0x68, (byte)0x74, (byte)0x74, (byte)0x70, (byte)0x3A,     // "http:"
            (byte)0x2F, (byte)0x2F, (byte)0x77, (byte)0x77, (byte)0x77,     // "//www"
            (byte)0x2E, (byte)0x68, (byte)0x79, (byte)0x62, (byte)0x72,     // ".hybr"
            (byte)0x61, (byte)0x73, (byte)0x79, (byte)0x6C, (byte)0x2E,     // "asyl."
            (byte)0x63, (byte)0x6F, (byte)0x6D);                            // "com"
    }

    [Fact]
    public void SetUrl_RoundTrip()
    {
        var packet = new UrlPacket { Form = new SetUrlForm { Url = "https://example.test/home" } };
        var parsed = UrlPacket.Parse(packet.ToBody());

        parsed.Form.Should().BeOfType<SetUrlForm>().Which.Url.Should().Be("https://example.test/home");
    }

    [Fact]
    public void UrlAlert_WriteBody_MatchesGroundedLayout()
    {
        // [u8 subtype][string16 url][string16 message].
        var packet = new UrlPacket
        {
            Form = new UrlAlertForm { Url = "http://x.test", Message = "hi", CloseClient = false },
        };

        packet.ToBody().Should().Equal(
            (byte)0x02,                                                     // subtype = redirect (stay)
            (byte)0x00, (byte)0x0D,                                         // string16-BE length (13) of url
            (byte)0x68, (byte)0x74, (byte)0x74, (byte)0x70, (byte)0x3A,     // "http:"
            (byte)0x2F, (byte)0x2F, (byte)0x78, (byte)0x2E, (byte)0x74,     // "//x.t"
            (byte)0x65, (byte)0x73, (byte)0x74,                             // "est"
            (byte)0x00, (byte)0x02,                                         // string16-BE length (2) of message
            (byte)0x68, (byte)0x69);                                        // "hi"
    }

    [Fact]
    public void UrlAlert_CloseClient_SelectsSubtype01()
    {
        var close = new UrlPacket { Form = new UrlAlertForm { Url = "u", Message = "m", CloseClient = true } };
        var stay = new UrlPacket { Form = new UrlAlertForm { Url = "u", Message = "m", CloseClient = false } };

        close.ToBody()[0].Should().Be(0x01); // redirect-and-quit
        stay.ToBody()[0].Should().Be(0x02);  // redirect-and-stay
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UrlAlert_RoundTrip(bool closeClient)
    {
        var packet = new UrlPacket
        {
            Form = new UrlAlertForm { Url = "file://host/share", Message = "Visit our homepage!", CloseClient = closeClient },
        };

        var parsed = UrlPacket.Parse(packet.ToBody());

        var form = parsed.Form.Should().BeOfType<UrlAlertForm>().Subject;
        form.Url.Should().Be("file://host/share");
        form.Message.Should().Be("Visit our homepage!");
        form.CloseClient.Should().Be(closeClient);
    }

    [Fact]
    public void Parse_UnknownSubtype_Throws()
    {
        var act = () => UrlPacket.Parse([0x05, 0x00, 0x00]);
        act.Should().Throw<InvalidDataException>();
    }
}
