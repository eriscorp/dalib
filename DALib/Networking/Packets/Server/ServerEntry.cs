using System.Net;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     One entry in <see cref="ServerTableDataPacket" /> - connection info for a single
///     advertised game server.
/// </summary>
/// <remarks>
///     The local <c>mServer.tbl</c> cache stores an additional description field per entry that
///     is never delivered on the wire and is not modeled here.
/// </remarks>
public sealed record ServerEntry
{
    /// <summary>
    ///     Server identifier echoed back in
    ///     <see cref="DALib.Networking.Packets.Client.ServerTableSelectPacket" />. This is
    ///     <em>not</em> the entry's index in <see cref="ServerTableDataPacket.Servers" /> -
    ///     always select by <see cref="Id" />, since multi-server lobbies allocate
    ///     non-sequential ids.
    /// </summary>
    public required byte Id { get; init; }

    /// <summary>IPv4 address. Sent on the wire as four network-order octets.</summary>
    public required IPAddress IpAddress { get; init; }

    /// <summary>TCP port (big-endian on the wire).</summary>
    public required ushort Port { get; init; }

    /// <summary>Display name (Latin-1, null-terminated on the wire).</summary>
    public required string Name { get; init; }
}
