using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x38 (C->S) - request a refresh of the surrounding area (the in-game "refresh" /
///     spacebar-clear). A pure trigger: the packet carries no body.
/// </summary>
[ClientOpcode(ClientOpcode.Refresh)]
public sealed record RefreshPacket : ClientPacket
{
    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.Refresh;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        // No body - the opcode alone requests a refresh.
    }

    /// <inheritdoc />
    public static RefreshPacket Parse(ReadOnlySpan<byte> body) => new();
}
