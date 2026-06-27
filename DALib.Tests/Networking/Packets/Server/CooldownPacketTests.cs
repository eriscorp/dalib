using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x3F Cooldown (S->C) - pins the <c>[u8 isSkill][u8 slot][u32 BE seconds]</c> body
///     and the codec round-trip for both the spell- and skill-pane forms.
/// </summary>
public class CooldownPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_Skill_PinsKnownLayout()
    {
        var packet = new CooldownPacket { IsSkill = true, Slot = 3, Seconds = 10 };

        // [01] isSkill [03] slot [00 00 00 0A] seconds BE
        packet.ToBody().Should().Equal(
            (byte)0x01,
            (byte)0x03,
            (byte)0x00, (byte)0x00, (byte)0x00, (byte)0x0A);
    }

    [Fact]
    public void WriteBody_Spell_LeadingByteIsZero()
    {
        var packet = new CooldownPacket { IsSkill = false, Slot = 1, Seconds = 5 };

        packet.ToBody().Should().Equal(
            (byte)0x00,
            (byte)0x01,
            (byte)0x00, (byte)0x00, (byte)0x00, (byte)0x05);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new CooldownPacket { IsSkill = true, Slot = 7, Seconds = 3600 };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<CooldownPacket>().Subject;
        typed.IsSkill.Should().BeTrue();
        typed.Slot.Should().Be((byte)7);
        typed.Seconds.Should().Be(3600u);
    }
}
