using DALib.Enums;
using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x33 DisplayUser (S->C) - pins the core + discriminated appearance layout for both
///     forms (full equipment and creature-sprite override), the 0xFFFF discriminator, the codec
///     round-trip, and that a creature sprite of 0xFFFF... is not possible (sentinel collision is the
///     form selector, not a sprite value).
/// </summary>
public class DisplayUserPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    private static EquipmentAppearance SampleEquipment() => new()
    {
        HeadSprite = 0x1234,
        BodySprite = 0x10,
        ArmorSprite1 = 0x1111,
        BootsSprite = 0x22,
        ArmorSprite2 = 0x1111,
        ShieldSprite = 0x33,
        WeaponSprite = 0x4444,
        HeadColor = 1,
        BootsColor = 2,
        AccessoryColor1 = 3,
        AccessorySprite1 = 0x5555,
        AccessoryColor2 = 4,
        AccessorySprite2 = 0x6666,
        AccessoryColor3 = 5,
        AccessorySprite3 = 0x7777,
        LanternSize = 1,
        RestPosition = 2,
        OvercoatSprite = 0x8888,
        OvercoatColor = 6,
        BodyColor = 7,
        IsHidden = false,
        FaceSprite = 9,
    };

    [Fact]
    public void WriteBody_CreatureSpriteForm_PinsKnownLayout()
    {
        var packet = new DisplayUserPacket
        {
            X = 0x0102,
            Y = 0x0304,
            Direction = Direction.South,
            Id = 0x11223344,
            Appearance = new CreatureSpriteAppearance { Sprite = 0xABCD, HeadColor = 7, BootsColor = 8 },
            NameTagStyle = 0,
            Name = "Bee",
            GroupName = "",
        };

        packet.ToBody().Should().Equal(
            // core
            (byte)0x01, (byte)0x02,             // X
            (byte)0x03, (byte)0x04,             // Y
            (byte)0x02,                         // direction = south
            (byte)0x11, (byte)0x22, (byte)0x33, (byte)0x44, // id
            // appearance: creature sprite
            (byte)0xFF, (byte)0xFF,             // discriminator sentinel
            (byte)0xAB, (byte)0xCD,             // sprite
            (byte)0x07,                         // headColor
            (byte)0x08,                         // bootsColor
            (byte)0x00, (byte)0x00, (byte)0x00, (byte)0x00, (byte)0x00, (byte)0x00, // 6 reserved
            // tail
            (byte)0x00,                         // nameTagStyle
            (byte)0x03, (byte)'B', (byte)'e', (byte)'e',     // string8 Name
            (byte)0x00);                        // string8 GroupName (empty)
    }

    [Fact]
    public void WriteBody_EquipmentForm_DiscriminatorIsHeadSprite()
    {
        var packet = new DisplayUserPacket
        {
            X = 1,
            Y = 2,
            Direction = Direction.North,
            Id = 0xDEADBEEF,
            Appearance = SampleEquipment(),
            NameTagStyle = 0,
            Name = "X",
            GroupName = "",
        };

        var body = packet.ToBody();

        // The equipment form's leading u16 (the discriminator) is HeadSprite (0x1234), BE.
        body[9].Should().Be(0x12);
        body[10].Should().Be(0x34);
    }

    [Fact]
    public void RoundTrip_EquipmentForm_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new DisplayUserPacket
        {
            X = 40,
            Y = 17,
            Direction = Direction.West,
            Id = 0xDEADBEEF,
            Appearance = SampleEquipment(),
            NameTagStyle = 3,
            Name = "Aisling",
            GroupName = "MyGroup",
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<DisplayUserPacket>().Subject;
        typed.X.Should().Be((ushort)40);
        typed.Y.Should().Be((ushort)17);
        typed.Direction.Should().Be(Direction.West);
        typed.Id.Should().Be(0xDEADBEEFu);
        typed.NameTagStyle.Should().Be((byte)3);
        typed.Name.Should().Be("Aisling");
        typed.GroupName.Should().Be("MyGroup");
        typed.Appearance.Should().Be(SampleEquipment());
    }

    [Fact]
    public void RoundTrip_CreatureSpriteForm_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new DisplayUserPacket
        {
            X = 5,
            Y = 6,
            Direction = Direction.East,
            Id = 0xCAFEBABE,
            Appearance = new CreatureSpriteAppearance { Sprite = 0x0BEE, HeadColor = 1, BootsColor = 2 },
            NameTagStyle = 1,
            Name = "Polymorphed",
            GroupName = "",
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<DisplayUserPacket>().Subject;
        var appearance = typed.Appearance.Should().BeOfType<CreatureSpriteAppearance>().Subject;
        appearance.Sprite.Should().Be((ushort)0x0BEE);
        appearance.HeadColor.Should().Be((byte)1);
        appearance.BootsColor.Should().Be((byte)2);
        typed.Name.Should().Be("Polymorphed");
    }
}
