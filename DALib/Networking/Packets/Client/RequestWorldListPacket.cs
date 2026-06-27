using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x18 (C->S) - request the list of online players (the in-game world/user list window).
///     Pure trigger; carries no body. The server replies with the S->C 0x36 world-list payload.
/// </summary>
[ClientOpcode(ClientOpcode.RequestWorldList)]
public sealed record RequestWorldListPacket : ClientPacket
{
    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.RequestWorldList;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        // No body; the opcode alone requests the world list.
    }

    /// <inheritdoc />
    public static RequestWorldListPacket Parse(ReadOnlySpan<byte> body) => new();
}
