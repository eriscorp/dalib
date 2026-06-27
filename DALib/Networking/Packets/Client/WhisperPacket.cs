using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x19 (C->S) - a directed message to a named target. Body: <c>[string8 target][string8 message]</c>.
///     Used for player-to-player whispers and, via magic target names, for guild and group chat.
/// </summary>
/// <remarks>
///     <see cref="Target" /> is normally a player name, but certain markers are treated as channels
///     rather than recipients: <c>"!"</c> is <strong>guild</strong> chat and <c>"!!"</c> is
///     <strong>group</strong> chat (so guild/group chat are not distinct opcodes - they ride 0x19);
///     <c>"#"</c>, <c>"@"</c>, and <c>"$"</c> are commonly routed as privileged channels. Routing is the
///     server's concern; this packet just carries the two strings verbatim.
///     <para>
///         Opcode 0x19 is direction-split: C->S it is this Whisper, but S->C it is a SoundEffect - a
///         different packet entirely. The separate Client/Server opcode enums keep that distinction
///         explicit.
///     </para>
/// </remarks>
[ClientOpcode(ClientOpcode.Whisper)]
public sealed record WhisperPacket : ClientPacket
{
    /// <summary>
    ///     The message target: a player name, or a channel marker (<c>"!"</c> guild, <c>"!!"</c>
    ///     group, and the privileged <c>"#"</c>/<c>"@"</c>/<c>"$"</c>).
    /// </summary>
    public required string Target { get; init; }

    /// <summary>The message text.</summary>
    public required string Message { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.Whisper;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteString8(Target);
        writer.WriteString8(Message);
    }

    /// <inheritdoc />
    public static WhisperPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var target = reader.ReadString8();
        var message = reader.ReadString8();

        return new WhisperPacket
        {
            Target = target,
            Message = message,
        };
    }
}
