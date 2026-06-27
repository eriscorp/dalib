using DALib.Enums;
using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x37 AddEquipment (S->C) - pins the
///     <c>[u8 slot][u16 sprite][u8 color][string8 name][u8 reserved][u32 maxDur][u32 curDur]</c>
///     body (including the mid-packet reserved byte) and the codec round-trip.
/// </summary>
public class AddEquipmentPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new AddEquipmentPacket
        {
            Slot = EquipmentSlot.Armor,
            Sprite = 0x8064,
            Color = 7,
            Name = "ab",
            MaxDurability = 0x0A0B0C0D,
            CurrentDurability = 0x0102030A,
        };

        // [02] slot=Armor [80 64] sprite BE [07] color [02 61 62] string8 "ab"
        // [00] reserved [0A 0B 0C 0D] maxDur BE [01 02 03 0A] curDur BE
        packet.ToBody().Should().Equal(
            (byte)0x02,
            (byte)0x80, (byte)0x64,
            (byte)0x07,
            (byte)0x02, (byte)0x61, (byte)0x62,
            (byte)0x00,
            (byte)0x0A, (byte)0x0B, (byte)0x0C, (byte)0x0D,
            (byte)0x01, (byte)0x02, (byte)0x03, (byte)0x0A);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new AddEquipmentPacket
        {
            Slot = EquipmentSlot.Weapon,
            Sprite = 0x80FA,
            Color = 0,
            Name = "Eppe",
            Unknown1 = 0x5A,
            MaxDurability = 5000,
            CurrentDurability = 4999,
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<AddEquipmentPacket>().Subject;
        typed.Slot.Should().Be(EquipmentSlot.Weapon);
        typed.Sprite.Should().Be((ushort)0x80FA);
        typed.Color.Should().Be((byte)0);
        typed.Name.Should().Be("Eppe");
        typed.Unknown1.Should().Be((byte)0x5A);
        typed.MaxDurability.Should().Be(5000u);
        typed.CurrentDurability.Should().Be(4999u);
    }
}
