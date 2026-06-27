using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x29 (C->S) - drop/give a count of items from an inventory slot onto a target creature or
///     player. Body: <c>[u8 Slot][u32 BE TargetId][u8 Count]</c>. The target gives the item to a
///     creature/NPC directly, or initiates an exchange when it is another player. Unlike
///     <see cref="DropItemPacket" /> (0x08), <see cref="Count" /> here is a single byte.
/// </summary>
[ClientOpcode(ClientOpcode.DropItemOnCreature)]
public sealed record DropItemOnCreaturePacket : ClientPacket
{
    /// <summary>Inventory slot to drop from (1-based; 0 is invalid).</summary>
    public required byte Slot { get; init; }

    /// <summary>World object id of the creature/player to drop onto.</summary>
    public required uint TargetId { get; init; }

    /// <summary>Number of items to drop from the stack.</summary>
    public required byte Count { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.DropItemOnCreature;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(Slot);
        writer.WriteUInt32(TargetId);
        writer.WriteByte(Count);
    }

    /// <inheritdoc />
    public static DropItemOnCreaturePacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var slot = reader.ReadByte();
        var targetId = reader.ReadUInt32();
        var count = reader.ReadByte();

        return new DropItemOnCreaturePacket
        {
            Slot = slot,
            TargetId = targetId,
            Count = count,
        };
    }
}
