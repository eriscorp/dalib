using System;
using System.Collections.Generic;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x32 (S->C) - updates the open/closed state of one or more door tiles. Body:
///     <c>[u8 Count]</c> followed by that many <c>[u8 X][u8 Y][u8 Closed][u8 OpenRight]</c> records.
/// </summary>
/// <remarks>
///     The leading byte is a genuine door <strong>count</strong>, not a flag: batching multiple
///     doors in one 0x32 packet is supported. The single-door case and a bare <c>[u8 0]</c> (count
///     zero) are just degenerate cases of this same count-prefixed shape. The two booleans are the
///     door's render state: <see cref="Door.Closed" /> (open vs shut) and <see cref="Door.OpenRight" />
///     (which way it is hinged / which leaf is drawn). Coordinates are absolute map tiles, single-byte
///     each.
/// </remarks>
[ServerOpcode(ServerOpcode.Door)]
public sealed record DoorPacket : ServerPacket
{
    /// <summary>The door-tile updates. Wire-emits a u8 count followed by each door record.</summary>
    public IList<Door> Doors { get; set; } = [];

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.Door;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        if (Doors.Count > byte.MaxValue)
            throw new InvalidOperationException(
                $"Door: door count {Doors.Count} exceeds wire u8 limit ({byte.MaxValue}).");

        writer.WriteByte((byte)Doors.Count);

        foreach (var door in Doors)
        {
            writer.WriteByte(door.X);
            writer.WriteByte(door.Y);
            writer.WriteBoolean(door.Closed);
            writer.WriteBoolean(door.OpenRight);
        }
    }

    /// <inheritdoc />
    public static DoorPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var count = reader.ReadByte();
        var doors = new List<Door>(count);

        for (var i = 0; i < count; i++)
        {
            var x = reader.ReadByte();
            var y = reader.ReadByte();
            var closed = reader.ReadBoolean();
            var openRight = reader.ReadBoolean();

            doors.Add(new Door
            {
                X = x,
                Y = y,
                Closed = closed,
                OpenRight = openRight,
            });
        }

        return new DoorPacket { Doors = doors };
    }
}

/// <summary>
///     A single door-tile state update carried by <see cref="DoorPacket" /> (0x32). Wire shape:
///     <c>[u8 X][u8 Y][u8 Closed][u8 OpenRight]</c>.
/// </summary>
public sealed record Door
{
    /// <summary>The door tile's absolute map X coordinate.</summary>
    public required byte X { get; init; }

    /// <summary>The door tile's absolute map Y coordinate.</summary>
    public required byte Y { get; init; }

    /// <summary>Whether the door is shut (true) or open (false).</summary>
    public required bool Closed { get; init; }

    /// <summary>Which way the door is hinged / which leaf is drawn.</summary>
    public required bool OpenRight { get; init; }
}
