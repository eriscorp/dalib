using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x23 (C->S) - save the text of a notepad/note attached to an item slot. Body:
///     <c>[u8 Slot][string16 Message]</c>. The message uses a 16-bit length prefix because the
///     text can run long.
/// </summary>
[ClientOpcode(ClientOpcode.SetNotepad)]
public sealed record SetNotepadPacket : ClientPacket
{
    /// <summary>The inventory slot whose notepad text is being saved.</summary>
    public required byte Slot { get; init; }

    /// <summary>The note text. Length-prefixed with a 16-bit count on the wire.</summary>
    public required string Message { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.SetNotepad;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(Slot);
        writer.WriteString16(Message);
    }

    /// <inheritdoc />
    public static SetNotepadPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        return new SetNotepadPacket
        {
            Slot = reader.ReadByte(),
            Message = reader.ReadString16(),
        };
    }
}
