using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x08 (C->S) - drop a count of items from an inventory slot onto a tile.
///     Body: <c>[u8 slot][u16 BE x][u16 BE y][u32 BE count]</c>. For a stackable item, a
///     <see cref="Count" /> below the held amount splits the stack; otherwise the whole item is
///     dropped. Coordinates are unsigned tile indices.
/// </summary>
[ClientOpcode(ClientOpcode.DropItem)]
public sealed record DropItemPacket : ClientPacket
{
    /// <summary>Inventory slot to drop from (1-based; 0 is invalid).</summary>
    public required byte Slot { get; init; }

    /// <summary>X of the tile to drop onto.</summary>
    public required ushort X { get; init; }

    /// <summary>Y of the tile to drop onto.</summary>
    public required ushort Y { get; init; }

    /// <summary>Number of items to drop from the stack.</summary>
    public required uint Count { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.DropItem;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(Slot);
        writer.WriteUInt16(X);
        writer.WriteUInt16(Y);
        writer.WriteUInt32(Count);
    }

    /// <inheritdoc />
    public static DropItemPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var slot = reader.ReadByte();
        var x = reader.ReadUInt16();
        var y = reader.ReadUInt16();
        var count = reader.ReadUInt32();

        return new DropItemPacket
        {
            Slot = slot,
            X = x,
            Y = y,
            Count = count,
        };
    }
}
