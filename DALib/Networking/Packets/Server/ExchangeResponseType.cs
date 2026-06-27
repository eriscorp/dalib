namespace DALib.Networking.Packets.Server;

/// <summary>
///     The action byte leading S->C 0x42 <see cref="ExchangeResponsePacket" /> - which step of a
///     player-to-player trade the server is reporting. Selects the variant and the tail layout. This is
///     the S->C mirror of the C->S
///     <see cref="DALib.Networking.Packets.Client.ExchangeRequestType" /> (whose names differ slightly -
///     e.g. C->S <c>AddStackableItem</c> vs S->C <c>RequestAmount</c>).
/// </summary>
public enum ExchangeResponseType : byte
{
    /// <summary>A trade has opened with another player. Tail: the other party's id and name.</summary>
    StartExchange = 0,

    /// <summary>Prompt the user for a quantity of the offered stackable item. Tail: the source slot.</summary>
    RequestAmount = 1,

    /// <summary>An item now sits on one side of the trade window.</summary>
    AddItem = 2,

    /// <summary>The gold on one side of the trade window changed.</summary>
    SetGold = 3,

    /// <summary>The trade was cancelled. Tail: which side, and a message.</summary>
    Cancel = 4,

    /// <summary>The trade was confirmed/completed. Tail: which side, and a message.</summary>
    Accept = 5
}
