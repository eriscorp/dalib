using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x07 (C->S) - pick up an item or gold stack from a tile, placing it in the given inventory
///     slot. Body: <c>[u8 slot][u16 BE x][u16 BE y]</c>. Tile coordinates are unsigned tile indices.
/// </summary>
[ClientOpcode(ClientOpcode.PickupItem)]
public sealed record PickupItemPacket : ClientPacket
{
    /// <summary>Destination inventory slot to receive the picked-up item (1-based; 0 is invalid).</summary>
    public required byte Slot { get; init; }

    /// <summary>X of the tile to pick up from.</summary>
    public required ushort X { get; init; }

    /// <summary>Y of the tile to pick up from.</summary>
    public required ushort Y { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.PickupItem;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(Slot);
        writer.WriteUInt16(X);
        writer.WriteUInt16(Y);
    }

    /// <inheritdoc />
    public static PickupItemPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var slot = reader.ReadByte();
        var x = reader.ReadUInt16();
        var y = reader.ReadUInt16();

        return new PickupItemPacket
        {
            Slot = slot,
            X = x,
            Y = y,
        };
    }
}
