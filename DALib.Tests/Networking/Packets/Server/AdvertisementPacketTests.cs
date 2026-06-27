using System;
using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x5B Advertisement (S->C) - body [u16 len][len bytes][u16][u16][u8]. Modeled for
///     protocol completeness; not emitted by typical servers.
/// </summary>
public class AdvertisementPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsLengthPrefixedBlobThenTrailers()
    {
        new AdvertisementPacket { Data = [0xAA, 0xBB], Unknown1 = 0x0102, Unknown2 = 0x0304, Unknown3 = 0x7F }
            .ToBody()
            .Should()
            .Equal(
                (byte)0x00, (byte)0x02,           // u16 len = 2
                (byte)0xAA, (byte)0xBB,           // blob
                (byte)0x01, (byte)0x02,           // u16 Unknown1
                (byte)0x03, (byte)0x04,           // u16 Unknown2
                (byte)0x7F);                       // u8 Unknown3
    }

    [Fact]
    public void Parse_ReadsBlobAndTrailers()
    {
        var parsed = AdvertisementPacket.Parse([0x00, 0x02, 0xAA, 0xBB, 0x01, 0x02, 0x03, 0x04, 0x7F]);

        parsed.Data.Should().Equal((byte)0xAA, (byte)0xBB);
        parsed.Unknown1.Should().Be((ushort)0x0102);
        parsed.Unknown2.Should().Be((ushort)0x0304);
        parsed.Unknown3.Should().Be((byte)0x7F);
    }

    [Fact]
    public void WriteBody_OversizedData_Throws()
    {
        var packet = new AdvertisementPacket { Data = new byte[ushort.MaxValue + 1], Unknown1 = 0, Unknown2 = 0, Unknown3 = 0 };

        var act = () => packet.ToBody();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new AdvertisementPacket { Data = [0x10, 0x20, 0x30], Unknown1 = 0xABCD, Unknown2 = 0x1234, Unknown3 = 0x55 };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto).Should().BeOfType<AdvertisementPacket>().Subject;

        parsed.Data.Should().Equal((byte)0x10, (byte)0x20, (byte)0x30);
        parsed.Unknown1.Should().Be((ushort)0xABCD);
        parsed.Unknown2.Should().Be((ushort)0x1234);
        parsed.Unknown3.Should().Be((byte)0x55);
    }
}
