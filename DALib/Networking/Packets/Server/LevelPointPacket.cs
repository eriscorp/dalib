using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x3D (S->C) - a two-byte level/point indicator. The body is <c>[u8][u8]</c>.
/// </summary>
/// <remarks>
///     Modeled for protocol completeness; not emitted by typical servers. Drives an "unspent
///     level/ability point(s)" indicator; a non-zero <see cref="Second" /> triggers a notification
///     effect. Modeled as raw <see cref="First" /> / <see cref="Second" /> for faithful round-tripping.
/// </remarks>
[ServerOpcode(ServerOpcode.LevelPoint)]
public sealed record LevelPointPacket : ServerPacket
{
    /// <summary>The first body byte. Preserved verbatim.</summary>
    public required byte First { get; init; }

    /// <summary>The second body byte. Preserved verbatim.</summary>
    public required byte Second { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.LevelPoint;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(First);
        writer.WriteByte(Second);
    }

    /// <inheritdoc />
    public static LevelPointPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        return new LevelPointPacket
        {
            First = reader.ReadByte(),
            Second = reader.ReadByte(),
        };
    }
}
