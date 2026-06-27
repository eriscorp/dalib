using System;
using System.IO;
using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x43 Click (C->S) - pins both variants (entity by serial vs tile by x,y), the
///     discriminator byte, the round-trips, the by-variant guards, and rejection of an unknown
///     click type.
/// </summary>
public class ClickPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_Entity_PinsTypeThenSerial()
    {
        var packet = ClickPacket.Entity(0xDEADBEEF);

        // [01] entity [DE AD BE EF] u32-BE target id
        packet.ToBody().Should().Equal(
            (byte)0x01,
            (byte)0xDE, (byte)0xAD, (byte)0xBE, (byte)0xEF);
    }

    [Fact]
    public void WriteBody_Point_PinsTypeThenCoords()
    {
        var packet = ClickPacket.Point(0x1234, 0xABCD);

        // [03] point [12 34] u16-BE x [AB CD] u16-BE y
        packet.ToBody().Should().Equal(
            (byte)0x03,
            (byte)0x12, (byte)0x34,
            (byte)0xAB, (byte)0xCD);
    }

    [Fact]
    public void RoundTrip_Entity_PreservesTargetId()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = ClickPacket.Entity(0x000A_BCDE);

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<ClickPacket>().Subject;
        typed.ClickType.Should().Be(ClickPacket.ClickTypeEntity);
        typed.TargetId.Should().Be(0x000A_BCDEu);
        typed.X.Should().BeNull();
        typed.Y.Should().BeNull();
    }

    [Fact]
    public void RoundTrip_Point_PreservesCoords()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = ClickPacket.Point(10, 11);

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<ClickPacket>().Subject;
        typed.ClickType.Should().Be(ClickPacket.ClickTypePoint);
        typed.X.Should().Be((ushort)10);
        typed.Y.Should().Be((ushort)11);
        typed.TargetId.Should().BeNull();
    }

    [Fact]
    public void WriteBody_EntityWithNullTargetId_Throws()
    {
        var packet = new ClickPacket { ClickType = ClickPacket.ClickTypeEntity, TargetId = null };

        var act = () => packet.ToBody();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Parse_UnknownClickType_Throws()
    {
        // [56] is an undocumented click type and must be rejected.
        var act = () => ClickPacket.Parse([0x56]);

        act.Should().Throw<InvalidDataException>();
    }
}
