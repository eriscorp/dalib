using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x05 RequestMap (C->S) - pins the 10-byte wire layout (two reserved
///     u16s, X, Y, u24-BE CRC), the round-trip, and the always-zero CRC high byte.
/// </summary>
public class RequestMapPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new RequestMapPacket
        {
            X = 0x12,
            Y = 0x34,
            CachedCrc = 0xABCD,
        };

        // [00 00] reserved [00 00] reserved [12] X [34] Y [00 AB CD] u24-BE CRC
        packet.ToBody().Should().Equal(
            (byte)0x00, (byte)0x00,
            (byte)0x00, (byte)0x00,
            (byte)0x12,
            (byte)0x34,
            (byte)0x00, (byte)0xAB, (byte)0xCD);
    }

    [Fact]
    public void CachedCrc_DefaultsToZero_ForNoCachedMap()
    {
        // CRC 0 indicates no cached on-disk tile file for the map.
        var packet = new RequestMapPacket { X = 0x12, Y = 0x34 };

        packet.CachedCrc.Should().Be(0);
        packet.ToBody().Should().Equal(
            (byte)0x00, (byte)0x00,
            (byte)0x00, (byte)0x00,
            (byte)0x12,
            (byte)0x34,
            (byte)0x00, (byte)0x00, (byte)0x00);
    }

    [Fact]
    public void RoundTrip_PreservesAllFields()
    {
        var original = new RequestMapPacket
        {
            X = 0x40,
            Y = 0x40,
            CachedCrc = 0x7FFF,
        };

        var parsed = RequestMapPacket.Parse(original.ToBody());

        parsed.X.Should().Be(0x40);
        parsed.Y.Should().Be(0x40);
        parsed.CachedCrc.Should().Be(0x7FFF);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new RequestMapPacket
        {
            X = 0x0A,
            Y = 0x0B,
            CachedCrc = 0xC0DE,
        };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<RequestMapPacket>().Subject;
        typed.X.Should().Be(0x0A);
        typed.Y.Should().Be(0x0B);
        typed.CachedCrc.Should().Be(0xC0DE);
    }
}
