using System.IO;
using DALib.Networking.Packets.Server;

namespace DALib.Tests.Networking.Packets.Server;

public class MetafilePacketTests
{
    [Fact]
    public void Data_WriteBody_MatchesGroundedLayout()
    {
        // [u8 0x00][string8 Name][u32-BE Checksum][u16-BE DataLen][bytes Data]
        // - the by-name response; Data is the zlib-compressed blob, Checksum is the CRC of the decompressed bytes.
        var packet = new MetafileDataPacket
        {
            Name = "SClass1",
            Checksum = 0x01020304,
            Data = [0x78, 0x9C, 0xDE, 0xAD]
        };

        packet.ToBody().Should().Equal(
            (byte)0x00,                                          // MetafileType.DataByName
            (byte)0x07,                                          // string8 length ("SClass1")
            (byte)'S', (byte)'C', (byte)'l', (byte)'a', (byte)'s', (byte)'s', (byte)'1',
            (byte)0x01, (byte)0x02, (byte)0x03, (byte)0x04,      // u32-BE checksum
            (byte)0x00, (byte)0x04,                              // u16-BE data length (4)
            (byte)0x78, (byte)0x9C, (byte)0xDE, (byte)0xAD);     // compressed payload
    }

    [Fact]
    public void Checksums_WriteBody_MatchesGroundedLayout()
    {
        // [u8 0x01][u16-BE Count]{[string8 Name][u32-BE Checksum]} - the manifest form.
        var packet = new MetafileChecksumsPacket
        {
            Entries =
            [
                new MetafileEntry("ItemInfo0", 0xCAFEF00D),
                new MetafileEntry("NationDesc", 0x11223344)
            ]
        };

        packet.ToBody().Should().Equal(
            (byte)0x01,                                          // MetafileType.AllCheckSums
            (byte)0x00, (byte)0x02,                              // u16-BE count (2)
            (byte)0x09,                                          // string8 length ("ItemInfo0")
            (byte)'I', (byte)'t', (byte)'e', (byte)'m', (byte)'I', (byte)'n', (byte)'f', (byte)'o', (byte)'0',
            (byte)0xCA, (byte)0xFE, (byte)0xF0, (byte)0x0D,      // u32-BE checksum
            (byte)0x0A,                                          // string8 length ("NationDesc")
            (byte)'N', (byte)'a', (byte)'t', (byte)'i', (byte)'o', (byte)'n', (byte)'D', (byte)'e', (byte)'s', (byte)'c',
            (byte)0x11, (byte)0x22, (byte)0x33, (byte)0x44);     // u32-BE checksum
    }

    [Fact]
    public void Data_RoundTrip()
    {
        var packet = new MetafileDataPacket
        {
            Name = "NPCIllust",
            Checksum = 0xDEADBEEF,
            Data = [0x78, 0x9C, 0x00, 0x11, 0x22]
        };

        var parsed = MetafilePacket.Parse(packet.ToBody());

        var data = parsed.Should().BeOfType<MetafileDataPacket>().Which;
        data.MetafileType.Should().Be(MetafileType.DataByName);
        data.Name.Should().Be("NPCIllust");
        data.Checksum.Should().Be(0xDEADBEEF);
        data.Data.Should().Equal((byte)0x78, (byte)0x9C, (byte)0x00, (byte)0x11, (byte)0x22);
    }

    [Fact]
    public void Checksums_RoundTrip()
    {
        var packet = new MetafileChecksumsPacket
        {
            Entries =
            [
                new MetafileEntry("SClass0", 0x00000001),
                new MetafileEntry("SClass1", 0xFFFFFFFF)
            ]
        };

        var parsed = MetafilePacket.Parse(packet.ToBody());

        var checksums = parsed.Should().BeOfType<MetafileChecksumsPacket>().Which;
        checksums.MetafileType.Should().Be(MetafileType.AllCheckSums);
        checksums.Entries.Should().Equal(
            new MetafileEntry("SClass0", 0x00000001),
            new MetafileEntry("SClass1", 0xFFFFFFFF));
    }

    [Fact]
    public void Checksums_Empty_RoundTrips()
    {
        var packet = new MetafileChecksumsPacket();

        packet.ToBody().Should().Equal((byte)0x01, (byte)0x00, (byte)0x00); // type + u16-BE count 0

        var parsed = MetafilePacket.Parse(packet.ToBody());
        parsed.Should().BeOfType<MetafileChecksumsPacket>().Which.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Data_EmptyPayload_RoundTrips()
    {
        var packet = new MetafileDataPacket { Name = "Empty", Checksum = 0, Data = [] };
        var parsed = MetafilePacket.Parse(packet.ToBody());

        parsed.Should().BeOfType<MetafileDataPacket>().Which.Data.Should().BeEmpty();
    }

    [Fact]
    public void Parse_UnknownType_Throws()
    {
        var act = () => MetafilePacket.Parse(new byte[] { 0x02 });

        act.Should().Throw<InvalidDataException>().WithMessage("*unknown type 0x02*");
    }
}
