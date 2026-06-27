using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x4C (S->C) - the answer to an exit request (C->S 0x0B ExitRequest with <c>IsRequest = true</c>):
///     a single <c>[u8 ExitConfirmed]</c> byte indicating whether the session may proceed to leave the
///     world. On confirmation the exit handshake advances (0x0B with <c>IsRequest = false</c>), followed
///     by a 0x03 Redirect back to the login server.
/// </summary>
/// <remarks>
///     Only the first body byte is read; any trailing zero bytes some emitters append are unread slack,
///     so this models the single significant byte.
/// </remarks>
[ServerOpcode(ServerOpcode.ConfirmExit)]
public sealed record ConfirmExitPacket : ServerPacket
{
    /// <summary>Whether the session is confirmed to exit the world.</summary>
    public required bool ExitConfirmed { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.ConfirmExit;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => writer.WriteBoolean(ExitConfirmed);

    /// <inheritdoc />
    public static ConfirmExitPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var exitConfirmed = reader.ReadBoolean();

        return new ConfirmExitPacket
        {
            ExitConfirmed = exitConfirmed,
        };
    }
}
