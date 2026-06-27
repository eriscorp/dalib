using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x3C (S->C) - one row of raw map tile data, sent once per map row in answer to C->S 0x05
///     RequestMap. The body is <c>[u16 RowIndex][raw row bytes]</c>; the rows are streamed into the
///     map buffer as they arrive.
/// </summary>
/// <remarks>
///     The body is <c>[u16 RowIndex]</c> followed by the raw row bytes with no length prefix - the row
///     data runs to the end of the packet. Each tile cell is six bytes (three big-endian <c>u16</c>
///     layers). The row contents are opaque tile indices; this packet models the transport, not the
///     tile encoding.
/// </remarks>
[ServerOpcode(ServerOpcode.MapData)]
public sealed record MapDataPacket : ServerPacket
{
    /// <summary>The zero-based row (Y) index this payload fills.</summary>
    public required ushort RowIndex { get; init; }

    /// <summary>The raw tile bytes for the row (no length prefix; the row runs to end of packet).</summary>
    public required byte[] RowData { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.MapData;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteUInt16(RowIndex);
        writer.WriteBytes(RowData);
    }

    /// <inheritdoc />
    public static MapDataPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var rowIndex = reader.ReadUInt16();
        var rowData = reader.Remaining.ToArray();

        return new MapDataPacket
        {
            RowIndex = rowIndex,
            RowData = rowData,
        };
    }
}
