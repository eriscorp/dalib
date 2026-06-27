using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x37 (S->C) - place an item in an equipment-pane slot. Body:
///     <c>[u8 Slot][u16 Sprite][u8 Color][string8 Name][u8 Unknown1][u32 MaxDurability]
///     [u32 CurrentDurability]</c> (14 + nameLen bytes), fully consumed. <see cref="Slot" /> values
///     1-18 are valid; 0 and values >= 19 are ignored. <see cref="Sprite" /> carries the 0x8000
///     panel-sprite offset, as in 0x0F <see cref="AddItemPacket" />. The inverse operation is 0x38
///     <see cref="RemoveEquipmentPacket" />.
/// </summary>
[ServerOpcode(ServerOpcode.AddEquipment)]
public sealed record AddEquipmentPacket : ServerPacket
{
    /// <summary>The equipment-pane slot (1-18; values 0 and >= 19 are ignored).</summary>
    public required EquipmentSlot Slot { get; init; }

    /// <summary>The item's panel sprite, offset by 0x8000.</summary>
    public required ushort Sprite { get; init; }

    /// <summary>Palette / dye color index.</summary>
    public required byte Color { get; init; }

    /// <summary>The item's display name.</summary>
    public required string Name { get; init; }

    /// <summary>
    ///     Reserved byte between the name and the durability pair; conventionally 0. Settable
    ///     round-trip field.
    /// </summary>
    public byte Unknown1 { get; set; }

    /// <summary>The item's maximum durability.</summary>
    public required uint MaxDurability { get; init; }

    /// <summary>The item's current durability.</summary>
    public required uint CurrentDurability { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.AddEquipment;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte((byte)Slot);
        writer.WriteUInt16(Sprite);
        writer.WriteByte(Color);
        writer.WriteString8(Name);
        writer.WriteByte(Unknown1);
        writer.WriteUInt32(MaxDurability);
        writer.WriteUInt32(CurrentDurability);
    }

    /// <inheritdoc />
    public static AddEquipmentPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var slot = (EquipmentSlot)reader.ReadByte();
        var sprite = reader.ReadUInt16();
        var color = reader.ReadByte();
        var name = reader.ReadString8();
        var unknown1 = reader.ReadByte();
        var maxDurability = reader.ReadUInt32();
        var currentDurability = reader.ReadUInt32();

        return new AddEquipmentPacket
        {
            Slot = slot,
            Sprite = sprite,
            Color = color,
            Name = name,
            Unknown1 = unknown1,
            MaxDurability = maxDurability,
            CurrentDurability = currentDurability,
        };
    }
}
