using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x17 AddSpell (S->C) - pins the
///     <c>[u8 slot][u16 icon][u8 useType][string8 name][string8 prompt][u8 castLines]</c> body and
///     the codec round-trip.
/// </summary>
public class AddSpellPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new AddSpellPacket
        {
            Slot = 5,
            Icon = 0x0102,
            UseType = SpellUseType.Target,
            Name = "ab",
            Prompt = "c",
            CastLines = 2,
        };

        // [05] slot [01 02] icon BE [02] useType=Target [02 61 62] string8 "ab"
        // [01 63] string8 "c" [02] castLines
        packet.ToBody().Should().Equal(
            (byte)0x05,
            (byte)0x01, (byte)0x02,
            (byte)0x02,
            (byte)0x02, (byte)0x61, (byte)0x62,
            (byte)0x01, (byte)0x63,
            (byte)0x02);
    }

    [Fact]
    public void WriteBody_EmptyPrompt_WritesZeroLengthString()
    {
        var packet = new AddSpellPacket
        {
            Slot = 1,
            Icon = 40,
            UseType = SpellUseType.NoTarget,
            Name = "x",
            CastLines = 0,
        };

        // [01] slot [00 28] icon [05] useType=NoTarget [01 78] "x" [00] empty prompt [00] castLines
        packet.ToBody().Should().Equal(
            (byte)0x01,
            (byte)0x00, (byte)0x28,
            (byte)0x05,
            (byte)0x01, (byte)0x78,
            (byte)0x00,
            (byte)0x00);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new AddSpellPacket
        {
            Slot = 36,
            Icon = 213,
            UseType = SpellUseType.Prompt,
            Name = "ard ioc",
            Prompt = "Who needs healing?",
            CastLines = 4,
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<AddSpellPacket>().Subject;
        typed.Slot.Should().Be((byte)36);
        typed.Icon.Should().Be((ushort)213);
        typed.UseType.Should().Be(SpellUseType.Prompt);
        typed.Name.Should().Be("ard ioc");
        typed.Prompt.Should().Be("Who needs healing?");
        typed.CastLines.Should().Be((byte)4);
    }
}
