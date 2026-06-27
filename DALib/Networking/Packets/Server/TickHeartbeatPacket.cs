using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x68 (S->C) - the server's tick-heartbeat challenge. The body is <c>[u32 BE ServerTick]</c>; the
///     reply echoes it back (alongside a local tick) via C->S 0x75
///     (<see cref="DALib.Networking.Packets.Client.TickHeartbeatPacket" />). Ticks are non-negative, so
///     the value is modeled as unsigned.
/// </summary>
[ServerOpcode(ServerOpcode.TickHeartbeat)]
public sealed record TickHeartbeatPacket : ServerPacket
{
    /// <summary>The server's tick value; echoed back as the server tick in the C->S 0x75 reply.</summary>
    public required uint ServerTick { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.TickHeartbeat;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => writer.WriteUInt32(ServerTick);

    /// <inheritdoc />
    public static TickHeartbeatPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var serverTick = reader.ReadUInt32();

        return new TickHeartbeatPacket
        {
            ServerTick = serverTick,
        };
    }
}
