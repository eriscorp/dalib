namespace DALib.Networking.Packets.Client;

/// <summary>
///     The leading action byte that selects a C->S 0x6A <see cref="MiniGamePacket" /> form and its tail.
///     Only the values the retail client is observed to emit are defined; the semantic names are inferred
///     from each form's structure and are not fully pinned.
/// </summary>
public enum MiniGameActionType : byte
{
    /// <summary>5 - open/enter the mini-game. No tail.</summary>
    Open = 5,

    /// <summary>6 - submit a data blob. Tail <c>[u8 Id][u8 len][bytes Data]</c>.</summary>
    Submit = 6,

    /// <summary>7 - sync with an incrementing sequence counter. Tail <c>[u32 Sequence]</c>.</summary>
    Sync = 7,

    /// <summary>8 - a result/state sub-action. Tail <c>[u8 0x02][u8 Flag][u8 0x00]</c>.</summary>
    Result = 8
}
