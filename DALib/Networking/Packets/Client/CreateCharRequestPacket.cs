using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x02 (C->S) - the first half of character creation: submits a desired name, password, and
///     email to reserve them. The login server validates the name/password and, on success, stashes
///     them against the connection so the follow-up <see cref="CreateCharFinalizePacket" /> (0x04)
///     can complete the character. There is no correlation token on the wire; the pair is tied
///     together by the connection. Wire body: <c>[string8 Name][string8 Password][string8 Email]</c>.
/// </summary>
/// <remarks>
///     All three fields are present on the wire as string8s. The email value may be ignored by the
///     server but is still transmitted, so it is modeled faithfully. An empty string is valid and
///     common.
/// </remarks>
[ClientOpcode(ClientOpcode.CreateCharRequest)]
public sealed record CreateCharRequestPacket : ClientPacket
{
    /// <summary>The desired character name (the server enforces 4-12 letters).</summary>
    public required string Name { get; init; }

    /// <summary>Plaintext password (the wire format is plaintext; the server hashes server-side).</summary>
    public required string Password { get; init; }

    /// <summary>Account email. Carried on the wire but may be unused by the server; may be empty.</summary>
    public required string Email { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.CreateCharRequest;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteString8(Name);
        writer.WriteString8(Password);
        writer.WriteString8(Email);
    }

    /// <inheritdoc />
    public static CreateCharRequestPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        return new CreateCharRequestPacket
        {
            Name = reader.ReadString8(),
            Password = reader.ReadString8(),
            Email = reader.ReadString8(),
        };
    }
}
