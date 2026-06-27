using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x26 (C-&gt;S) - change an account's password, handled by the login server. Only one copy of
///     the new password is on the wire (it is confirmed by being typed twice before sending).
///     Wire body: <c>[string8 Name][string8 CurrentPassword][string8 NewPassword]</c>. All three are
///     plaintext on the wire; the server verifies the current password and hashes the new one
///     server-side.
/// </summary>
[ClientOpcode(ClientOpcode.ChangePassword)]
public sealed record ChangePasswordPacket : ClientPacket
{
    /// <summary>Account / character name whose password is changing.</summary>
    public required string Name { get; init; }

    /// <summary>The existing password, verified by the server before any change.</summary>
    public required string CurrentPassword { get; init; }

    /// <summary>The desired new password (plaintext; the server hashes it).</summary>
    public required string NewPassword { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.ChangePassword;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteString8(Name);
        writer.WriteString8(CurrentPassword);
        writer.WriteString8(NewPassword);
    }

    /// <inheritdoc />
    public static ChangePasswordPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        return new ChangePasswordPacket
        {
            Name = reader.ReadString8(),
            CurrentPassword = reader.ReadString8(),
            NewPassword = reader.ReadString8(),
        };
    }
}
