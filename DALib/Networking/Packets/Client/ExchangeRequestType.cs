namespace DALib.Networking.Packets.Client;

/// <summary>
///     The stage byte leading C->S 0x4A <see cref="ExchangePacket" /> - which step of a
///     player-to-player trade is being driven. Selects the variant and the tail layout.
/// </summary>
public enum ExchangeRequestType : byte
{
    /// <summary>Stage 0 - begin a trade with a target player. The prefix's id is the target.</summary>
    StartExchange = 0,

    /// <summary>Stage 1 - offer an item from a slot (the server prompts for quantity if it stacks).</summary>
    AddItem = 1,

    /// <summary>Stage 2 - offer a counted quantity of a stackable item from a slot.</summary>
    AddStackableItem = 2,

    /// <summary>Stage 3 - set the gold offered.</summary>
    SetGold = 3,

    /// <summary>Stage 4 - cancel the trade (also an auto-decline of an invite).</summary>
    Cancel = 4,

    /// <summary>Stage 5 - confirm/accept the trade.</summary>
    Accept = 5
}
