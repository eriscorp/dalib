using System;
using System.IO;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x3B (C-&gt;S) - a board or mailbox operation. The body opens with a single
///     <see cref="BoardRequestType" /> action byte that selects the form; the tail layout then
///     depends on the action. The concrete forms are the sealed records deriving from this base
///     (<see cref="BoardListPacket" />, <see cref="ViewBoardPacket" />, <see cref="ViewPostPacket" />,
///     <see cref="NewPostPacket" />, <see cref="DeletePostPacket" />, <see cref="SendMailPacket" />,
///     <see cref="HighlightPostPacket" />).
/// </summary>
/// <remarks>
///     <c>boardId</c> is a big-endian <c>u16</c>; post ids and the start cursor are 16-bit and the
///     scroll/navigation <c>Offset</c> is an 8-bit signed value, all modeled signed here. ViewBoard
///     carries a trailing <c>s8</c> offset, making it wire-identical to ViewPost; the open-board form
///     uses <c>startPostId = 0x7FFF</c> (newest) and <c>offset = -16</c>, and the paginate form passes
///     an explicit cursor. Delete is the 6-byte <c>[u16 boardId][u16 postId]</c>; some senders append
///     a stray trailing <c>0</c> byte, which the codec tolerates on parse since it advances by the
///     frame length rather than the reader position.
/// </remarks>
[ClientOpcode(ClientOpcode.BoardRequest)]
public abstract record BoardRequestPacket : ClientPacket
{
    /// <summary>The action byte that leads the body and selects this variant's form.</summary>
    public abstract BoardRequestType RequestType { get; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.BoardRequest;

    /// <summary>Writes the leading action byte. Variants call this, then append their tail.</summary>
    protected void WriteType(IPacketWriter writer) => writer.WriteByte((byte)RequestType);

    /// <summary>
    ///     Parses a 0x3B body, dispatching on the leading <see cref="BoardRequestType" /> action byte
    ///     to the matching variant. This is the standalone entry and what
    ///     <see cref="ClientOpcodeAttribute" /> dispatch binds.
    /// </summary>
    public static BoardRequestPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var type = (BoardRequestType)reader.ReadByte();

        return type switch
        {
            BoardRequestType.BoardList => new BoardListPacket(),
            BoardRequestType.ViewBoard => ViewBoardPacket.ParseTail(ref reader),
            BoardRequestType.ViewPost  => ViewPostPacket.ParseTail(ref reader),
            BoardRequestType.NewPost   => NewPostPacket.ParseTail(ref reader),
            BoardRequestType.Delete    => DeletePostPacket.ParseTail(ref reader),
            BoardRequestType.SendMail  => SendMailPacket.ParseTail(ref reader),
            BoardRequestType.Highlight => HighlightPostPacket.ParseTail(ref reader),
            _ => throw new InvalidDataException(
                $"0x3B BoardRequest: unknown request type 0x{(byte)type:X2}.")
        };
    }
}

/// <summary>
///     0x3B action 1 - request the list of boards / mailboxes (the "w" key). No tail.
/// </summary>
public sealed record BoardListPacket : BoardRequestPacket
{
    /// <inheritdoc />
    public override BoardRequestType RequestType => BoardRequestType.BoardList;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => WriteType(writer);
}

/// <summary>
///     0x3B action 2 - request a board's message list. Tail
///     <c>[u16 boardId][s16 startPostId][s8 offset]</c>. The "open board" form sends
///     <c>startPostId = 0x7FFF</c> (start from the newest) with <see cref="Offset" /> a downward
///     page count; the paginate form sends an explicit cursor.
/// </summary>
public sealed record ViewBoardPacket : BoardRequestPacket
{
    /// <summary>The board to list.</summary>
    public required ushort BoardId { get; init; }

    /// <summary>The post id to page from (<c>0x7FFF</c> means "from the newest").</summary>
    public required short StartPostId { get; init; }

    /// <summary>The scroll/page navigation amount.</summary>
    public required sbyte Offset { get; init; }

    /// <inheritdoc />
    public override BoardRequestType RequestType => BoardRequestType.ViewBoard;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WriteType(writer);
        writer.WriteUInt16(BoardId);
        writer.WriteInt16(StartPostId);
        writer.WriteSByte(Offset);
    }

    internal static ViewBoardPacket ParseTail(ref PacketReader reader) => new()
    {
        BoardId = reader.ReadUInt16(),
        StartPostId = reader.ReadInt16(),
        Offset = reader.ReadSByte()
    };
}

/// <summary>
///     0x3B action 3 - request a single post's text. Tail
///     <c>[u16 boardId][s16 postId][s8 offset]</c>. <see cref="Offset" /> is the prev/next/scroll
///     navigation.
/// </summary>
public sealed record ViewPostPacket : BoardRequestPacket
{
    /// <summary>The board the post lives on.</summary>
    public required ushort BoardId { get; init; }

    /// <summary>The post to view.</summary>
    public required short PostId { get; init; }

    /// <summary>Navigation within/around the post (e.g. previous / next).</summary>
    public required sbyte Offset { get; init; }

    /// <inheritdoc />
    public override BoardRequestType RequestType => BoardRequestType.ViewPost;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WriteType(writer);
        writer.WriteUInt16(BoardId);
        writer.WriteInt16(PostId);
        writer.WriteSByte(Offset);
    }

    internal static ViewPostPacket ParseTail(ref PacketReader reader) => new()
    {
        BoardId = reader.ReadUInt16(),
        PostId = reader.ReadInt16(),
        Offset = reader.ReadSByte()
    };
}

/// <summary>
///     0x3B action 4 - submit a new post to a board. Tail
///     <c>[u16 boardId][string8 subject][string16 body]</c>.
/// </summary>
public sealed record NewPostPacket : BoardRequestPacket
{
    /// <summary>The board to post to.</summary>
    public required ushort BoardId { get; init; }

    /// <summary>The post subject (a <c>string8</c>, capped at 255 bytes).</summary>
    public required string Subject { get; init; }

    /// <summary>The post body (a <c>string16</c>, capped at 4095 bytes).</summary>
    public required string Body { get; init; }

    /// <inheritdoc />
    public override BoardRequestType RequestType => BoardRequestType.NewPost;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WriteType(writer);
        writer.WriteUInt16(BoardId);
        writer.WriteString8(Subject);
        writer.WriteString16(Body);
    }

    internal static NewPostPacket ParseTail(ref PacketReader reader) => new()
    {
        BoardId = reader.ReadUInt16(),
        Subject = reader.ReadString8(),
        Body = reader.ReadString16()
    };
}

/// <summary>
///     0x3B action 5 - delete a post by id. Tail <c>[u16 boardId][u16 postId]</c>.
/// </summary>
public sealed record DeletePostPacket : BoardRequestPacket
{
    /// <summary>The board the post lives on.</summary>
    public required ushort BoardId { get; init; }

    /// <summary>The post to delete.</summary>
    public required short PostId { get; init; }

    /// <inheritdoc />
    public override BoardRequestType RequestType => BoardRequestType.Delete;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WriteType(writer);
        writer.WriteUInt16(BoardId);
        writer.WriteInt16(PostId);
    }

    internal static DeletePostPacket ParseTail(ref PacketReader reader) => new()
    {
        BoardId = reader.ReadUInt16(),
        PostId = reader.ReadInt16()
    };
}

/// <summary>
///     0x3B action 6 - send mail to a named recipient (a post with a "to"). Tail
///     <c>[u16 boardId][string8 recipient][string8 subject][string16 body]</c>.
/// </summary>
public sealed record SendMailPacket : BoardRequestPacket
{
    /// <summary>The mailbox board id.</summary>
    public required ushort BoardId { get; init; }

    /// <summary>The recipient character name (<c>string8</c>).</summary>
    public required string Recipient { get; init; }

    /// <summary>The mail subject (<c>string8</c>).</summary>
    public required string Subject { get; init; }

    /// <summary>The mail body (<c>string16</c>).</summary>
    public required string Body { get; init; }

    /// <inheritdoc />
    public override BoardRequestType RequestType => BoardRequestType.SendMail;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WriteType(writer);
        writer.WriteUInt16(BoardId);
        writer.WriteString8(Recipient);
        writer.WriteString8(Subject);
        writer.WriteString16(Body);
    }

    internal static SendMailPacket ParseTail(ref PacketReader reader) => new()
    {
        BoardId = reader.ReadUInt16(),
        Recipient = reader.ReadString8(),
        Subject = reader.ReadString8(),
        Body = reader.ReadString16()
    };
}

/// <summary>
///     0x3B action 7 - toggle a post's highlight. Tail <c>[u16 boardId][s16 postId]</c>.
/// </summary>
public sealed record HighlightPostPacket : BoardRequestPacket
{
    /// <summary>The board the post lives on.</summary>
    public required ushort BoardId { get; init; }

    /// <summary>The post to highlight.</summary>
    public required short PostId { get; init; }

    /// <inheritdoc />
    public override BoardRequestType RequestType => BoardRequestType.Highlight;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WriteType(writer);
        writer.WriteUInt16(BoardId);
        writer.WriteInt16(PostId);
    }

    internal static HighlightPostPacket ParseTail(ref PacketReader reader) => new()
    {
        BoardId = reader.ReadUInt16(),
        PostId = reader.ReadInt16()
    };
}
