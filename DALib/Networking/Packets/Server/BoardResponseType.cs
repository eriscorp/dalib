namespace DALib.Networking.Packets.Server;

/// <summary>
///     The leading byte of S->C 0x31 <see cref="BoardResponsePacket" /> - selects which board/mail view
///     or result the body represents, and therefore the body layout.
/// </summary>
/// <remarks>
///     Form shape is not 1:1 with this byte. The two index views (<see cref="PublicBoard" /> /
///     <see cref="PrivateBoard" />) share one body shape, and the two single-post views
///     (<see cref="PublicPost" /> / <see cref="PrivatePost" />) share another; the byte additionally
///     selects which dialog (bulletin board vs mailbox) to open. The three result values
///     (<see cref="EndResult" />/<see cref="DeleteResult" />/<see cref="HighlightResult" />) share the
///     result shape.
/// </remarks>
public enum BoardResponseType : byte
{
    /// <summary>The list of available boards (plus the "Mail" pseudo-board).</summary>
    BoardList = 1,

    /// <summary>A bulletin board's message index.</summary>
    PublicBoard = 2,

    /// <summary>A single bulletin post (with body).</summary>
    PublicPost = 3,

    /// <summary>The mailbox message index.</summary>
    PrivateBoard = 4,

    /// <summary>A single mail message (with body).</summary>
    PrivatePost = 5,

    /// <summary>A post/send result popup.</summary>
    EndResult = 6,

    /// <summary>A delete result popup.</summary>
    DeleteResult = 7,

    /// <summary>A highlight result popup.</summary>
    HighlightResult = 8
}
