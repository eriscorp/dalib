using System;
using System.IO;
using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x4B Bounce (S->C) - pins the <c>[u16-BE innerLen][u8 ClientOpcode][bytes Data]</c>
///     layout (a big-endian u16 followed by that many raw bytes), the derived length prefix, and the
///     codec round-trip.
/// </summary>
public class BouncePacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new BouncePacket
        {
            ClientOpcode = ClientOpcode.Walk, // 0x06
            Data = [0x01, 0x02, 0x03]
        };

        // [u16-BE innerLen = 4][u8 opcode 0x06][03 data bytes]
        packet.ToBody().Should().Equal(
            (byte)0x00, (byte)0x04, // u16-BE innerLen = 1 (opcode) + 3 (data)
            (byte)0x06,             // forced ClientOpcode (Walk)
            (byte)0x01, (byte)0x02, (byte)0x03);
    }

    [Fact]
    public void WriteBody_EmptyData_LengthIsJustTheOpcode()
    {
        var packet = new BouncePacket { ClientOpcode = ClientOpcode.RequestMap, Data = [] };

        // [u16-BE 1][u8 opcode 0x05] - innerLen counts the opcode byte only
        packet.ToBody().Should().Equal((byte)0x00, (byte)0x01, (byte)0x05);
    }

    [Fact]
    public void RoundTrip_PreservesOpcodeAndData()
    {
        var packet = new BouncePacket
        {
            ClientOpcode = ClientOpcode.DropItem,
            Data = [0xAA, 0xBB, 0xCC, 0xDD]
        };

        var parsed = BouncePacket.Parse(packet.ToBody());

        parsed.ClientOpcode.Should().Be(ClientOpcode.DropItem);
        parsed.Data.Should().Equal((byte)0xAA, (byte)0xBB, (byte)0xCC, (byte)0xDD);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new BouncePacket
        {
            ClientOpcode = ClientOpcode.PickupItem,
            Data = [0x10, 0x20]
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<BouncePacket>().Subject;
        typed.ClientOpcode.Should().Be(ClientOpcode.PickupItem);
        typed.Data.Should().Equal((byte)0x10, (byte)0x20);
    }

    [Fact]
    public void Parse_TooShortInnerLength_Throws()
    {
        // innerLen = 0 cannot even contain the forced opcode byte.
        var act = () => BouncePacket.Parse(new byte[] { 0x00, 0x00 });

        act.Should().Throw<InvalidDataException>().WithMessage("*too small*");
    }
}
