using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x0F UseSpell (C->S). The body is <c>[u8 slot][args...]</c> where the args are
///     spell-type-dependent with no wire discriminator: empty (no-target), <c>[u32 serial][u16 x]
///     [u16 y]</c> (targeted), or raw text (prompt). Pins each shape and the round-trip.
/// </summary>
public class UseSpellPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void NoTarget_WriteBody_IsJustTheSlot()
    {
        var packet = UseSpellPacket.NoTarget(0x03);

        packet.Args.Should().BeEmpty();
        packet.ToBody().Should().Equal((byte)0x03);
    }

    [Fact]
    public void Targeted_WriteBody_PinsCapturedLayout()
    {
        // slot 1, target serial 0x000C15BC, point (0x0027, 0x0007).
        var packet = UseSpellPacket.Targeted(0x01, 0x000C15BC, 0x0027, 0x0007);

        // [01] slot [00 0C 15 BC] u32-BE serial [00 27] u16-BE x [00 07] u16-BE y
        packet.ToBody().Should().Equal(
            (byte)0x01,
            (byte)0x00, (byte)0x0C, (byte)0x15, (byte)0xBC,
            (byte)0x00, (byte)0x27,
            (byte)0x00, (byte)0x07);
    }

    [Fact]
    public void Targeted_SerialOnly_ZeroesThePoint()
    {
        var packet = UseSpellPacket.Targeted(0x01, 0x000C15BC);

        // [01] slot [00 0C 15 BC] u32-BE serial [00 00] x=0 [00 00] y=0
        packet.ToBody().Should().Equal(
            (byte)0x01,
            (byte)0x00, (byte)0x0C, (byte)0x15, (byte)0xBC,
            (byte)0x00, (byte)0x00,
            (byte)0x00, (byte)0x00);
    }

    [Fact]
    public void Prompt_WriteBody_IsSlotThenRawLatin1Text()
    {
        var packet = UseSpellPacket.Prompt(0x05, "abc");

        // [05] slot ['a' 'b' 'c'] - no length prefix
        packet.ToBody().Should().Equal((byte)0x05, (byte)0x61, (byte)0x62, (byte)0x63);
    }

    [Fact]
    public void RoundTrip_NoTarget_PreservesSlotAndEmptyArgs()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = UseSpellPacket.NoTarget(0x2A);

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<UseSpellPacket>().Subject;
        typed.Slot.Should().Be(0x2A);
        typed.Args.Should().BeEmpty();
    }

    [Fact]
    public void RoundTrip_Targeted_PreservesSlotAndArgs()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = UseSpellPacket.Targeted(0x01, 0x000C15BC, 0x0027, 0x0007);

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<UseSpellPacket>().Subject;
        typed.Slot.Should().Be(0x01);
        typed.Args.Should().Equal(original.Args);
    }

    [Fact]
    public void RoundTrip_Prompt_PreservesSlotAndText()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = UseSpellPacket.Prompt(0x07, "hello world");

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<UseSpellPacket>().Subject;
        typed.Slot.Should().Be(0x07);
        typed.Args.Should().Equal(original.Args);
    }
}
