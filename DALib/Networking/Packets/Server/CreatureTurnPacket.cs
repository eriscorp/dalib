using System;
using DALib.Enums;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x11 (S->C) - a creature or user turned in place to face a new direction. Body:
///     <c>[u32 BE SourceId][u8 Direction]</c>. The identified object's facing changes without
///     moving it. This is the S->C counterpart of C->S 0x11 Turn (same opcode, opposite direction).
/// </summary>
[ServerOpcode(ServerOpcode.CreatureTurn)]
public sealed record CreatureTurnPacket : ServerPacket
{
    /// <summary>The turning creature's serial.</summary>
    public required uint SourceId { get; init; }

    /// <summary>The direction the creature now faces.</summary>
    public required Direction Direction { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.CreatureTurn;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteUInt32(SourceId);
        writer.WriteByte((byte)Direction);
    }

    /// <inheritdoc />
    public static CreatureTurnPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var sourceId = reader.ReadUInt32();
        var direction = (Direction)reader.ReadByte();

        return new CreatureTurnPacket
        {
            SourceId = sourceId,
            Direction = direction,
        };
    }
}
