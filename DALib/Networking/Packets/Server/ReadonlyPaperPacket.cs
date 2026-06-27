using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x35 (S->C) - display a read-only paper/scroll (the non-writable signpost slate). The body is
///     <c>[u8 Type][u8 Width][u8 Height][bool Centered][string16 Text]</c>.
/// </summary>
[ServerOpcode(ServerOpcode.ReadonlyPaper)]
public sealed record ReadonlyPaperPacket : ServerPacket
{
    /// <summary>The paper-background texture. Closed set 0-4; see <see cref="PaperType" />.</summary>
    public required PaperType Type { get; init; }

    /// <summary>Paper width in 16px tiles (horizontal extent).</summary>
    public required byte Width { get; init; }

    /// <summary>Paper height in 16px tiles (vertical extent).</summary>
    public required byte Height { get; init; }

    /// <summary>When set, <see cref="Text" /> is horizontally centered within the paper.</summary>
    public required bool Centered { get; init; }

    /// <summary>The paper's text. Tab characters are rendered as line breaks.</summary>
    public required string Text { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.ReadonlyPaper;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte((byte)Type);
        writer.WriteByte(Width);
        writer.WriteByte(Height);
        writer.WriteBoolean(Centered);
        writer.WriteString16(Text);
    }

    /// <inheritdoc />
    public static ReadonlyPaperPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var type = (PaperType)reader.ReadByte();
        var width = reader.ReadByte();
        var height = reader.ReadByte();
        var centered = reader.ReadBoolean();
        var text = reader.ReadString16();

        return new ReadonlyPaperPacket
        {
            Type = type,
            Width = width,
            Height = height,
            Centered = centered,
            Text = text
        };
    }
}
