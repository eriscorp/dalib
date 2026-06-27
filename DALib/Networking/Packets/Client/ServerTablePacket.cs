using System;
using System.IO;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x57 (C->S) - two-purpose lobby packet: <see cref="ServerTableRequestPacket" /> asks
///     for the full server table; <see cref="ServerTableSelectPacket" /> commits a chosen
///     server. Dispatched by the leading mismatch-flag byte.
/// </summary>
[ClientOpcode(ClientOpcode.ServerTable)]
public abstract record ServerTablePacket : ClientPacket
{
    /// <summary>The flag value carried on the wire to request the full table.</summary>
    public const byte MismatchRequestTable = 0x01;

    /// <summary>The flag value carried on the wire to commit a server selection.</summary>
    public const byte MismatchSelectServer = 0x00;

    /// <summary>
    ///     Trailing padding byte emitted at the end of both variants' bodies; always <c>0x00</c>.
    ///     Override only for protocol probing.
    /// </summary>
    public byte Padding { get; init; } = 0x00;

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.ServerTable;

    /// <summary>
    ///     Parses a 0x57 body, dispatching on the mismatch flag to the appropriate concrete
    ///     variant.
    /// </summary>
    public static ServerTablePacket Parse(ReadOnlySpan<byte> body)
    {
        if (body.Length < 1)
            throw new InvalidDataException(
                "0x57 ServerTable: body is empty, no mismatch flag to read.");

        return body[0] switch
        {
            MismatchRequestTable => ServerTableRequestPacket.ParseAfterFlag(body[1..]),
            MismatchSelectServer => ServerTableSelectPacket.ParseAfterFlag(body[1..]),
            _ => throw new InvalidDataException(
                $"0x57 ServerTable: unknown mismatch flag 0x{body[0]:X2} " +
                $"(expected 0x{MismatchRequestTable:X2} or 0x{MismatchSelectServer:X2}).")
        };
    }
}

/// <summary>
///     Requests the full server table from the lobby. Expects
///     <see cref="DALib.Networking.Packets.Server.ServerTableDataPacket" /> (S->C 0x56) in
///     reply.
/// </summary>
public sealed record ServerTableRequestPacket : ServerTablePacket
{
    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(MismatchRequestTable);
        writer.WriteByte(Padding);
    }

    internal static ServerTableRequestPacket ParseAfterFlag(ReadOnlySpan<byte> rest)
    {
        var reader = new PacketReader(rest);
        var padding = reader.ReadByte();

        return new ServerTableRequestPacket { Padding = padding };
    }
}

/// <summary>
///     Commits a server selection by id. Expects S->C 0x03 Redirect in reply.
/// </summary>
public sealed record ServerTableSelectPacket : ServerTablePacket
{
    /// <summary>
    ///     The chosen server's <see cref="DALib.Networking.Packets.Server.ServerEntry.Id" /> -
    ///     <em>not</em> the index of the entry in the received table.
    /// </summary>
    public required byte ServerId { get; init; }

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(MismatchSelectServer);
        writer.WriteByte(ServerId);
        writer.WriteByte(Padding);
    }

    internal static ServerTableSelectPacket ParseAfterFlag(ReadOnlySpan<byte> rest)
    {
        var reader = new PacketReader(rest);
        var serverId = reader.ReadByte();
        var padding = reader.ReadByte();

        return new ServerTableSelectPacket
        {
            ServerId = serverId,
            Padding = padding,
        };
    }
}
