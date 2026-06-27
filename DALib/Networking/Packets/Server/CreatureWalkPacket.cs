using System;
using DALib.Enums;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x0C (S->C) - another creature or user walked one tile within the player's view. Body:
///     <c>[u32 BE SourceId][u16 OldX][u16 OldY][u8 Direction][u8 0]</c>, where the coordinates are
///     the tile the creature walked <em>from</em>. The trailing zero byte is modeled as a settable
///     round-trip field so re-emit stays byte-faithful.
/// </summary>
[ServerOpcode(ServerOpcode.CreatureWalk)]
public sealed record CreatureWalkPacket : ServerPacket
{
    /// <summary>The walking creature's serial.</summary>
    public required uint SourceId { get; init; }

    /// <summary>The X coordinate of the tile the creature walked from.</summary>
    public required ushort OldX { get; init; }

    /// <summary>The Y coordinate of the tile the creature walked from.</summary>
    public required ushort OldY { get; init; }

    /// <summary>The direction the creature stepped toward.</summary>
    public required Direction Direction { get; init; }

    /// <summary>Unknown-purpose trailing byte, typically 0. Settable round-trip field.</summary>
    public byte Unknown { get; set; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.CreatureWalk;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteUInt32(SourceId);
        writer.WriteUInt16(OldX);
        writer.WriteUInt16(OldY);
        writer.WriteByte((byte)Direction);
        writer.WriteByte(Unknown);
    }

    /// <inheritdoc />
    public static CreatureWalkPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var sourceId = reader.ReadUInt32();
        var oldX = reader.ReadUInt16();
        var oldY = reader.ReadUInt16();
        var direction = (Direction)reader.ReadByte();
        var unknown = reader.ReadByte();

        return new CreatureWalkPacket
        {
            SourceId = sourceId,
            OldX = oldX,
            OldY = oldY,
            Direction = direction,
            Unknown = unknown,
        };
    }
}
