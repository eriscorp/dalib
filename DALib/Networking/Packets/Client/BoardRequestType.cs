namespace DALib.Networking.Packets.Client;

/// <summary>
///     The action byte leading a C-&gt;S 0x3B <see cref="BoardRequestPacket" /> - which board/mail
///     operation is being requested. Selects the variant and the tail layout. Values start at 1;
///     there is no defined 0.
/// </summary>
public enum BoardRequestType : byte
{
    /// <summary>Request the list of boards / mailboxes (the "w" key). No tail.</summary>
    BoardList = 1,

    /// <summary>Request a board's message list, paged from a starting post.</summary>
    ViewBoard = 2,

    /// <summary>Request a single post's text.</summary>
    ViewPost = 3,

    /// <summary>Submit a new post to a board.</summary>
    NewPost = 4,

    /// <summary>Delete a post by id.</summary>
    Delete = 5,

    /// <summary>Send mail to a named recipient (a post with a "to").</summary>
    SendMail = 6,

    /// <summary>Toggle a post's highlight.</summary>
    Highlight = 7
}
