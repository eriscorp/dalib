using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x75 (C->S) - reply to the server's tick-heartbeat challenge. Body:
///     <c>[u32 BE ServerTick][u32 BE ClientTick]</c>.
/// </summary>
/// <remarks>
///     Echoes the server's tick value (<see cref="ServerTick" />) and reports the local tick
///     (<see cref="ClientTick" />); the session is disconnected if the echoed server tick does not
///     validate. Ticks are non-negative and modeled as unsigned.
/// </remarks>
[ClientOpcode(ClientOpcode.TickHeartbeat)]
public sealed record TickHeartbeatPacket : ClientPacket
{
    /// <summary>The server's tick value, echoed back from the challenge.</summary>
    public required uint ServerTick { get; init; }

    /// <summary>The client's own local tick value.</summary>
    public required uint ClientTick { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.TickHeartbeat;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteUInt32(ServerTick);
        writer.WriteUInt32(ClientTick);
    }

    /// <inheritdoc />
    public static TickHeartbeatPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var serverTick = reader.ReadUInt32();
        var clientTick = reader.ReadUInt32();

        return new TickHeartbeatPacket
        {
            ServerTick = serverTick,
            ClientTick = clientTick,
        };
    }
}
