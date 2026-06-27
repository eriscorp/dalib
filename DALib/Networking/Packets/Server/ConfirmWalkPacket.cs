using System;
using DALib.Definitions;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x0B (S->C) - confirms the player's own one-tile walk, answering a C->S 0x06 Walk request. The
///     body is <c>[u8 Direction][u16 OldX][u16 OldY][u16 11][u16 11][u8 1]</c>; the step is committed
///     (scrolling the viewport) on receipt.
/// </summary>
/// <remarks>
///     The coordinates are the tile the player walked <em>from</em>; the destination is implied by
///     <see cref="Direction" />. The trailing <c>[u16 11][u16 11][u8 1]</c> is an unread constant
///     (likely an 11x11-tile display window); modeled as settable round-trip fields with those defaults
///     so re-emit stays byte-faithful.
/// </remarks>
[ServerOpcode(ServerOpcode.ConfirmWalk)]
public sealed record ConfirmWalkPacket : ServerPacket
{
    /// <summary>The direction the player stepped toward.</summary>
    public required Direction Direction { get; init; }

    /// <summary>The X coordinate of the tile the player walked from.</summary>
    public required ushort OldX { get; init; }

    /// <summary>The Y coordinate of the tile the player walked from.</summary>
    public required ushort OldY { get; init; }

    /// <summary>
    ///     Unknown-purpose constant, emitted as 11 (possibly the viewport width in tiles - unverified).
    ///     Settable round-trip field.
    /// </summary>
    public ushort Unknown1 { get; set; } = 11;

    /// <summary>
    ///     Unknown-purpose constant, emitted as 11 (possibly the viewport height in tiles - unverified).
    ///     Settable round-trip field.
    /// </summary>
    public ushort Unknown2 { get; set; } = 11;

    /// <summary>Unknown-purpose constant, emitted as 1. Settable round-trip field.</summary>
    public byte Unknown3 { get; set; } = 1;

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.ConfirmWalk;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte((byte)Direction);
        writer.WriteUInt16(OldX);
        writer.WriteUInt16(OldY);
        writer.WriteUInt16(Unknown1);
        writer.WriteUInt16(Unknown2);
        writer.WriteByte(Unknown3);
    }

    /// <inheritdoc />
    public static ConfirmWalkPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var direction = (Direction)reader.ReadByte();
        var oldX = reader.ReadUInt16();
        var oldY = reader.ReadUInt16();
        var unknown1 = reader.ReadUInt16();
        var unknown2 = reader.ReadUInt16();
        var unknown3 = reader.ReadByte();

        return new ConfirmWalkPacket
        {
            Direction = direction,
            OldX = oldX,
            OldY = oldY,
            Unknown1 = unknown1,
            Unknown2 = unknown2,
            Unknown3 = unknown3,
        };
    }
}
