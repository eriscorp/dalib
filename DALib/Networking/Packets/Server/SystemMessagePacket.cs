using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x0A (S->C) - a system/UI message: chat-log lines, action-bar text, and the various pop-up
///     windows. The body is <c>[u8 MessageType][string16 Message]</c>. Note the length prefix is a
///     <c>u16</c> (<see cref="IPacketWriter.WriteString16" />), not the <c>u8</c> most text packets use,
///     so the message can exceed 255 bytes. <see cref="MessageType" /> (see <see cref="SystemMessageType" />)
///     selects how and where the text is rendered; the enum is byte-backed, so an unrecognized value still
///     round-trips as an unnamed value.
/// </summary>
/// <remarks>
///     This packet models the uniform <c>[u8 type][string16 message]</c> shape. Type <c>0x11</c> uses a
///     different body layout (an extra <c>[u8][u8][string8]</c> block before the message) and is not
///     modeled here, so it must not be used with this packet.
/// </remarks>
[ServerOpcode(ServerOpcode.SystemMessage)]
public sealed record SystemMessagePacket : ServerPacket
{
    // Note: type 0x11 uses a different body layout (an extra [u8][u8][string8] block before the
    // message; see the remarks), so it is not a member of SystemMessageType - this packet does not
    // model that variant.

    /// <summary>How and where the message is rendered.</summary>
    public required SystemMessageType MessageType { get; init; }

    /// <summary>The message text (Latin-1), length-prefixed on the wire by a <c>u16</c>.</summary>
    public required string Message { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.SystemMessage;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte((byte)MessageType);
        writer.WriteString16(Message);
    }

    /// <inheritdoc />
    public static SystemMessagePacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var messageType = (SystemMessageType)reader.ReadByte();
        var message = reader.ReadString16();

        return new SystemMessagePacket
        {
            MessageType = messageType,
            Message = message,
        };
    }
}
