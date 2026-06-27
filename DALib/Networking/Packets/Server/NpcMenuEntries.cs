using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     One selectable option in an <see cref="OptionsMenu" /> / <see cref="OptionsWithArgumentMenu" />
///     (S->C 0x2F): a label and the pursuit id its selection sends back. Wire: <c>[u8 len][latin-1 Text][u16-BE Pursuit]</c>.
/// </summary>
public sealed record NpcMenuOption(string Text, ushort Pursuit);

/// <summary>
///     One purchasable item in an <see cref="ItemListMenu" /> (S->C 0x2F). Wire:
///     <c>[u16-BE Sprite][u8 Color][u32-BE Cost][u8 len][name][u8 len][description]</c>.
/// </summary>
/// <remarks>
///     <see cref="Sprite" /> is the raw on-wire value, item-offset-encoded; DALib round-trips it verbatim
///     and leaves the offset decode to the caller. <see cref="Cost" /> is unsigned on the wire.
/// </remarks>
public sealed record NpcMenuItem(ushort Sprite, byte Color, uint Cost, string Name, string Description);

/// <summary>
///     One learnable spell or skill in a <see cref="SpellListMenu" /> / <see cref="SkillListMenu" />
///     (S->C 0x2F). Wire: <c>[u8 IconType][u16-BE Icon][u8 Color][u8 len][name]</c>. Spells and skills
///     share an identical wire shape, so both lists use this record.
/// </summary>
/// <remarks>
///     <see cref="IconType" /> selects how <see cref="Icon" /> is rendered: 0=none, 1=item (offset
///     sprite), 2=spell icon, 3=skill icon, 4=monster sprite.
/// </remarks>
public sealed record NpcMenuCastable(byte IconType, ushort Icon, byte Color, string Name);

/// <summary>
///     One row of a <see cref="PlayerItemHandleMenu" /> (S->C 0x2F), used when a type-5/11 menu's pursuit
///     id is <c>0x4E</c>. Pairs one of the player's inventory <see cref="Slot" />s with a server-assigned
///     <see cref="Handle" />. Wire: <c>[u8 Slot][u32-BE Handle]</c>.
/// </summary>
/// <remarks>
///     The <c>0x4E</c> pursuit selects this handle-carrying form; the plain
///     <see cref="PlayerItemListMenu" /> (any other pursuit) carries bare slot bytes with no handle.
/// </remarks>
public sealed record NpcPlayerItemHandle(byte Slot, uint Handle);

/// <summary>
///     One row of a <see cref="ServerItemMenu" /> (S->C 0x2F): the rich, navigable item-tree form used
///     when a type-4/10 menu's pursuit id is <c>0x4B</c>. Distinct from the flat <see cref="NpcMenuItem" />:
///     each row carries a server-assigned <see cref="Handle" />, a leaf/branch discriminator for
///     sub-menus, an <see cref="Available" /> (sold-out) flag, and a <see cref="StockRemaining" /> /
///     <see cref="StockMax" /> pair for limited-stock vendors.
/// </summary>
/// <remarks>
///     <para>
///         Wire: <c>[u32-BE Handle][u16-BE Sprite][u8 Color][u32-BE Cost][u8 Available][u8 len][Name]
///         [u8 LeafBranch]</c>, then - only when <see cref="LeafBranch" /> == 1 - <c>[u8 len][SubLabel]</c>,
///         then <c>[u32-BE StockRemaining][u32-BE StockMax]</c>.
///     </para>
///     <para>
///         <see cref="Handle" /> is the row id echoed back when this row is selected (C->S 0x39 Form D).
///         <see cref="LeafBranch" /> is the navigable-tree discriminator: <c>1</c> = a branch (a
///         <see cref="SubLabel" /> follows), any other value = a leaf (no sub-label). The byte is kept
///         raw for round-trip fidelity; <see cref="SubLabel" /> is on the wire iff it is exactly 1.
///     </para>
///     <para>
///         <see cref="Available" /> is the sold-out flag, tested only zero/nonzero, so it is modeled as a
///         <c>bool</c> (emit 1 for available). <see cref="StockRemaining" /> / <see cref="StockMax" />
///         render a "remaining / max" line, shown only when remaining != 0 and max != <see cref="Unlimited" />
///         (<c>0xFFFFFFFF</c> = no cap).
///     </para>
/// </remarks>
public sealed record NpcServerItem
{
    /// <summary>The "no stock cap" sentinel for <see cref="StockMax" /> - no count is rendered when it is set.</summary>
    public const uint Unlimited = uint.MaxValue;

    /// <summary>The server-assigned row handle, echoed back on selection (C->S 0x39 Form D).</summary>
    public required uint Handle { get; init; }

    /// <summary>The row's sprite, raw on-wire value (offset-encoded).</summary>
    public ushort Sprite { get; init; }

    /// <summary>The row's color.</summary>
    public byte Color { get; init; }

    /// <summary>The item's gold price (rendered comma-formatted in the row's right column).</summary>
    public uint Cost { get; init; }

    /// <summary>
    ///     Availability flag. <c>false</c> marks the row sold out, <c>true</c> available. Tested only
    ///     zero/nonzero (emit 1 for available, 0 for sold out). Defaults to available.
    /// </summary>
    public bool Available { get; init; } = true;

    /// <summary>The row label (<c>string8</c>).</summary>
    public required string Name { get; init; }

    /// <summary>
    ///     Leaf/branch discriminator. <c>1</c> = branch (a <see cref="SubLabel" /> follows on the wire);
    ///     any other value = leaf. Kept raw for fidelity.
    /// </summary>
    public byte LeafBranch { get; init; }

    /// <summary>The sub-menu label, present on the wire only when <see cref="LeafBranch" /> == 1.</summary>
    public string SubLabel { get; init; } = string.Empty;

    /// <summary>
    ///     Units remaining in stock. With <see cref="StockMax" /> renders a "remaining / max" line -
    ///     suppressed when this is 0.
    /// </summary>
    public uint StockRemaining { get; init; }

    /// <summary>
    ///     Maximum stock. Rendered with <see cref="StockRemaining" /> unless set to
    ///     <see cref="Unlimited" /> (<c>0xFFFFFFFF</c>), which suppresses the count.
    /// </summary>
    public uint StockMax { get; init; }

    internal void Write(IPacketWriter writer)
    {
        writer.WriteUInt32(Handle);
        writer.WriteUInt16(Sprite);
        writer.WriteByte(Color);
        writer.WriteUInt32(Cost);
        writer.WriteBoolean(Available);
        writer.WriteString8(Name);
        writer.WriteByte(LeafBranch);

        if (LeafBranch == 1)
            writer.WriteString8(SubLabel);

        writer.WriteUInt32(StockRemaining);
        writer.WriteUInt32(StockMax);
    }

    internal static NpcServerItem Parse(ref PacketReader reader)
    {
        var handle = reader.ReadUInt32();
        var sprite = reader.ReadUInt16();
        var color = reader.ReadByte();
        var cost = reader.ReadUInt32();
        var available = reader.ReadBoolean();
        var name = reader.ReadString8();
        var leafBranch = reader.ReadByte();
        var subLabel = leafBranch == 1 ? reader.ReadString8() : string.Empty;
        var stockRemaining = reader.ReadUInt32();
        var stockMax = reader.ReadUInt32();

        return new NpcServerItem
        {
            Handle = handle,
            Sprite = sprite,
            Color = color,
            Cost = cost,
            Available = available,
            Name = name,
            LeafBranch = leafBranch,
            SubLabel = subLabel,
            StockRemaining = stockRemaining,
            StockMax = stockMax,
        };
    }
}
