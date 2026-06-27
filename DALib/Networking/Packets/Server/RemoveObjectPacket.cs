using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x0E (S->C) - remove an object from the visible map by serial. The body is
///     <c>[u32 BE SourceId]</c>; the identified object (creature, user, or ground item) is erased
///     from view.
/// </summary>
/// <remarks>
///     The inverse operations are 0x07 DrawObjects (creatures/items entering view) and 0x33
///     DisplayUser (users entering view).
/// </remarks>
[ServerOpcode(ServerOpcode.RemoveObject)]
public sealed record RemoveObjectPacket : ServerPacket
{
    /// <summary>The serial of the object to remove from view.</summary>
    public required uint SourceId { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.RemoveObject;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => writer.WriteUInt32(SourceId);

    /// <inheritdoc />
    public static RemoveObjectPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var sourceId = reader.ReadUInt32();

        return new RemoveObjectPacket
        {
            SourceId = sourceId,
        };
    }
}
