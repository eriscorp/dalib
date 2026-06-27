using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x47 (S->C) - an online-user count. The body is a single <c>[u16]</c> (big-endian). Modeled for
///     protocol completeness; not emitted by typical servers.
/// </summary>
[ServerOpcode(ServerOpcode.TotalUsers)]
public sealed record TotalUsersPacket : ServerPacket
{
    /// <summary>The online-user count (big-endian <c>u16</c>).</summary>
    public required ushort Count { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.TotalUsers;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => writer.WriteUInt16(Count);

    /// <inheritdoc />
    public static TotalUsersPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        return new TotalUsersPacket
        {
            Count = reader.ReadUInt16(),
        };
    }
}
