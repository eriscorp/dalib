namespace DALib.Networking.Packets.Client;

/// <summary>
///     The leading sub-action byte that selects a C->S 0x73 <see cref="BrowserActionPacket" /> form and its
///     tail. Only the values the retail client is observed to emit are defined.
/// </summary>
public enum BrowserActionType : byte
{
    /// <summary>0 - the in-client browser window opened. No tail.</summary>
    Opened = 0,

    /// <summary>3 - a browser navigation/page action. Tail <c>[u8 Index][u8 Arg]</c>.</summary>
    Navigate = 3
}
