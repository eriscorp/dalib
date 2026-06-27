using System;
using System.Collections.Generic;
using System.IO;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x31 (S->C) - drives the bulletin-board / mailbox UI. The body opens with a single
///     <c>[u8 type]</c> byte (a <see cref="BoardResponseType" />) selecting the view; the rest varies by
///     form. The concrete forms are the sealed records deriving from this base
///     (<see cref="BoardListPacket" />, <see cref="BoardIndexPacket" />, <see cref="BoardPostPacket" />,
///     <see cref="BoardResultPacket" />). The corresponding client request is C->S 0x3B
///     (<see cref="DALib.Networking.Packets.Client.BoardRequestPacket" />).
/// </summary>
/// <remarks>
///     The leading type byte selects both the UI (bulletin board vs mailbox) and the body form: the
///     index views (<see cref="BoardResponseType.PublicBoard" />/<see cref="BoardResponseType.PrivateBoard" />)
///     share one body, the post views (<see cref="BoardResponseType.PublicPost" />/
///     <see cref="BoardResponseType.PrivatePost" />) share another, and the three result values share the
///     result body. Because the body shape alone is ambiguous, <see cref="ResponseType" /> travels as its
///     own field, validated against each variant's allowed set in <c>WriteBody</c>. The result forms
///     (types 6/7/8) are modeled for protocol completeness.
/// </remarks>
[ServerOpcode(ServerOpcode.Board)]
public abstract record BoardResponsePacket : ServerPacket
{
    /// <summary>The leading byte that selects this form. Validated against the variant's allowed set on write.</summary>
    public required BoardResponseType ResponseType { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.Board;

    /// <summary>Writes the leading <c>[u8 type]</c> byte after validating it is one this variant accepts.</summary>
    private protected void WriteLeadingByte(IPacketWriter writer, params BoardResponseType[] allowed)
    {
        if (Array.IndexOf(allowed, ResponseType) < 0)
            throw new InvalidOperationException(
                $"{GetType().Name}: ResponseType {ResponseType} is not valid for this form (expected one of " +
                $"{string.Join(", ", allowed)}).");

        writer.WriteByte((byte)ResponseType);
    }

    /// <summary>
    ///     Parses a 0x31 body, dispatching on the leading <see cref="BoardResponseType" /> byte to the
    ///     matching variant. This is what <see cref="ServerOpcodeAttribute" /> dispatch binds.
    /// </summary>
    public static BoardResponsePacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var type = (BoardResponseType)reader.ReadByte();

        return type switch
        {
            BoardResponseType.BoardList => BoardListPacket.ParseBody(ref reader),
            BoardResponseType.PublicBoard or BoardResponseType.PrivateBoard
                => BoardIndexPacket.ParseBody(type, ref reader),
            BoardResponseType.PublicPost or BoardResponseType.PrivatePost
                => BoardPostPacket.ParseBody(type, ref reader),
            BoardResponseType.EndResult or BoardResponseType.DeleteResult or BoardResponseType.HighlightResult
                => BoardResultPacket.ParseBody(type, ref reader),
            _ => throw new InvalidDataException(
                $"0x31 Board: unknown response type 0x{(byte)type:X2}.")
        };
    }
}

/// <summary>One entry of a <see cref="BoardListPacket" />: a board's id and display name.</summary>
public readonly record struct BoardListEntry(ushort Id, string Name);

/// <summary>
///     One row of a <see cref="BoardIndexPacket" /> message index: the post's highlight state, id,
///     author, date, and subject (no body - the body arrives with <see cref="BoardPostPacket" />).
/// </summary>
public readonly record struct BoardMessageHeader(
    bool Highlight, ushort PostId, string Author, byte Month, byte Day, string Subject);

/// <summary>
///     0x31 type 1 - the list of boards. Body <c>[string8 Name][u8 count]{[u16 Id][string8 Name]}</c>. The
///     "Mail" mailbox conventionally appears as the first entry (id 0).
///     <see cref="BoardResponsePacket.ResponseType" /> must be <see cref="BoardResponseType.BoardList" />.
/// </summary>
public sealed record BoardListPacket : BoardResponsePacket
{
    /// <summary>A leading name field read before the entries; conventionally empty.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     The boards offered (id + name each); the "Mail" pseudo-board conventionally appears first.
    ///     Mutable so a server can accumulate entries while filtering by per-character access.
    /// </summary>
    public IList<BoardListEntry> Boards { get; set; } = [];

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WriteLeadingByte(writer, BoardResponseType.BoardList);

        if (Boards.Count > byte.MaxValue)
            throw new InvalidOperationException(
                $"BoardListPacket: board count {Boards.Count} exceeds the wire u8 limit ({byte.MaxValue}).");

        writer.WriteString8(Name);
        writer.WriteByte((byte)Boards.Count);

        foreach (var board in Boards)
        {
            writer.WriteUInt16(board.Id);
            writer.WriteString8(board.Name);
        }
    }

    internal static BoardListPacket ParseBody(ref PacketReader reader)
    {
        var name = reader.ReadString8();
        var count = reader.ReadByte();
        var boards = new List<BoardListEntry>(count);

        for (var i = 0; i < count; i++)
            boards.Add(new BoardListEntry(reader.ReadUInt16(), reader.ReadString8()));

        return new BoardListPacket
        {
            ResponseType = BoardResponseType.BoardList,
            Name = name,
            Boards = boards
        };
    }
}

/// <summary>
///     0x31 types 2/4 - a board's (<see cref="BoardResponseType.PublicBoard" />) or mailbox's
///     (<see cref="BoardResponseType.PrivateBoard" />) message index. Body
///     <c>[u8 RefreshFlag][u16 BoardId][string8 BoardName][u8 count]{[u8 Highlight][u16 PostId][string8 Author][u8 Month][u8 Day][string8 Subject]}</c>.
/// </summary>
public sealed record BoardIndexPacket : BoardResponsePacket
{
    /// <summary>
    ///     A refresh control: a value of 0 requests a list refresh; any nonzero value skips it.
    /// </summary>
    public required byte RefreshFlag { get; init; }

    /// <summary>The board's id.</summary>
    public required ushort BoardId { get; init; }

    /// <summary>The board's display name.</summary>
    public required string BoardName { get; init; }

    /// <summary>
    ///     The message headers shown in the index. Mutable so a server can accumulate entries while
    ///     filtering by per-character access.
    /// </summary>
    public IList<BoardMessageHeader> Messages { get; set; } = [];

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WriteLeadingByte(writer, BoardResponseType.PublicBoard, BoardResponseType.PrivateBoard);

        if (Messages.Count > byte.MaxValue)
            throw new InvalidOperationException(
                $"BoardIndexPacket: message count {Messages.Count} exceeds the wire u8 limit ({byte.MaxValue}).");

        writer.WriteByte(RefreshFlag);
        writer.WriteUInt16(BoardId);
        writer.WriteString8(BoardName);
        writer.WriteByte((byte)Messages.Count);

        foreach (var message in Messages)
        {
            writer.WriteBoolean(message.Highlight);
            writer.WriteUInt16(message.PostId);
            writer.WriteString8(message.Author);
            writer.WriteByte(message.Month);
            writer.WriteByte(message.Day);
            writer.WriteString8(message.Subject);
        }
    }

    internal static BoardIndexPacket ParseBody(BoardResponseType type, ref PacketReader reader)
    {
        var refreshFlag = reader.ReadByte();
        var boardId = reader.ReadUInt16();
        var boardName = reader.ReadString8();
        var count = reader.ReadByte();
        var messages = new List<BoardMessageHeader>(count);

        for (var i = 0; i < count; i++)
            messages.Add(new BoardMessageHeader(
                reader.ReadBoolean(), reader.ReadUInt16(), reader.ReadString8(),
                reader.ReadByte(), reader.ReadByte(), reader.ReadString8()));

        return new BoardIndexPacket
        {
            ResponseType = type,
            RefreshFlag = refreshFlag,
            BoardId = boardId,
            BoardName = boardName,
            Messages = messages
        };
    }
}

/// <summary>
///     0x31 types 3/5 - a single bulletin post (<see cref="BoardResponseType.PublicPost" />) or mail
///     message (<see cref="BoardResponseType.PrivatePost" />). Body
///     <c>[u8 RefreshFlag][u8 Highlight][u16 PostId][string8 Author][u8 Month][u8 Day][string8 Subject][string16 Body]</c>.
///     A <see cref="PostId" /> of 0 signals "no such message" and closes the pane.
/// </summary>
public sealed record BoardPostPacket : BoardResponsePacket
{
    /// <summary>A refresh control (see <see cref="BoardIndexPacket.RefreshFlag" />).</summary>
    public required byte RefreshFlag { get; init; }

    /// <summary>
    ///     The post's highlight state. This byte is not used in the post view (highlight is honored only
    ///     in the index); preserved for round-tripping.
    /// </summary>
    public required bool Highlight { get; init; }

    /// <summary>The post's id (a value of 0 means "no message" and closes the pane).</summary>
    public required ushort PostId { get; init; }

    /// <summary>The post's author.</summary>
    public required string Author { get; init; }

    /// <summary>The post date's month component (rendered as <c>%2d/%2d</c> month/day).</summary>
    public required byte Month { get; init; }

    /// <summary>The post date's day component.</summary>
    public required byte Day { get; init; }

    /// <summary>The post's subject/title.</summary>
    public required string Subject { get; init; }

    /// <summary>The post's body text (<c>string16</c>).</summary>
    public required string Body { get; init; }

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WriteLeadingByte(writer, BoardResponseType.PublicPost, BoardResponseType.PrivatePost);

        writer.WriteByte(RefreshFlag);
        writer.WriteBoolean(Highlight);
        writer.WriteUInt16(PostId);
        writer.WriteString8(Author);
        writer.WriteByte(Month);
        writer.WriteByte(Day);
        writer.WriteString8(Subject);
        writer.WriteString16(Body);
    }

    internal static BoardPostPacket ParseBody(BoardResponseType type, ref PacketReader reader)
        => new()
        {
            ResponseType = type,
            RefreshFlag = reader.ReadByte(),
            Highlight = reader.ReadBoolean(),
            PostId = reader.ReadUInt16(),
            Author = reader.ReadString8(),
            Month = reader.ReadByte(),
            Day = reader.ReadByte(),
            Subject = reader.ReadString8(),
            Body = reader.ReadString16()
        };
}

/// <summary>
///     0x31 types 6/7/8 - a result popup for a board action (<see cref="BoardResponseType.EndResult" />
///     post/send, <see cref="BoardResponseType.DeleteResult" /> delete, <see cref="BoardResponseType.HighlightResult" />
///     highlight). Body <c>[bool Success][string8 Message]</c>. Modeled for protocol completeness.
/// </summary>
public sealed record BoardResultPacket : BoardResponsePacket
{
    /// <summary>Whether the action succeeded.</summary>
    public required bool Success { get; init; }

    /// <summary>The result message the server wants displayed.</summary>
    public required string Message { get; init; }

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WriteLeadingByte(
            writer, BoardResponseType.EndResult, BoardResponseType.DeleteResult, BoardResponseType.HighlightResult);

        writer.WriteBoolean(Success);
        writer.WriteString8(Message);
    }

    internal static BoardResultPacket ParseBody(BoardResponseType type, ref PacketReader reader)
        => new()
        {
            ResponseType = type,
            Success = reader.ReadBoolean(),
            Message = reader.ReadString8()
        };
}
