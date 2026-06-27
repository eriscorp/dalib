using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x04 (S->C) - snap the player to a map coordinate (sent on map entry and teleports).
///     Body: <c>[u16 X][u16 Y]</c>. An optional trailing <c>[u16][u16]</c> pair is supported for
///     byte-faithful round-tripping of servers that emit it; it is not part of the canonical body.
/// </summary>
[ServerOpcode(ServerOpcode.Location)]
public sealed record LocationPacket : ServerPacket
{
    /// <summary>The X coordinate to place the player at.</summary>
    public required ushort X { get; init; }

    /// <summary>The Y coordinate to place the player at.</summary>
    public required ushort Y { get; init; }

    /// <summary>
    ///     First value of the optional trailing pair; <see langword="null" /> (omitted) by default.
    ///     Settable for round-tripping.
    /// </summary>
    public ushort? Unknown1 { get; set; }

    /// <summary>
    ///     Second value of the optional trailing pair; <see langword="null" /> (omitted) by default.
    ///     Settable for round-tripping.
    /// </summary>
    public ushort? Unknown2 { get; set; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.Location;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteUInt16(X);
        writer.WriteUInt16(Y);

        if (Unknown1 is { } unknown1)
            writer.WriteUInt16(unknown1);

        if (Unknown2 is { } unknown2)
            writer.WriteUInt16(unknown2);
    }

    /// <inheritdoc />
    public static LocationPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var x = reader.ReadUInt16();
        var y = reader.ReadUInt16();

        ushort? unknown1 = reader.Remaining.Length >= 2 ? reader.ReadUInt16() : null;
        ushort? unknown2 = reader.Remaining.Length >= 2 ? reader.ReadUInt16() : null;

        return new LocationPacket
        {
            X = x,
            Y = y,
            Unknown1 = unknown1,
            Unknown2 = unknown2,
        };
    }
}
