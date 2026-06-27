using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x0F AddItem (S->C) - pins the
///     <c>[u8 slot][u16 sprite][u8 color][string8 name][u32 count][u8 stackable][u32 maxDur][u32 curDur]</c>
///     body, the optional trailing slack preserved verbatim on round-trip, and the codec round-trip.
/// </summary>
public class AddItemPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_MinimalForm_PinsKnownLayout()
    {
        var packet = new AddItemPacket
        {
            Slot = 3,
            Sprite = 0x8064,
            Color = 7,
            Name = "ab",
            Count = 0x01020304,
            Stackable = true,
            MaxDurability = 0x0A0B0C0D,
            CurrentDurability = 0x0102030A,
        };

        // [03] slot [80 64] sprite BE [07] color [02 61 62] string8 "ab"
        // [01 02 03 04] count BE [01] stackable [0A 0B 0C 0D] maxDur [01 02 03 0A] curDur
        packet.ToBody().Should().Equal(
            (byte)0x03,
            (byte)0x80, (byte)0x64,
            (byte)0x07,
            (byte)0x02, (byte)0x61, (byte)0x62,
            (byte)0x01, (byte)0x02, (byte)0x03, (byte)0x04,
            (byte)0x01,
            (byte)0x0A, (byte)0x0B, (byte)0x0C, (byte)0x0D,
            (byte)0x01, (byte)0x02, (byte)0x03, (byte)0x0A);
    }

    [Fact]
    public void WriteBody_HybrasylSlack_AppendsVerbatim()
    {
        var packet = new AddItemPacket
        {
            Slot = 1,
            Sprite = 0x8001,
            Color = 0,
            Name = "x",
            Count = 1,
            Stackable = false,
            MaxDurability = 0,
            CurrentDurability = 0,
            TrailingSlack = [0x00, 0x00, 0x00, 0x00],
        };

        var body = packet.ToBody();

        // 18 + nameLen (1) body bytes + 4 trailing slack bytes
        body.Should().HaveCount(23);
        body[^4..].Should().Equal((byte)0x00, (byte)0x00, (byte)0x00, (byte)0x00);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new AddItemPacket
        {
            Slot = 59,
            Sprite = 0x812C,
            Color = 13,
            Name = "Stick",
            Count = 42,
            Stackable = true,
            MaxDurability = 1000,
            CurrentDurability = 567,
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<AddItemPacket>().Subject;
        typed.Slot.Should().Be((byte)59);
        typed.Sprite.Should().Be((ushort)0x812C);
        typed.Color.Should().Be((byte)13);
        typed.Name.Should().Be("Stick");
        typed.Count.Should().Be(42u);
        typed.Stackable.Should().BeTrue();
        typed.MaxDurability.Should().Be(1000u);
        typed.CurrentDurability.Should().Be(567u);
        typed.TrailingSlack.Should().BeNull();
    }

    [Fact]
    public void RoundTrip_ChaosStackableSlack_PreservedVerbatim()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new AddItemPacket
        {
            Slot = 2,
            Sprite = 0x8002,
            Color = 1,
            Name = "Mold",
            Count = 7,
            Stackable = true,
            MaxDurability = 0,
            CurrentDurability = 0,
            TrailingSlack = [0x00],
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<AddItemPacket>().Subject;
        typed.TrailingSlack.Should().Equal(0x00);
    }
}
