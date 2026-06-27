namespace DALib.Networking.Packets.Server;

/// <summary>
///     The body discriminator of an <see cref="NpcMenuPacket" /> (S->C 0x2F). The first wire byte
///     of the packet; selects which menu body shape follows the shared prefix.
/// </summary>
/// <remarks>
///     Types 10 and 11 are aliases that reuse the bodies of <see cref="ItemList" /> (4) and
///     <see cref="PlayerItemList" /> (5) respectively - byte-for-byte identical bodies. Modeled for wire
///     completeness and round-trip fidelity; not emitted by typical servers. Because 10/11 share shapes
///     with 4/5, MenuType is not derivable from the body and lives as an explicit field on
///     <see cref="NpcMenuPacket" />.
/// </remarks>
public enum NpcMenuType : byte
{
    /// <summary>0 - a list of selectable text options, each carrying a pursuit id.</summary>
    Options = 0,

    /// <summary>1 - an options list preceded by a free-text argument string.</summary>
    OptionsWithArgument = 1,

    /// <summary>2 - a free-text input prompt bound to a pursuit id.</summary>
    TextEntry = 2,

    /// <summary>3 - a text input preceded by a free-text argument string.</summary>
    TextEntryWithArgument = 3,

    /// <summary>
    ///     4 - an item list (sprite, color, cost, name, description). When the body's pursuit id is
    ///     <c>0x4B</c> the richer <see cref="ServerItemMenu" /> tree layout is parsed instead; otherwise
    ///     <see cref="ItemListMenu" />.
    /// </summary>
    ItemList = 4,

    /// <summary>5 - the player's own inventory presented as a list of slots.</summary>
    PlayerItemList = 5,

    /// <summary>6 - learnable spells (icon entries).</summary>
    SpellList = 6,

    /// <summary>7 - learnable skills (icon entries).</summary>
    SkillList = 7,

    /// <summary>8 - the player's own spellbook, by pursuit id.</summary>
    PlayerSpellList = 8,

    /// <summary>9 - the player's own skillbook, by pursuit id.</summary>
    PlayerSkillList = 9,

    /// <summary>
    ///     10 - alias of <see cref="ItemList" />: body identical to type 4 (including the <c>0x4B</c>
    ///     <see cref="ServerItemMenu" /> option). Modeled for completeness.
    /// </summary>
    ItemListAlternate = 10,

    /// <summary>
    ///     11 - alias of <see cref="PlayerItemList" />: body identical to type 5. Modeled for completeness.
    /// </summary>
    PlayerItemListAlternate = 11
}
