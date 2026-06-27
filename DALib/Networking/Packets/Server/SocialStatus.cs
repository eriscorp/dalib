namespace DALib.Networking.Packets.Server;

/// <summary>
///     A player's social / grouping-availability status. Shown as a status indicator on the
///     profile pane (0x34 <see cref="ProfilePacket" />) and beside each entry of the online-user
///     list (0x36 <see cref="UserListPacket" />). On the wire it is a single byte (values 0-7),
///     rendered as the matching status glyph.
/// </summary>
public enum SocialStatus : byte
{
    /// <summary>0 - no special status (awake / available).</summary>
    Awake = 0,

    /// <summary>1 - do not disturb.</summary>
    DoNotDisturb = 1,

    /// <summary>2 - day dreaming.</summary>
    DayDreaming = 2,

    /// <summary>3 - looking for a group.</summary>
    NeedGroup = 3,

    /// <summary>4 - currently grouped.</summary>
    Grouped = 4,

    /// <summary>5 - soloing by choice (lone hunter).</summary>
    LoneHunter = 5,

    /// <summary>6 - group hunting.</summary>
    GroupHunting = 6,

    /// <summary>7 - needs help.</summary>
    NeedHelp = 7
}
