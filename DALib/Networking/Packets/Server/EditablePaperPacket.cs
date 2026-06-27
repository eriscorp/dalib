using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x1B (S->C) - open an editable paper/sign the player can write on. The body is
///     <c>[u8 Slot][u8 Type][u8 Width][u8 Height][string16 Text]</c>. The readonly form (0x35)
///     has no slot and instead carries a trailing <c>Centered</c> flag.
/// </summary>
[ServerOpcode(ServerOpcode.EditablePaper)]
public sealed record EditablePaperPacket : ServerPacket
{
    /// <summary>The inventory slot the editable paper is bound to.</summary>
    public required byte Slot { get; init; }

    /// <summary>The paper-background texture; see <see cref="PaperType" />. Closed set 0-4.</summary>
    public required PaperType Type { get; init; }

    /// <summary>Paper width in 16px tiles (horizontal extent); rendered as <c>(Width+2)*16</c> pixels wide.</summary>
    public required byte Width { get; init; }

    /// <summary>Paper height in 16px tiles (vertical extent); rendered as <c>(Height+3)*16</c> pixels tall.</summary>
    public required byte Height { get; init; }

    /// <summary>The paper's current text. Tab characters are rendered as line breaks.</summary>
    public required string Text { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.EditablePaper;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(Slot);
        writer.WriteByte((byte)Type);
        writer.WriteByte(Width);
        writer.WriteByte(Height);
        writer.WriteString16(Text);
    }

    /// <inheritdoc />
    public static EditablePaperPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var slot = reader.ReadByte();
        var type = (PaperType)reader.ReadByte();
        var width = reader.ReadByte();
        var height = reader.ReadByte();
        var text = reader.ReadString16();

        return new EditablePaperPacket
        {
            Slot = slot,
            Type = type,
            Width = width,
            Height = height,
            Text = text
        };
    }
}
