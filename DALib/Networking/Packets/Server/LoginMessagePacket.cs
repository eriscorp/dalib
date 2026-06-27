using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x02 (S->C) - response to login-phase actions (character creation, login attempts,
///     password changes). Carries a result code and a message string.
/// </summary>
/// <remarks>
///     On a successful login the server sends <see cref="TypeSuccess" /> with an empty
///     message, then immediately follows with S->C 0x03 <see cref="RedirectPacket" /> to
///     hand the connection off to the world server.
/// </remarks>
[ServerOpcode(ServerOpcode.LoginMessage)]
public sealed record LoginMessagePacket : ServerPacket
{
    /// <summary>Login succeeded. <see cref="Message" /> is conventionally <c>"\0"</c>.</summary>
    public const byte TypeSuccess = 0x00;

    /// <summary>Generic error. <see cref="Message" /> carries the user-facing error text.</summary>
    public const byte TypeError = 0x03;

    /// <summary>Password too short or too long.</summary>
    public const byte TypePasswordLength = 0x05;

    /// <summary>Numeric portion of the password is too short.</summary>
    public const byte TypePasswordNumericTooShort = 0x07;

    /// <summary>Password is too simple.</summary>
    public const byte TypePasswordTooSimple = 0x08;

    /// <summary>Password contains invalid characters.</summary>
    public const byte TypePasswordInvalidChars = 0x09;

    /// <summary>Account / character name does not exist.</summary>
    public const byte TypeNameDoesNotExist = 0x0E;

    /// <summary>Incorrect password.</summary>
    public const byte TypeIncorrectPassword = 0x0F;

    /// <summary>The result code. See the <c>Type*</c> constants for the documented values.</summary>
    public required byte Type { get; init; }

    /// <summary>Response message (Latin-1; conventionally <c>"\0"</c> on success).</summary>
    public required string Message { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.LoginMessage;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteString8(Message);
    }

    /// <inheritdoc />
    public static LoginMessagePacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var type = reader.ReadByte();
        var message = reader.ReadString8();

        return new LoginMessagePacket
        {
            Type = type,
            Message = message,
        };
    }
}
