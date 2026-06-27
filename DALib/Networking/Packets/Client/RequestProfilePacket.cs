using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x2D (C->S) - request the player's own profile pane (the player-info / group-management
///     display). Pure trigger; carries no body. The server replies with the player's self-profile.
/// </summary>
/// <remarks>
///     Distinct from <em>setting</em> the profile, which is a separate opcode.
/// </remarks>
[ClientOpcode(ClientOpcode.RequestProfile)]
public sealed record RequestProfilePacket : ClientPacket
{
    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.RequestProfile;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        // No body; the opcode alone requests the profile pane.
    }

    /// <inheritdoc />
    public static RequestProfilePacket Parse(ReadOnlySpan<byte> body) => new();
}
