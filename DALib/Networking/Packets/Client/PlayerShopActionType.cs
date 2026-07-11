namespace DALib.Networking.Packets.Client;

/// <summary>
///     The action byte (after the <c>0x01</c> gate and the <c>u32</c> shop id) that selects a C->S 0x54
///     <see cref="PlayerShopActionPacket" /> form and its tail. Sent by the client while an
///     employee/consignment shop window is open; the counterpart to the S->C 0x4F
///     <see cref="DALib.Networking.Packets.Server.PlayerShopType" /> the server pushes.
/// </summary>
/// <remarks>
///     No reference server parses C->S 0x54 today (Hybrasyl has no handler), so these forms are modeled
///     for wire completeness. The <em>structure</em> of each form is binary-verified in both the 7.41 and
///     5.51 retail clients (identical layout); the <em>semantics</em> of individual fields are inferred
///     from the client's builder call sites and the traced S->C 0x4F consumer, and are flagged as such on
///     the fields that are not pinned.
/// </remarks>
public enum PlayerShopActionType : byte
{
    /// <summary>0 - withdraw gold from the shop's till. Tail <c>[u8 Selector][u32 Amount]</c>.</summary>
    WithdrawGold = 0,

    /// <summary>1 - add an item to the shop. Tail <c>[u32 Operand1][u32 Operand2][u16 0][u16 0]</c>; the
    ///     client always sends the two u16s as 0.</summary>
    AddItem = 1,

    /// <summary>2 - update a listing's terms. Tail <c>[u32 ListingId][u32 Price][u32 Count]</c>.</summary>
    UpdateListing = 2,

    /// <summary>3 - remove a listing, by id. Tail <c>[u32 ListingId]</c>.</summary>
    RemoveListing = 3,

    /// <summary>4 - close the shop window (the Dismiss button). No tail.</summary>
    CloseShop = 4,

    /// <summary>5 - the shop-opened handshake, auto-sent from the window's constructor. No tail.</summary>
    ShopOpened = 5
}
