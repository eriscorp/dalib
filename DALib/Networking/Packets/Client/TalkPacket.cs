using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     How a <see cref="TalkPacket" /> line is broadcast: a normal line sends <c>0x00</c>, a shout
///     sends <c>0x01</c>. No other value is used on opcode 0x0E; guild and group chat use a
///     different opcode, as does a spell chant.
/// </summary>
public enum ChatType : byte
{
    /// <summary>0 - normal local chat, heard by nearby players.</summary>
    Say = 0,

    /// <summary>1 - a shout, heard map-wide.</summary>
    Shout = 1
}

/// <summary>
///     0x0E (C->S) - send a line of public chat. Carries a <see cref="ChatType" /> discriminator
///     (say vs shout) and the message text. A message beginning with the server's configured command
///     prefix (default <c>/</c>) is treated as a command rather than chat.
/// </summary>
/// <remarks>
///     Wire layout is <c>[u8 chatType][string8 text]</c>. A "say" line sends chatType <c>0</c>, a
///     shout sends chatType <c>1</c>; no other chatType is used on this opcode. Other chat surfaces
///     are separate opcodes, not additional 0x0E types. <see cref="ChatType" /> is a
///     <see cref="byte" />-backed enum, so any unrecognized value still round-trips faithfully. A
///     blank message is valid protocol, so <see cref="Message" /> is not required and defaults to
///     empty.
/// </remarks>
[ClientOpcode(ClientOpcode.Talk)]
public sealed record TalkPacket : ClientPacket
{
    /// <summary>Whether this line is a normal say or a map-wide shout.</summary>
    public ChatType ChatType { get; init; } = ChatType.Say;

    /// <summary>The message text. May be empty (a blank line is valid).</summary>
    public string Message { get; init; } = string.Empty;

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.Talk;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte((byte)ChatType);
        writer.WriteString8(Message);
    }

    /// <inheritdoc />
    public static TalkPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var chatType = (ChatType)reader.ReadByte();
        var message = reader.ReadString8();

        return new TalkPacket
        {
            ChatType = chatType,
            Message = message,
        };
    }
}
