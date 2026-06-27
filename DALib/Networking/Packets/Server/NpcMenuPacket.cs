using System;
using System.Collections.Generic;
using System.IO;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x2F (S->C) - displays an NPC/merchant menu. A shared prefix (the menu's source entity, sprite,
///     illustration, name and text) is followed by exactly one body shape; the leading
///     <see cref="MenuType" /> byte selects which. This is the merchant subsystem's display packet;
///     answered with C->S 0x39 (<see cref="DALib.Networking.Packets.Client.NpcMainMenuPacket" />).
/// </summary>
/// <remarks>
///     <para>
///         Clicking an NPC (C->S 0x43) yields either this packet (merchant menus, keyed by
///         <see cref="NpcMenuType" />, answered by 0x39) or 0x30 DisplayDialog (scripted dialog,
///         answered by 0x3A).
///     </para>
///     <para>
///         <see cref="MenuType" /> travels as its own field: types 10 and 11 reuse the bodies of 4 and 5
///         (see <see cref="NpcMenuType" />), so the type and body shape are not 1:1.
///         <see cref="WriteBody" /> validates that the <see cref="Menu" /> shape is compatible with
///         <see cref="MenuType" />.
///     </para>
///     <para>
///         Two families carry a pursuit-keyed row fork. For
///         <see cref="NpcMenuType.ItemList" />/<see cref="NpcMenuType.ItemListAlternate" />, a pursuit id
///         of <c>0x4B</c> selects the rich <see cref="ServerItemMenu" /> tree, otherwise the flat
///         <see cref="ItemListMenu" />. For
///         <see cref="NpcMenuType.PlayerItemList" />/<see cref="NpcMenuType.PlayerItemListAlternate" />, a
///         pursuit of <c>0x4E</c> selects the per-row handle in <see cref="PlayerItemHandleMenu" />, else
///         the bare-slot <see cref="PlayerItemListMenu" />.
///     </para>
///     <para>
///         Four prefix bytes (<see cref="Unknown1" />, <see cref="Unknown2" />, <see cref="Sprite2" /> and
///         <see cref="Color2" />) are parsed but unused; they remain settable round-trip fields.
///     </para>
/// </remarks>
[ServerOpcode(ServerOpcode.NpcMenu)]
public sealed record NpcMenuPacket : ServerPacket
{
    /// <summary>The menu's source entity is a merchant/creature - the common value.</summary>
    public const byte EntityTypeMerchant = 0x01;

    /// <summary>The wire MenuType byte. Selects the <see cref="Menu" /> body shape; see <see cref="NpcMenuType" />.</summary>
    public required NpcMenuType MenuType { get; set; }

    /// <summary>
    ///     The source entity kind; <see cref="EntityTypeMerchant" /> (1) is the common value. With the
    ///     sprite-offset ranges, it determines how <see cref="Sprite" /> is interpreted
    ///     (item/creature/aisling).
    /// </summary>
    public byte EntityType { get; set; } = EntityTypeMerchant;

    /// <summary>The source entity's server id. Cosmetic.</summary>
    public uint SourceId { get; set; }

    /// <summary>
    ///     First prefix byte after <see cref="SourceId" />. Parsed but unused; emit 0. Preserved as a
    ///     settable round-trip field.
    /// </summary>
    public byte Unknown1 { get; set; }

    /// <summary>
    ///     The portrait sprite shown beside the menu, raw on-wire value (item/creature-offset-encoded).
    ///     Round-tripped verbatim.
    /// </summary>
    public ushort Sprite { get; set; }

    /// <summary>The color applied to <see cref="Sprite" /> (items/aislings).</summary>
    public byte Color { get; set; }

    /// <summary>
    ///     Second prefix byte, between the two sprite/color pairs. Parsed but unused; defaults to 1.
    ///     Settable round-trip field.
    /// </summary>
    public byte Unknown2 { get; set; } = 1;

    /// <summary>
    ///     A second sprite slot on the wire. Parsed but unused; preserved as a settable round-trip field.
    /// </summary>
    public ushort Sprite2 { get; set; }

    /// <summary>
    ///     A second color slot, paired with <see cref="Sprite2" /> on the wire. Parsed but unused;
    ///     settable round-trip field.
    /// </summary>
    public byte Color2 { get; set; }

    /// <summary>
    ///     Index into the NPC's illustration-filename list (<c>npci.tbl</c> merged with the server's
    ///     <c>NPCIllust</c> metafile). 0 selects the default/only illustration; non-zero only when an NPC
    ///     defines multiple variants.
    /// </summary>
    public byte IllustrationIndex { get; set; }

    /// <summary>The source entity's display name (<c>string8</c>).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The menu's body text (<c>string16</c>).</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>The menu body. Its shape must be compatible with <see cref="MenuType" />. Never null.</summary>
    public required NpcMenu Menu { get; set; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.NpcMenu;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        if (!Menu.Accepts(MenuType))
            throw new InvalidOperationException(
                $"NpcMenuPacket: body {Menu.GetType().Name} is not valid for MenuType {MenuType}.");

        writer.WriteByte((byte)MenuType);
        writer.WriteByte(EntityType);
        writer.WriteUInt32(SourceId);
        writer.WriteByte(Unknown1);
        writer.WriteUInt16(Sprite);
        writer.WriteByte(Color);
        writer.WriteByte(Unknown2);
        writer.WriteUInt16(Sprite2);
        writer.WriteByte(Color2);
        writer.WriteByte(IllustrationIndex);
        writer.WriteString8(Name);
        writer.WriteString16(Text);

        Menu.Write(writer);
    }

    /// <inheritdoc />
    public static NpcMenuPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var menuType = (NpcMenuType)reader.ReadByte();
        var entityType = reader.ReadByte();
        var sourceId = reader.ReadUInt32();
        var unknown1 = reader.ReadByte();
        var sprite = reader.ReadUInt16();
        var color = reader.ReadByte();
        var unknown2 = reader.ReadByte();
        var sprite2 = reader.ReadUInt16();
        var color2 = reader.ReadByte();
        var illustrationIndex = reader.ReadByte();
        var name = reader.ReadString8();
        var text = reader.ReadString16();

        var menu = NpcMenu.Parse(menuType, ref reader);

        if (reader.Position != reader.Length)
            throw new InvalidDataException(
                $"NpcMenuPacket: {reader.Length - reader.Position} trailing byte(s) after the " +
                $"{menuType} body at position {reader.Position}.");

        return new NpcMenuPacket
        {
            MenuType = menuType,
            EntityType = entityType,
            SourceId = sourceId,
            Unknown1 = unknown1,
            Sprite = sprite,
            Color = color,
            Unknown2 = unknown2,
            Sprite2 = sprite2,
            Color2 = color2,
            IllustrationIndex = illustrationIndex,
            Name = name,
            Text = text,
            Menu = menu,
        };
    }
}

/// <summary>
///     The body of an <see cref="NpcMenuPacket" /> (S->C 0x2F): one menu shape. Which
///     <see cref="NpcMenuType" /> values a shape is valid for is reported by <see cref="Accepts" />
///     (most map to one type; the ItemList/PlayerItemList shapes also accept their type-10/11 aliases).
///     Sealed variants follow this file.
/// </summary>
public abstract record NpcMenu
{
    /// <summary>Whether this body shape is a valid body for <paramref name="menuType" /> on the wire.</summary>
    internal abstract bool Accepts(NpcMenuType menuType);

    /// <summary>Writes this body's bytes, following the packet prefix.</summary>
    internal abstract void Write(IPacketWriter writer);

    /// <summary>Reads the body matching <paramref name="menuType" /> from the position after the prefix.</summary>
    internal static NpcMenu Parse(NpcMenuType menuType, ref PacketReader reader) => menuType switch
    {
        NpcMenuType.Options => OptionsMenu.ParseBody(ref reader),
        NpcMenuType.OptionsWithArgument => OptionsWithArgumentMenu.ParseBody(ref reader),
        NpcMenuType.TextEntry => TextEntryMenu.ParseBody(ref reader),
        NpcMenuType.TextEntryWithArgument => TextEntryWithArgumentMenu.ParseBody(ref reader),
        NpcMenuType.ItemList or NpcMenuType.ItemListAlternate => ParseItemFamily(ref reader),
        NpcMenuType.PlayerItemList or NpcMenuType.PlayerItemListAlternate => ParsePlayerItemFamily(ref reader),
        NpcMenuType.SpellList => SpellListMenu.ParseBody(ref reader),
        NpcMenuType.SkillList => SkillListMenu.ParseBody(ref reader),
        NpcMenuType.PlayerSpellList => PlayerSpellListMenu.ParseBody(ref reader),
        NpcMenuType.PlayerSkillList => PlayerSkillListMenu.ParseBody(ref reader),
        _ => throw new InvalidDataException($"NpcMenuPacket: unknown menu type 0x{(byte)menuType:X2}."),
    };

    /// <summary>
    ///     The ItemList family (types 4/10): read the pursuit id, then dispatch to the rich
    ///     <see cref="ServerItemMenu" /> when it is <c>0x4B</c>, else the flat <see cref="ItemListMenu" />.
    /// </summary>
    private static NpcMenu ParseItemFamily(ref PacketReader reader)
    {
        var pursuitId = reader.ReadUInt16();

        return pursuitId == ServerItemMenu.ServerItemPursuit
            ? ServerItemMenu.ParseRows(ref reader)
            : ItemListMenu.ParseRows(pursuitId, ref reader);
    }

    /// <summary>
    ///     The PlayerItemList family (types 5/11): read the pursuit id, then dispatch to the
    ///     handle-carrying <see cref="PlayerItemHandleMenu" /> when it is <c>0x4E</c>, else the bare-slot
    ///     <see cref="PlayerItemListMenu" />. The 0x4E fork is the structural twin of the ItemList
    ///     family's 0x4B fork.
    /// </summary>
    private static NpcMenu ParsePlayerItemFamily(ref PacketReader reader)
    {
        var pursuitId = reader.ReadUInt16();

        return pursuitId == PlayerItemHandleMenu.HandlePursuit
            ? PlayerItemHandleMenu.ParseRows(ref reader)
            : PlayerItemListMenu.ParseRows(pursuitId, ref reader);
    }

    private protected static void WriteOptions(IPacketWriter writer, IList<NpcMenuOption> options)
    {
        if (options.Count > byte.MaxValue)
            throw new InvalidOperationException(
                $"NpcMenu: option count {options.Count} exceeds the wire u8 limit ({byte.MaxValue}).");

        writer.WriteByte((byte)options.Count);

        foreach (var option in options)
        {
            writer.WriteString8(option.Text);
            writer.WriteUInt16(option.Pursuit);
        }
    }

    private protected static IList<NpcMenuOption> ReadOptions(ref PacketReader reader)
    {
        var count = reader.ReadByte();
        var options = new List<NpcMenuOption>(count);

        for (var i = 0; i < count; i++)
            options.Add(new NpcMenuOption(reader.ReadString8(), reader.ReadUInt16()));

        return options;
    }

    private protected static void WriteCastables(
        IPacketWriter writer, ushort pursuitId, IList<NpcMenuCastable> castables)
    {
        if (castables.Count > ushort.MaxValue)
            throw new InvalidOperationException(
                $"NpcMenu: entry count {castables.Count} exceeds the wire u16 limit ({ushort.MaxValue}).");

        writer.WriteUInt16(pursuitId);
        writer.WriteUInt16((ushort)castables.Count);

        foreach (var castable in castables)
        {
            writer.WriteByte(castable.IconType);
            writer.WriteUInt16(castable.Icon);
            writer.WriteByte(castable.Color);
            writer.WriteString8(castable.Name);
        }
    }

    private protected static (ushort PursuitId, IList<NpcMenuCastable> Castables) ReadCastables(ref PacketReader reader)
    {
        var pursuitId = reader.ReadUInt16();
        var count = reader.ReadUInt16();
        var castables = new List<NpcMenuCastable>(count);

        for (var i = 0; i < count; i++)
            castables.Add(new NpcMenuCastable(
                reader.ReadByte(), reader.ReadUInt16(), reader.ReadByte(), reader.ReadString8()));

        return (pursuitId, castables);
    }
}

/// <summary>0x2F body - <see cref="NpcMenuType.Options" />: <c>[u8 count]</c> then that many options.</summary>
public sealed record OptionsMenu : NpcMenu
{
    /// <summary>The selectable options (label + pursuit id each).</summary>
    public IList<NpcMenuOption> Options { get; init; } = [];

    internal override bool Accepts(NpcMenuType menuType) => menuType == NpcMenuType.Options;

    internal override void Write(IPacketWriter writer) => WriteOptions(writer, Options);

    internal static OptionsMenu ParseBody(ref PacketReader reader) => new() { Options = ReadOptions(ref reader) };
}

/// <summary>
///     0x2F body - <see cref="NpcMenuType.OptionsWithArgument" />: <c>[u8 len][argument]</c> then an
///     option list. Same as <see cref="OptionsMenu" /> with a leading free-text argument.
/// </summary>
public sealed record OptionsWithArgumentMenu : NpcMenu
{
    /// <summary>The free-text argument echoed/used with the selection.</summary>
    public string Argument { get; init; } = string.Empty;

    /// <summary>The selectable options (label + pursuit id each).</summary>
    public IList<NpcMenuOption> Options { get; init; } = [];

    internal override bool Accepts(NpcMenuType menuType) => menuType == NpcMenuType.OptionsWithArgument;

    internal override void Write(IPacketWriter writer)
    {
        writer.WriteString8(Argument);
        WriteOptions(writer, Options);
    }

    internal static OptionsWithArgumentMenu ParseBody(ref PacketReader reader)
    {
        var argument = reader.ReadString8();

        return new OptionsWithArgumentMenu { Argument = argument, Options = ReadOptions(ref reader) };
    }
}

/// <summary>0x2F body - <see cref="NpcMenuType.TextEntry" />: <c>[u16-BE PursuitId]</c>. A free-text input prompt.</summary>
public sealed record TextEntryMenu : NpcMenu
{
    /// <summary>The pursuit the typed response is bound to.</summary>
    public ushort PursuitId { get; init; }

    internal override bool Accepts(NpcMenuType menuType) => menuType == NpcMenuType.TextEntry;

    internal override void Write(IPacketWriter writer) => writer.WriteUInt16(PursuitId);

    internal static TextEntryMenu ParseBody(ref PacketReader reader) => new() { PursuitId = reader.ReadUInt16() };
}

/// <summary>
///     0x2F body - <see cref="NpcMenuType.TextEntryWithArgument" />: <c>[u8 len][argument][u16-BE PursuitId]</c>.
///     A text-input prompt preceded by a free-text argument.
/// </summary>
public sealed record TextEntryWithArgumentMenu : NpcMenu
{
    /// <summary>The free-text argument shown with the prompt.</summary>
    public string Argument { get; init; } = string.Empty;

    /// <summary>The pursuit the typed response is bound to.</summary>
    public ushort PursuitId { get; init; }

    internal override bool Accepts(NpcMenuType menuType) => menuType == NpcMenuType.TextEntryWithArgument;

    internal override void Write(IPacketWriter writer)
    {
        writer.WriteString8(Argument);
        writer.WriteUInt16(PursuitId);
    }

    internal static TextEntryWithArgumentMenu ParseBody(ref PacketReader reader)
    {
        var argument = reader.ReadString8();

        return new TextEntryWithArgumentMenu { Argument = argument, PursuitId = reader.ReadUInt16() };
    }
}

/// <summary>
///     0x2F body - flat item list for <see cref="NpcMenuType.ItemList" /> /
///     <see cref="NpcMenuType.ItemListAlternate" /> when the pursuit id is <em>not</em> <c>0x4B</c>:
///     <c>[u16-BE PursuitId][u16-BE count]</c> then that many <see cref="NpcMenuItem" /> entries. A
///     pursuit of <c>0x4B</c> selects the richer <see cref="ServerItemMenu" /> instead.
/// </summary>
public sealed record ItemListMenu : NpcMenu
{
    /// <summary>The pursuit a selection is bound to. Must not be <c>0x4B</c> (that is <see cref="ServerItemMenu" />).</summary>
    public ushort PursuitId { get; init; }

    /// <summary>The items offered for sale.</summary>
    public IList<NpcMenuItem> Items { get; init; } = [];

    internal override bool Accepts(NpcMenuType menuType)
        => menuType is NpcMenuType.ItemList or NpcMenuType.ItemListAlternate;

    internal override void Write(IPacketWriter writer)
    {
        if (PursuitId == ServerItemMenu.ServerItemPursuit)
            throw new InvalidOperationException(
                $"ItemListMenu: pursuit 0x{ServerItemMenu.ServerItemPursuit:X} is reserved for ServerItemMenu " +
                "(the client parses the rich row layout for it). Use ServerItemMenu instead.");

        if (Items.Count > ushort.MaxValue)
            throw new InvalidOperationException(
                $"ItemListMenu: item count {Items.Count} exceeds the wire u16 limit ({ushort.MaxValue}).");

        writer.WriteUInt16(PursuitId);
        writer.WriteUInt16((ushort)Items.Count);

        foreach (var item in Items)
        {
            writer.WriteUInt16(item.Sprite);
            writer.WriteByte(item.Color);
            writer.WriteUInt32(item.Cost);
            writer.WriteString8(item.Name);
            writer.WriteString8(item.Description);
        }
    }

    /// <summary>Reads the flat rows, given the pursuit id already consumed by the family dispatcher.</summary>
    internal static ItemListMenu ParseRows(ushort pursuitId, ref PacketReader reader)
    {
        var count = reader.ReadUInt16();
        var items = new List<NpcMenuItem>(count);

        for (var i = 0; i < count; i++)
            items.Add(new NpcMenuItem(
                reader.ReadUInt16(), reader.ReadByte(), reader.ReadUInt32(),
                reader.ReadString8(), reader.ReadString8()));

        return new ItemListMenu { PursuitId = pursuitId, Items = items };
    }
}

/// <summary>
///     0x2F body - the rich, navigable item tree used for <see cref="NpcMenuType.ItemList" /> /
///     <see cref="NpcMenuType.ItemListAlternate" /> when the pursuit id is <c>0x4B</c>:
///     <c>[u16-BE 0x4B][u16-BE count]</c> then that many <see cref="NpcServerItem" /> rows (each with a
///     server handle and leaf/branch tree info). The pursuit id is fixed to <c>0x4B</c>, which selects
///     this layout.
/// </summary>
public sealed record ServerItemMenu : NpcMenu
{
    /// <summary>The pursuit id that selects the rich row layout. Fixed.</summary>
    public const ushort ServerItemPursuit = 0x4B;

    /// <summary>The item-tree rows.</summary>
    public IList<NpcServerItem> Items { get; init; } = [];

    internal override bool Accepts(NpcMenuType menuType)
        => menuType is NpcMenuType.ItemList or NpcMenuType.ItemListAlternate;

    internal override void Write(IPacketWriter writer)
    {
        if (Items.Count > ushort.MaxValue)
            throw new InvalidOperationException(
                $"ServerItemMenu: row count {Items.Count} exceeds the wire u16 limit ({ushort.MaxValue}).");

        writer.WriteUInt16(ServerItemPursuit);
        writer.WriteUInt16((ushort)Items.Count);

        foreach (var item in Items)
            item.Write(writer);
    }

    /// <summary>Reads the rich rows, given the <c>0x4B</c> pursuit already consumed by the family dispatcher.</summary>
    internal static ServerItemMenu ParseRows(ref PacketReader reader)
    {
        var count = reader.ReadUInt16();
        var items = new List<NpcServerItem>(count);

        for (var i = 0; i < count; i++)
            items.Add(NpcServerItem.Parse(ref reader));

        return new ServerItemMenu { Items = items };
    }
}

/// <summary>
///     0x2F body - bare slot list for <see cref="NpcMenuType.PlayerItemList" /> /
///     <see cref="NpcMenuType.PlayerItemListAlternate" /> when the pursuit id is <em>not</em> <c>0x4E</c>:
///     <c>[u16-BE PursuitId][u8 count]</c> then that many inventory slot bytes. The player's own items,
///     referenced by slot. A pursuit of <c>0x4E</c> selects the handle-carrying
///     <see cref="PlayerItemHandleMenu" /> instead.
/// </summary>
public sealed record PlayerItemListMenu : NpcMenu
{
    /// <summary>The pursuit a selection is bound to. Must not be <c>0x4E</c> (that is <see cref="PlayerItemHandleMenu" />).</summary>
    public ushort PursuitId { get; init; }

    /// <summary>The inventory slots to present.</summary>
    public IList<byte> Slots { get; init; } = [];

    internal override bool Accepts(NpcMenuType menuType)
        => menuType is NpcMenuType.PlayerItemList or NpcMenuType.PlayerItemListAlternate;

    internal override void Write(IPacketWriter writer)
    {
        if (PursuitId == PlayerItemHandleMenu.HandlePursuit)
            throw new InvalidOperationException(
                $"PlayerItemListMenu: pursuit 0x{PlayerItemHandleMenu.HandlePursuit:X} is reserved for " +
                "PlayerItemHandleMenu (the client parses an extra handle per row for it). Use PlayerItemHandleMenu instead.");

        if (Slots.Count > byte.MaxValue)
            throw new InvalidOperationException(
                $"PlayerItemListMenu: slot count {Slots.Count} exceeds the wire u8 limit ({byte.MaxValue}).");

        writer.WriteUInt16(PursuitId);
        writer.WriteByte((byte)Slots.Count);

        foreach (var slot in Slots)
            writer.WriteByte(slot);
    }

    /// <summary>Reads the bare slot rows, given the pursuit id already consumed by the family dispatcher.</summary>
    internal static PlayerItemListMenu ParseRows(ushort pursuitId, ref PacketReader reader)
    {
        var count = reader.ReadByte();
        var slots = new List<byte>(count);

        for (var i = 0; i < count; i++)
            slots.Add(reader.ReadByte());

        return new PlayerItemListMenu { PursuitId = pursuitId, Slots = slots };
    }
}

/// <summary>
///     0x2F body - the handle-carrying player-item menu used for
///     <see cref="NpcMenuType.PlayerItemList" /> / <see cref="NpcMenuType.PlayerItemListAlternate" />
///     when the pursuit id is <c>0x4E</c>: <c>[u16-BE 0x4E][u8 count]</c> then that many
///     <see cref="NpcPlayerItemHandle" /> rows (each a slot byte + a server handle). The pursuit id is
///     fixed to <c>0x4E</c>, the structural twin of <see cref="ServerItemMenu" />'s <c>0x4B</c>.
/// </summary>
public sealed record PlayerItemHandleMenu : NpcMenu
{
    /// <summary>The pursuit id that selects the per-row handle layout. Fixed.</summary>
    public const ushort HandlePursuit = 0x4E;

    /// <summary>The player's items as (slot, server handle) pairs.</summary>
    public IList<NpcPlayerItemHandle> Items { get; init; } = [];

    internal override bool Accepts(NpcMenuType menuType)
        => menuType is NpcMenuType.PlayerItemList or NpcMenuType.PlayerItemListAlternate;

    internal override void Write(IPacketWriter writer)
    {
        if (Items.Count > byte.MaxValue)
            throw new InvalidOperationException(
                $"PlayerItemHandleMenu: row count {Items.Count} exceeds the wire u8 limit ({byte.MaxValue}).");

        writer.WriteUInt16(HandlePursuit);
        writer.WriteByte((byte)Items.Count);

        foreach (var item in Items)
        {
            writer.WriteByte(item.Slot);
            writer.WriteUInt32(item.Handle);
        }
    }

    /// <summary>Reads the handle rows, given the <c>0x4E</c> pursuit already consumed by the family dispatcher.</summary>
    internal static PlayerItemHandleMenu ParseRows(ref PacketReader reader)
    {
        var count = reader.ReadByte();
        var items = new List<NpcPlayerItemHandle>(count);

        for (var i = 0; i < count; i++)
            items.Add(new NpcPlayerItemHandle(reader.ReadByte(), reader.ReadUInt32()));

        return new PlayerItemHandleMenu { Items = items };
    }
}

/// <summary>
///     0x2F body - <see cref="NpcMenuType.SpellList" />: <c>[u16-BE PursuitId][u16-BE count]</c> then
///     that many <see cref="NpcMenuCastable" /> entries. Learnable spells.
/// </summary>
public sealed record SpellListMenu : NpcMenu
{
    /// <summary>The pursuit a selection is bound to.</summary>
    public ushort PursuitId { get; init; }

    /// <summary>The spells available to learn.</summary>
    public IList<NpcMenuCastable> Spells { get; init; } = [];

    internal override bool Accepts(NpcMenuType menuType) => menuType == NpcMenuType.SpellList;

    internal override void Write(IPacketWriter writer) => WriteCastables(writer, PursuitId, Spells);

    internal static SpellListMenu ParseBody(ref PacketReader reader)
    {
        var (pursuitId, spells) = ReadCastables(ref reader);

        return new SpellListMenu { PursuitId = pursuitId, Spells = spells };
    }
}

/// <summary>
///     0x2F body - <see cref="NpcMenuType.SkillList" />: <c>[u16-BE PursuitId][u16-BE count]</c> then
///     that many <see cref="NpcMenuCastable" /> entries. Learnable skills (identical wire shape to
///     <see cref="SpellListMenu" />).
/// </summary>
public sealed record SkillListMenu : NpcMenu
{
    /// <summary>The pursuit a selection is bound to.</summary>
    public ushort PursuitId { get; init; }

    /// <summary>The skills available to learn.</summary>
    public IList<NpcMenuCastable> Skills { get; init; } = [];

    internal override bool Accepts(NpcMenuType menuType) => menuType == NpcMenuType.SkillList;

    internal override void Write(IPacketWriter writer) => WriteCastables(writer, PursuitId, Skills);

    internal static SkillListMenu ParseBody(ref PacketReader reader)
    {
        var (pursuitId, skills) = ReadCastables(ref reader);

        return new SkillListMenu { PursuitId = pursuitId, Skills = skills };
    }
}

/// <summary>0x2F body - <see cref="NpcMenuType.PlayerSpellList" />: <c>[u16-BE PursuitId]</c>. The player's own spellbook.</summary>
public sealed record PlayerSpellListMenu : NpcMenu
{
    /// <summary>The pursuit a selection is bound to.</summary>
    public ushort PursuitId { get; init; }

    internal override bool Accepts(NpcMenuType menuType) => menuType == NpcMenuType.PlayerSpellList;

    internal override void Write(IPacketWriter writer) => writer.WriteUInt16(PursuitId);

    internal static PlayerSpellListMenu ParseBody(ref PacketReader reader) => new() { PursuitId = reader.ReadUInt16() };
}

/// <summary>0x2F body - <see cref="NpcMenuType.PlayerSkillList" />: <c>[u16-BE PursuitId]</c>. The player's own skillbook.</summary>
public sealed record PlayerSkillListMenu : NpcMenu
{
    /// <summary>The pursuit a selection is bound to.</summary>
    public ushort PursuitId { get; init; }

    internal override bool Accepts(NpcMenuType menuType) => menuType == NpcMenuType.PlayerSkillList;

    internal override void Write(IPacketWriter writer) => writer.WriteUInt16(PursuitId);

    internal static PlayerSkillListMenu ParseBody(ref PacketReader reader) => new() { PursuitId = reader.ReadUInt16() };
}
