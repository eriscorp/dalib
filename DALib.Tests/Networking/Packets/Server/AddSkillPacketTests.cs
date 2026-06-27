using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x2C AddSkill (S->C) - pins the <c>[u8 slot][u16 icon][string8 name]</c> body
///     and the codec round-trip.
/// </summary>
public class AddSkillPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new AddSkillPacket
        {
            Slot = 9,
            Icon = 0x0203,
            Name = "ab",
        };

        // [09] slot [02 03] icon BE [02 61 62] string8 "ab"
        packet.ToBody().Should().Equal(
            (byte)0x09,
            (byte)0x02, (byte)0x03,
            (byte)0x02, (byte)0x61, (byte)0x62);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new AddSkillPacket
        {
            Slot = 72,
            Icon = 88,
            Name = "Assail (Lev:100/100)",
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<AddSkillPacket>().Subject;
        typed.Slot.Should().Be((byte)72);
        typed.Icon.Should().Be((ushort)88);
        typed.Name.Should().Be("Assail (Lev:100/100)");
    }
}
