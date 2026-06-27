using System.IO;
using DALib.Networking.Packets.Server;

namespace DALib.Tests.Networking.Packets.Server;

public class LoginNotificationPacketTests
{
    [Fact]
    public void Checksum_WriteBody_MatchesGroundedLayout()
    {
        // [bool false][u32-BE checksum] - subtype 0x00 plus checksum.
        var packet = new LoginNotificationPacket { Form = new NotificationChecksumForm { Checksum = 0x01020304 } };

        packet.ToBody().Should().Equal(
            (byte)0x00,                                         // IsFullResponse = false
            (byte)0x01, (byte)0x02, (byte)0x03, (byte)0x04);   // u32-BE checksum
    }

    [Fact]
    public void Data_WriteBody_MatchesGroundedLayout()
    {
        // [bool true][u16-BE length][bytes] - subtype 0x01 plus u16 length plus payload.
        var packet = new LoginNotificationPacket
        {
            Form = new NotificationDataForm { Data = [0xDE, 0xAD, 0xBE, 0xEF] },
        };

        packet.ToBody().Should().Equal(
            (byte)0x01,                                         // IsFullResponse = true
            (byte)0x00, (byte)0x04,                             // u16-BE length (4)
            (byte)0xDE, (byte)0xAD, (byte)0xBE, (byte)0xEF);    // payload
    }

    [Fact]
    public void Checksum_RoundTrip()
    {
        var packet = new LoginNotificationPacket { Form = new NotificationChecksumForm { Checksum = 0xCAFEF00D } };
        var parsed = LoginNotificationPacket.Parse(packet.ToBody());

        parsed.Form.Should().BeOfType<NotificationChecksumForm>().Which.Checksum.Should().Be(0xCAFEF00D);
    }

    [Fact]
    public void Data_RoundTrip()
    {
        var payload = new byte[] { 0x78, 0x9C, 0x00, 0x11, 0x22 };
        var packet = new LoginNotificationPacket { Form = new NotificationDataForm { Data = payload } };
        var parsed = LoginNotificationPacket.Parse(packet.ToBody());

        parsed.Form.Should().BeOfType<NotificationDataForm>().Which.Data.Should().Equal(payload);
    }
}
