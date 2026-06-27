using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x2F (C->S) - toggles the player's "accepting group invitations" flag. The packet is a pure
///     trigger and carries no body.
/// </summary>
[ClientOpcode(ClientOpcode.GroupToggle)]
public sealed record GroupTogglePacket : ClientPacket
{
    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.GroupToggle;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        // No body - the opcode alone toggles the grouping flag.
    }

    /// <inheritdoc />
    public static GroupTogglePacket Parse(ReadOnlySpan<byte> body) => new();
}
