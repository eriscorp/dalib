namespace DALib.Networking.Packets.Server;

/// <summary>
///     The render channel of a <see cref="SystemMessagePacket" /> (0x0A) - the leading type byte that
///     selects how and where the message is displayed. The backing type is <see cref="byte" />, so an
///     unrecognized value still round-trips as an unnamed enum value. Type 17 (0x11) is deliberately not
///     a member: it uses a different body layout (an extra <c>[u8][u8][string8]</c> block before the
///     message) that <see cref="SystemMessagePacket" /> does not model.
/// </summary>
public enum SystemMessageType : byte
{
    /// <summary>0 - blue text, top-left (the "whisper"/private channel).</summary>
    Whisper = 0,

    /// <summary>1 - action-bar only; not shown in chat history.</summary>
    OrangeBar1 = 1,

    /// <summary>2 - action-bar only; not shown in chat history.</summary>
    OrangeBar2 = 2,

    /// <summary>3 - action bar and chat history.</summary>
    ActiveMessage = 3,

    /// <summary>4 - action-bar only; not shown in chat history.</summary>
    OrangeBar3 = 4,

    /// <summary>5 - admin/world message (action-bar only).</summary>
    AdminMessage = 5,

    /// <summary>6 - action-bar only; not shown in chat history.</summary>
    OrangeBar5 = 6,

    /// <summary>7 - user-option list channel.</summary>
    UserOptions = 7,

    /// <summary>8 - opens a pop-up window with a scroll bar.</summary>
    ScrollWindow = 8,

    /// <summary>9 - opens a pop-up window with no scroll bar.</summary>
    NonScrollWindow = 9,

    /// <summary>10 - opens a wooden-bordered window (signposts / boards).</summary>
    WoodenBoard = 10,

    /// <summary>11 - group-chat color.</summary>
    GroupChat = 11,

    /// <summary>12 - guild-chat color.</summary>
    GuildChat = 12,

    /// <summary>18 - white text pinned in the top-right until cleared.</summary>
    TopRight = 18,
}
