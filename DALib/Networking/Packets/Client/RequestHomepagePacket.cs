using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x68 (C->S) - request the homepage/account URL (sent from the main-menu homepage button).
///     The server answers with S->C 0x66 subtype 3
///     (<see cref="DALib.Networking.Packets.Server.SetUrlForm" />). Pure trigger; carries no body.
/// </summary>
[ClientOpcode(ClientOpcode.RequestHomepage)]
public sealed record RequestHomepagePacket : ClientPacket
{
    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.RequestHomepage;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        // No body; the opcode alone requests the homepage URL.
    }

    /// <inheritdoc />
    public static RequestHomepagePacket Parse(ReadOnlySpan<byte> body) => new();
}
