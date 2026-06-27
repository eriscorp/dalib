namespace DALib.Networking.Packets.Server;

/// <summary>
///     The subtype byte (the seventh body byte, after the <c>0x01</c> gate and the <c>u32</c> shop id)
///     that selects an S->C 0x4F <see cref="PlayerShopPacket" /> form and its tail.
/// </summary>
/// <remarks>
///     The open-shop path accepts only <see cref="FullState" />; the update-open-shop path is a 5-way
///     switch over all of these values, gated on a shop already being open with a matching id. Subtypes
///     <see cref="AddItem" />, <see cref="RemoveItem" />, and <see cref="UpdateItem" /> are modeled for
///     protocol completeness; not emitted by typical servers.
/// </remarks>
public enum PlayerShopType : byte
{
    /// <summary>0 - full shop state: gold, capacity, and every listing. The only subtype that can open a
    ///     closed shop window.</summary>
    FullState = 0,

    /// <summary>1 - upsert a single listing into an already-open shop. Carries one standard listing record.
    ///     Not emitted by typical servers.</summary>
    AddItem = 1,

    /// <summary>2 - remove a single listing from an already-open shop, by id. Not emitted by typical
    ///     servers.</summary>
    RemoveItem = 2,

    /// <summary>3 - update an existing listing's details (and re-key it) in an already-open shop. Carries the
    ///     <em>extended</em> listing record: an extra <c>u32</c> (the new id) after the match id, and a
    ///     retained description string. Not emitted by typical servers.</summary>
    UpdateItem = 3,

    /// <summary>4 - rename an already-open shop. Carries a single <c>string8</c> name.</summary>
    Rename = 4
}
