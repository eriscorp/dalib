using System;
using DALib.Enums;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x33 (S->C) - display another player ("aisling") entering the receiving view: map position,
///     facing direction, serial, then a discriminated appearance block, then the name and group
///     tags. This is the player-visibility counterpart of 0x07 DrawObjects (which carries items,
///     gold, and creatures/NPCs).
/// </summary>
/// <remarks>
///     Body: <c>[u16 X][u16 Y][u8 Direction][u32 Id]</c>, then a <see cref="DisplayUserAppearance" />
///     block, then <c>[u8 NameTagStyle][string8 Name][string8 GroupName]</c>. The appearance block
///     opens with a <c>u16</c> discriminator: the sentinel <c>0xFFFF</c>
///     (<see cref="CreatureSpriteAppearance" />) selects a single creature-sprite override (used for
///     polymorph / display-as-monster); any other value is the head-sprite of a full
///     <see cref="EquipmentAppearance" /> block.
/// </remarks>
[ServerOpcode(ServerOpcode.DisplayUser)]
public sealed record DisplayUserPacket : ServerPacket
{
    /// <summary>The map X coordinate the player is standing on.</summary>
    public required ushort X { get; init; }

    /// <summary>The map Y coordinate the player is standing on.</summary>
    public required ushort Y { get; init; }

    /// <summary>The direction the player is facing.</summary>
    public required Direction Direction { get; init; }

    /// <summary>The player's serial.</summary>
    public required uint Id { get; init; }

    /// <summary>
    ///     The discriminated appearance block - a full <see cref="EquipmentAppearance" /> or a
    ///     <see cref="CreatureSpriteAppearance" /> override. Never null.
    /// </summary>
    public required DisplayUserAppearance Appearance { get; init; }

    /// <summary>
    ///     The name-tag display style (e.g. greyed/normal/hostile coloring). A plain byte mapped to a
    ///     name-plate color/format.
    /// </summary>
    public required byte NameTagStyle { get; init; }

    /// <summary>The player's name (<c>string8</c>).</summary>
    public required string Name { get; init; }

    /// <summary>The player's group-box text, empty when not grouped (<c>string8</c>).</summary>
    public string GroupName { get; init; } = string.Empty;

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.DisplayUser;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteUInt16(X);
        writer.WriteUInt16(Y);
        writer.WriteByte((byte)Direction);
        writer.WriteUInt32(Id);

        Appearance.Write(writer);

        writer.WriteByte(NameTagStyle);
        writer.WriteString8(Name);
        writer.WriteString8(GroupName);
    }

    /// <inheritdoc />
    public static DisplayUserPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var x = reader.ReadUInt16();
        var y = reader.ReadUInt16();
        var direction = (Direction)reader.ReadByte();
        var id = reader.ReadUInt32();

        var appearance = DisplayUserAppearance.Parse(ref reader);

        var nameTagStyle = reader.ReadByte();
        var name = reader.ReadString8();
        var groupName = reader.ReadString8();

        return new DisplayUserPacket
        {
            X = x,
            Y = y,
            Direction = direction,
            Id = id,
            Appearance = appearance,
            NameTagStyle = nameTagStyle,
            Name = name,
            GroupName = groupName,
        };
    }
}

/// <summary>
///     The discriminated appearance block of a <see cref="DisplayUserPacket" /> (0x33). Opens on the
///     wire with a <c>u16</c> discriminator: <see cref="DisplayUserPacket" />'s
///     <c>0xFFFF</c> sentinel selects <see cref="CreatureSpriteAppearance" />; any other value is the
///     <see cref="EquipmentAppearance.HeadSprite" /> of a full equipment block.
/// </summary>
public abstract record DisplayUserAppearance
{
    /// <summary>The discriminator value marking a creature-sprite override rather than equipment.</summary>
    public const ushort CreatureSpriteSentinel = 0xFFFF;

    /// <summary>Writes this appearance block (including its leading discriminator) to the wire.</summary>
    internal abstract void Write(IPacketWriter writer);

    /// <summary>Reads the discriminator and dispatches to the matching appearance form.</summary>
    internal static DisplayUserAppearance Parse(ref PacketReader reader)
    {
        var discriminator = reader.ReadUInt16();

        return discriminator == CreatureSpriteSentinel
            ? CreatureSpriteAppearance.ParseBody(ref reader)
            : EquipmentAppearance.ParseBody(ref reader, headSprite: discriminator);
    }
}

/// <summary>
///     The creature-sprite appearance form of a <see cref="DisplayUserPacket" /> - a single sprite
///     override used when a player is polymorphed / displayed as a monster. Wire shape (after the
///     <c>0xFFFF</c> discriminator): <c>[u16 Sprite][u8 HeadColor][u8 BootsColor][6 reserved bytes]</c>.
/// </summary>
/// <remarks>
///     The six trailing bytes are reserved (zero). Emitted as six zero bytes and read-and-discarded here.
/// </remarks>
public sealed record CreatureSpriteAppearance : DisplayUserAppearance
{
    /// <summary>The creature sprite to render in place of the player's body.</summary>
    public required ushort Sprite { get; init; }

    /// <summary>The hair/head color overlay.</summary>
    public required byte HeadColor { get; init; }

    /// <summary>The boots color overlay.</summary>
    public required byte BootsColor { get; init; }

    /// <inheritdoc />
    internal override void Write(IPacketWriter writer)
    {
        writer.WriteUInt16(CreatureSpriteSentinel);
        writer.WriteUInt16(Sprite);
        writer.WriteByte(HeadColor);
        writer.WriteByte(BootsColor);
        writer.WriteBytes(stackalloc byte[6]);
    }

    internal static CreatureSpriteAppearance ParseBody(ref PacketReader reader)
    {
        var sprite = reader.ReadUInt16();
        var headColor = reader.ReadByte();
        var bootsColor = reader.ReadByte();
        reader.ReadBytes(6);

        return new CreatureSpriteAppearance
        {
            Sprite = sprite,
            HeadColor = headColor,
            BootsColor = bootsColor,
        };
    }
}

/// <summary>
///     The full-equipment appearance form of a <see cref="DisplayUserPacket" /> - a normally-dressed
///     player. The leading <c>u16</c> discriminator <em>is</em> <see cref="HeadSprite" /> (any value
///     other than <c>0xFFFF</c>).
/// </summary>
public sealed record EquipmentAppearance : DisplayUserAppearance
{
    /// <summary>Head/helmet sprite; also the appearance discriminator. Must not be <c>0xFFFF</c>.</summary>
    public required ushort HeadSprite { get; init; }

    /// <summary>
    ///     Body sprite byte: high nibble selects body form (human/spirit/mounted/doll/invisible),
    ///     low nibble selects the body+pants base.
    /// </summary>
    public required byte BodySprite { get; init; }

    /// <summary>
    ///     Primary armor sprite. Rendered as one of two depth-distinct body-armor passes (paperdoll
    ///     layer 7); suppressed when <see cref="OvercoatSprite" /> is non-zero. See
    ///     <see cref="ArmorSprite2" />.
    /// </summary>
    public required ushort ArmorSprite1 { get; init; }

    /// <summary>Boots sprite.</summary>
    public required byte BootsSprite { get; init; }

    /// <summary>
    ///     Secondary armor sprite. Despite the name it is <em>not</em> a duplicate: it is a separate
    ///     paperdoll pass (layer 5) from <see cref="ArmorSprite1" />'s layer 7, also suppressed when
    ///     <see cref="OvercoatSprite" /> is worn. Typically emitted with the same value as
    ///     <see cref="ArmorSprite1" /> so the two passes form one coherent garment; different values
    ///     would split the body-armor into two sprites.
    /// </summary>
    public required ushort ArmorSprite2 { get; init; }

    /// <summary>Shield sprite.</summary>
    public required byte ShieldSprite { get; init; }

    /// <summary>Weapon sprite.</summary>
    public required ushort WeaponSprite { get; init; }

    /// <summary>Hair/head color.</summary>
    public required byte HeadColor { get; init; }

    /// <summary>Boots color.</summary>
    public required byte BootsColor { get; init; }

    /// <summary>First accessory color.</summary>
    public required byte AccessoryColor1 { get; init; }

    /// <summary>First accessory sprite.</summary>
    public required ushort AccessorySprite1 { get; init; }

    /// <summary>Second accessory color.</summary>
    public required byte AccessoryColor2 { get; init; }

    /// <summary>Second accessory sprite.</summary>
    public required ushort AccessorySprite2 { get; init; }

    /// <summary>Third accessory color.</summary>
    public required byte AccessoryColor3 { get; init; }

    /// <summary>Third accessory sprite.</summary>
    public required ushort AccessorySprite3 { get; init; }

    /// <summary>Lantern light radius.</summary>
    public required byte LanternSize { get; init; }

    /// <summary>Rest/sitting position.</summary>
    public required byte RestPosition { get; init; }

    /// <summary>
    ///     Overcoat sprite. Rendered as paperdoll layers 0x12/0x13; when non-zero it
    ///     <strong>suppresses both armor layers</strong> (<see cref="ArmorSprite1" /> and
    ///     <see cref="ArmorSprite2" />), covering the body.
    /// </summary>
    public required ushort OvercoatSprite { get; init; }

    /// <summary>Overcoat color.</summary>
    public required byte OvercoatColor { get; init; }

    /// <summary>Body/skin color.</summary>
    public required byte BodyColor { get; init; }

    /// <summary>Whether the player is hidden/invisible.</summary>
    public required bool IsHidden { get; init; }

    /// <summary>Face sprite.</summary>
    public required byte FaceSprite { get; init; }

    /// <inheritdoc />
    internal override void Write(IPacketWriter writer)
    {
        writer.WriteUInt16(HeadSprite);
        writer.WriteByte(BodySprite);
        writer.WriteUInt16(ArmorSprite1);
        writer.WriteByte(BootsSprite);
        writer.WriteUInt16(ArmorSprite2);
        writer.WriteByte(ShieldSprite);
        writer.WriteUInt16(WeaponSprite);
        writer.WriteByte(HeadColor);
        writer.WriteByte(BootsColor);
        writer.WriteByte(AccessoryColor1);
        writer.WriteUInt16(AccessorySprite1);
        writer.WriteByte(AccessoryColor2);
        writer.WriteUInt16(AccessorySprite2);
        writer.WriteByte(AccessoryColor3);
        writer.WriteUInt16(AccessorySprite3);
        writer.WriteByte(LanternSize);
        writer.WriteByte(RestPosition);
        writer.WriteUInt16(OvercoatSprite);
        writer.WriteByte(OvercoatColor);
        writer.WriteByte(BodyColor);
        writer.WriteBoolean(IsHidden);
        writer.WriteByte(FaceSprite);
    }

    internal static EquipmentAppearance ParseBody(ref PacketReader reader, ushort headSprite)
    {
        var bodySprite = reader.ReadByte();
        var armorSprite1 = reader.ReadUInt16();
        var bootsSprite = reader.ReadByte();
        var armorSprite2 = reader.ReadUInt16();
        var shieldSprite = reader.ReadByte();
        var weaponSprite = reader.ReadUInt16();
        var headColor = reader.ReadByte();
        var bootsColor = reader.ReadByte();
        var accessoryColor1 = reader.ReadByte();
        var accessorySprite1 = reader.ReadUInt16();
        var accessoryColor2 = reader.ReadByte();
        var accessorySprite2 = reader.ReadUInt16();
        var accessoryColor3 = reader.ReadByte();
        var accessorySprite3 = reader.ReadUInt16();
        var lanternSize = reader.ReadByte();
        var restPosition = reader.ReadByte();
        var overcoatSprite = reader.ReadUInt16();
        var overcoatColor = reader.ReadByte();
        var bodyColor = reader.ReadByte();
        var isHidden = reader.ReadBoolean();
        var faceSprite = reader.ReadByte();

        return new EquipmentAppearance
        {
            HeadSprite = headSprite,
            BodySprite = bodySprite,
            ArmorSprite1 = armorSprite1,
            BootsSprite = bootsSprite,
            ArmorSprite2 = armorSprite2,
            ShieldSprite = shieldSprite,
            WeaponSprite = weaponSprite,
            HeadColor = headColor,
            BootsColor = bootsColor,
            AccessoryColor1 = accessoryColor1,
            AccessorySprite1 = accessorySprite1,
            AccessoryColor2 = accessoryColor2,
            AccessorySprite2 = accessorySprite2,
            AccessoryColor3 = accessoryColor3,
            AccessorySprite3 = accessorySprite3,
            LanternSize = lanternSize,
            RestPosition = restPosition,
            OvercoatSprite = overcoatSprite,
            OvercoatColor = overcoatColor,
            BodyColor = bodyColor,
            IsHidden = isHidden,
            FaceSprite = faceSprite,
        };
    }
}
