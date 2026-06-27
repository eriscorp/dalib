using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x2A (C->S) - drop/give an amount of gold onto a target creature or player. The body is
///     <c>[u32 BE Amount][u32 BE TargetId]</c>.
/// </summary>
/// <remarks>
///     <see cref="TargetId" /> is the world object id of the drop target. The server gives the gold
///     to a creature/NPC directly, or initiates an exchange when the target is another player.
/// </remarks>
[ClientOpcode(ClientOpcode.DropGoldOnCreature)]
public sealed record DropGoldOnCreaturePacket : ClientPacket
{
    /// <summary>Amount of gold to drop.</summary>
    public required uint Amount { get; init; }

    /// <summary>World object id of the creature/player to drop onto.</summary>
    public required uint TargetId { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.DropGoldOnCreature;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteUInt32(Amount);
        writer.WriteUInt32(TargetId);
    }

    /// <inheritdoc />
    public static DropGoldOnCreaturePacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var amount = reader.ReadUInt32();
        var targetId = reader.ReadUInt32();

        return new DropGoldOnCreaturePacket
        {
            Amount = amount,
            TargetId = targetId,
        };
    }
}
