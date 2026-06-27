using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x0F (S->C) - place an item in an inventory-pane slot. The canonical body is
///     <c>[u8 Slot][u16 Sprite][u8 Color][string8 Name][u32 Count][u8 Stackable][u32 MaxDurability]
///     [u32 CurrentDurability]</c> (18 + nameLen bytes); optional trailing bytes are tolerated and
///     round-tripped via <see cref="TrailingSlack" />. <see cref="Sprite" /> is the panel sprite
///     offset by <c>0x8000</c>, the same item-sprite range convention as ground objects (see
///     <see cref="ItemWorldObject" />). The inverse operation is 0x10 <see cref="RemoveItemPacket" />.
/// </summary>
[ServerOpcode(ServerOpcode.AddItem)]
public sealed record AddItemPacket : ServerPacket
{
    /// <summary>The inventory-pane slot to place the item in.</summary>
    public required byte Slot { get; init; }

    /// <summary>The item's panel sprite, offset by 0x8000.</summary>
    public required ushort Sprite { get; init; }

    /// <summary>Palette / dye color index.</summary>
    public required byte Color { get; init; }

    /// <summary>The item's display name.</summary>
    public required string Name { get; init; }

    /// <summary>The stack count displayed for the slot.</summary>
    public required uint Count { get; init; }

    /// <summary>Whether the item is stackable.</summary>
    public required bool Stackable { get; init; }

    /// <summary>The item's maximum durability.</summary>
    public required uint MaxDurability { get; init; }

    /// <summary>The item's current durability.</summary>
    public required uint CurrentDurability { get; init; }

    /// <summary>
    ///     Optional trailing bytes beyond the canonical body, kept for byte-faithful round-trips.
    ///     <see langword="null" /> (the default) writes nothing. Settable round-trip field.
    /// </summary>
    public byte[]? TrailingSlack { get; set; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.AddItem;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(Slot);
        writer.WriteUInt16(Sprite);
        writer.WriteByte(Color);
        writer.WriteString8(Name);
        writer.WriteUInt32(Count);
        writer.WriteBoolean(Stackable);
        writer.WriteUInt32(MaxDurability);
        writer.WriteUInt32(CurrentDurability);

        if (TrailingSlack is { Length: > 0 })
            writer.WriteBytes(TrailingSlack);
    }

    /// <inheritdoc />
    public static AddItemPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var slot = reader.ReadByte();
        var sprite = reader.ReadUInt16();
        var color = reader.ReadByte();
        var name = reader.ReadString8();
        var count = reader.ReadUInt32();
        var stackable = reader.ReadBoolean();
        var maxDurability = reader.ReadUInt32();
        var currentDurability = reader.ReadUInt32();

        var trailingSlack = reader.Remaining.Length > 0 ? reader.Remaining.ToArray() : null;

        return new AddItemPacket
        {
            Slot = slot,
            Sprite = sprite,
            Color = color,
            Name = name,
            Count = count,
            Stackable = stackable,
            MaxDurability = maxDurability,
            CurrentDurability = currentDurability,
            TrailingSlack = trailingSlack,
        };
    }
}
