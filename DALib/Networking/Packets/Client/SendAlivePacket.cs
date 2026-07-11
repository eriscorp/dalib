using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x71 (C->S) - a keep-alive "still here" signal. Pure trigger; carries no body. The client sends it
///     on a timer (re-arming a ~30s timer after each send) while its activity flag is set.
/// </summary>
/// <remarks>
///     Binary-verified send; not emitted by Hybrasyl or Chaos. Uses Normal encryption (in the client's
///     C->S encryption table). Modeled for wire completeness.
/// </remarks>
[ClientOpcode(ClientOpcode.SendAlive)]
public sealed record SendAlivePacket : ClientPacket
{
    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.SendAlive;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        // No body; the opcode alone is the keep-alive.
    }

    /// <inheritdoc />
    public static SendAlivePacket Parse(ReadOnlySpan<byte> body) => new();
}
