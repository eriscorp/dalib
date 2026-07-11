namespace DALib.Networking.Packets.Client;

/// <summary>
///     The leading subtype byte that selects a C->S 0x6C <see cref="CashShopPacket" /> form and its tail.
/// </summary>
public enum CashShopActionType : byte
{
    /// <summary>0 - open/initialize the cash-shop window. No tail.</summary>
    Open = 0,

    /// <summary>1 - purchase an item. Tail <c>[u8 Slot][u32 ItemId]</c>.</summary>
    Purchase = 1,

    /// <summary>2 - close the cash-shop window. No tail.</summary>
    Close = 2
}
