using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x4B (C->S) - request the full login notice. Sent on the login screen after a S->C 0x60 checksum
///     (<c>NotificationChecksumForm</c>) does not match the cached copy; the server answers with the
///     S->C 0x60 full payload (<see cref="DALib.Networking.Packets.Server.NotificationDataForm" />).
///     Pure trigger; carries no body.
/// </summary>
[ClientOpcode(ClientOpcode.RequestNotification)]
public sealed record RequestNotificationPacket : ClientPacket
{
    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.RequestNotification;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        // No body; the opcode alone requests the full notice.
    }

    /// <inheritdoc />
    public static RequestNotificationPacket Parse(ReadOnlySpan<byte> body) => new();
}
