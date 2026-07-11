using System;
using System.Collections.Generic;
using System.IO;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x4F (S->C) - opens or updates a player-run shop (the employee/consignment shop window).
///     Every body opens with the same 7-byte header <c>[u8 0x01 gate][u32 BE ShopId][u8 subtype]</c>; the
///     subtype byte (a <see cref="PlayerShopType" />) selects the form and tail. The concrete forms are the
///     sealed records deriving from this base (<see cref="PlayerShopFullStatePacket" />,
///     <see cref="PlayerShopAddItemPacket" />, <see cref="PlayerShopRemoveItemPacket" />,
///     <see cref="PlayerShopUpdateItemPacket" />, <see cref="PlayerShopRenamePacket" />). Shop interactions
///     are answered with C->S 0x54 (<see cref="DALib.Networking.Packets.Client.PlayerShopActionPacket" />).
/// </summary>
/// <remarks>
///     The gate byte is a hard-coded <c>0x01</c>; any 0x4F whose second byte is not <c>0x01</c> is dropped,
///     and <see cref="Parse" /> rejects anything else. The <see cref="ShopId" /> is the session token: only
///     <c>FullState</c> can open a closed window, and the incremental subtypes (1-4) are honored only when a
///     shop with a matching id is already open. Subtypes 1-4 are modeled for protocol completeness; not
///     emitted by typical servers.
/// </remarks>
[ServerOpcode(ServerOpcode.PlayerShop)]
public abstract record PlayerShopPacket : ServerPacket
{
    /// <summary>The hard-coded gate byte that leads every 0x4F body; a body whose gate byte is not
    ///     <c>0x01</c> is dropped.</summary>
    public const byte GateByte = 0x01;

    /// <summary>The subtype byte that selects this variant's form.</summary>
    public abstract PlayerShopType ShopActionType { get; }

    /// <summary>The shop's id, used as a session token and matched on incremental updates (subtypes 1-4);
    ///     only <see cref="PlayerShopType.FullState" /> can open a closed window.</summary>
    public required uint ShopId { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.PlayerShop;

    /// <summary>Writes the shared <c>[u8 0x01 gate][u32 BE ShopId][u8 subtype]</c> header. Variants call
    ///     this, then append their tail.</summary>
    protected void WriteHeader(IPacketWriter writer)
    {
        writer.WriteByte(GateByte);
        writer.WriteUInt32(ShopId);
        writer.WriteByte((byte)ShopActionType);
    }

    /// <summary>
    ///     Parses a 0x4F body, validating the gate byte and dispatching on the subtype byte (the seventh body
    ///     byte) to the matching variant. This is what <see cref="ServerOpcodeAttribute" /> dispatch binds.
    /// </summary>
    public static PlayerShopPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var gate = reader.ReadByte();

        if (gate != GateByte)
            throw new InvalidDataException(
                $"0x4F PlayerShop: expected gate byte 0x{GateByte:X2}, got 0x{gate:X2}.");

        var shopId = reader.ReadUInt32();
        var subtype = (PlayerShopType)reader.ReadByte();

        return subtype switch
        {
            PlayerShopType.FullState => PlayerShopFullStatePacket.ParseBody(shopId, ref reader),
            PlayerShopType.AddItem => new PlayerShopAddItemPacket
                { ShopId = shopId, Listing = PlayerShopListing.Read(ref reader) },
            PlayerShopType.RemoveItem => new PlayerShopRemoveItemPacket
                { ShopId = shopId, ListingId = reader.ReadUInt32() },
            PlayerShopType.UpdateItem => PlayerShopUpdateItemPacket.ParseBody(shopId, ref reader),
            PlayerShopType.Rename => new PlayerShopRenamePacket
                { ShopId = shopId, ShopName = reader.ReadString8() },
            _ => throw new InvalidDataException(
                $"0x4F PlayerShop: unknown subtype 0x{(byte)subtype:X2}.")
        };
    }
}

/// <summary>
///     0x4F subtype 0 - full shop state: the shop's gold, its capacity, and every listing. The only subtype
///     that can open a closed shop window. Tail <c>[u32 BE ShopGold][u8 Capacity][u8 count][listings x count]</c>.
/// </summary>
public sealed record PlayerShopFullStatePacket : PlayerShopPacket
{
    /// <summary>The shop's current gold balance.</summary>
    public required uint ShopGold { get; init; }

    /// <summary>
    ///     The shop's maximum capacity. Must be at least the listing count; validated against the listing
    ///     count on write.
    /// </summary>
    public required byte Capacity { get; init; }

    /// <summary>
    ///     The shop's listings. Mutable so a server can accumulate entries; materialized as a list on parse.
    ///     A <see cref="PlayerShopEmptySlot" /> entry (listing id 0) is a sparse-slot marker; not emitted by
    ///     typical servers.
    /// </summary>
    public IList<PlayerShopListing> Listings { get; set; } = [];

    /// <inheritdoc />
    public override PlayerShopType ShopActionType => PlayerShopType.FullState;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        if (Listings.Count > byte.MaxValue)
            throw new InvalidOperationException(
                $"PlayerShopFullStatePacket: listing count {Listings.Count} exceeds the wire u8 limit ({byte.MaxValue}).");

        if (Capacity < Listings.Count)
            throw new InvalidOperationException(
                $"PlayerShopFullStatePacket: Capacity ({Capacity}) must be at least the listing count ({Listings.Count}).");

        WriteHeader(writer);
        writer.WriteUInt32(ShopGold);
        writer.WriteByte(Capacity);
        writer.WriteByte((byte)Listings.Count);

        foreach (var listing in Listings)
            listing.Write(writer);
    }

    internal static PlayerShopFullStatePacket ParseBody(uint shopId, ref PacketReader reader)
    {
        var shopGold = reader.ReadUInt32();
        var capacity = reader.ReadByte();
        var count = reader.ReadByte();
        var listings = new List<PlayerShopListing>(count);

        for (var i = 0; i < count; i++)
            listings.Add(PlayerShopListing.Read(ref reader));

        return new PlayerShopFullStatePacket
        {
            ShopId = shopId,
            ShopGold = shopGold,
            Capacity = capacity,
            Listings = listings
        };
    }
}

/// <summary>
///     0x4F subtype 1 - upsert a single listing into an already-open shop. Carries one standard listing
///     record. Modeled for protocol completeness; not emitted by typical servers.
/// </summary>
public sealed record PlayerShopAddItemPacket : PlayerShopPacket
{
    /// <summary>The listing to add or replace.</summary>
    public required PlayerShopListing Listing { get; init; }

    /// <inheritdoc />
    public override PlayerShopType ShopActionType => PlayerShopType.AddItem;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WriteHeader(writer);
        Listing.Write(writer);
    }
}

/// <summary>
///     0x4F subtype 2 - remove a single listing (by id) from an already-open shop. Tail
///     <c>[u32 BE ListingId]</c>. Modeled for protocol completeness; not emitted by typical servers.
/// </summary>
public sealed record PlayerShopRemoveItemPacket : PlayerShopPacket
{
    /// <summary>The id of the listing to remove.</summary>
    public required uint ListingId { get; init; }

    /// <inheritdoc />
    public override PlayerShopType ShopActionType => PlayerShopType.RemoveItem;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WriteHeader(writer);
        writer.WriteUInt32(ListingId);
    }
}

/// <summary>
///     0x4F subtype 3 - update an existing listing's details in an already-open shop, and re-key it. Carries
///     the <em>extended</em> listing record. Tail
///     <c>[u32 BE ListingId][u32 BE NewListingId][u16 BE Sprite][u8 Color][string8 Name][subcontent][u8 pad][u32 BE Price][u32 BE Unknown][u32 BE Reserved]</c>.
///     Modeled for protocol completeness; not emitted by typical servers.
/// </summary>
/// <remarks>
///     A re-key plus detail update: the listing whose id equals <see cref="ListingId" /> is matched, then
///     its id is overwritten with <see cref="NewListingId" /> along with the new
///     sprite/color/name/description/price. Differs from the standard listing in two ways: an extra
///     <c>u32</c> (<see cref="NewListingId" />) follows the id, and the sub-content string is retained where
///     the standard listing reads and discards it. This form does not carry quantity; the trailing
///     <c>u32</c>s map to price plus two carried-but-inert fields.
/// </remarks>
public sealed record PlayerShopUpdateItemPacket : PlayerShopPacket
{
    /// <summary>The id of the listing to update (the match key).</summary>
    public required uint ListingId { get; init; }

    /// <summary>The listing's <em>new</em> id after the update (this form re-keys the listing). Often equal
    ///     to <see cref="ListingId" /> for an in-place edit.</summary>
    public required uint NewListingId { get; init; }

    /// <summary>The item's display sprite, raw on-wire (<c>u16 BE</c>).</summary>
    public required ushort Sprite { get; init; }

    /// <summary>The item's color.</summary>
    public required byte Color { get; init; }

    /// <summary>The item's display name (<c>string8</c>).</summary>
    public required string Name { get; init; }

    /// <summary>Optional sub-content/description string: present on the wire as <c>[u8 1][string8]</c>, absent
    ///     as a single <c>0</c> flag byte. Unlike the standard listing, this form <em>retains</em> it.</summary>
    public string? SubContent { get; init; }

    /// <summary>The listing's price (<c>u32 BE</c>).</summary>
    public required uint Price { get; init; }

    /// <summary>A carried-but-inert <c>u32</c>, the same attribute the standard listing exposes as
    ///     <see cref="PlayerShopItemListing.Unknown" />. Preserved for round-tripping.</summary>
    public uint Unknown { get; init; }

    /// <summary>A trailing <c>u32</c> read but not otherwise used. Preserved for round-tripping.</summary>
    public uint Reserved { get; init; }

    /// <inheritdoc />
    public override PlayerShopType ShopActionType => PlayerShopType.UpdateItem;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WriteHeader(writer);
        writer.WriteUInt32(ListingId);
        writer.WriteUInt32(NewListingId);
        writer.WriteUInt16(Sprite);
        writer.WriteByte(Color);
        writer.WriteString8(Name);
        PlayerShopListing.WriteSubContent(writer, SubContent);
        writer.WriteByte(0); // padding byte
        writer.WriteUInt32(Price);
        writer.WriteUInt32(Unknown);
        writer.WriteUInt32(Reserved);
    }

    internal static PlayerShopUpdateItemPacket ParseBody(uint shopId, ref PacketReader reader)
    {
        var listingId = reader.ReadUInt32();
        var newListingId = reader.ReadUInt32();
        var sprite = reader.ReadUInt16();
        var color = reader.ReadByte();
        var name = reader.ReadString8();
        var subContent = PlayerShopListing.ReadSubContent(ref reader);
        reader.ReadByte(); // padding byte
        var price = reader.ReadUInt32();
        var unknown = reader.ReadUInt32();
        var reserved = reader.ReadUInt32();

        return new PlayerShopUpdateItemPacket
        {
            ShopId = shopId,
            ListingId = listingId,
            NewListingId = newListingId,
            Sprite = sprite,
            Color = color,
            Name = name,
            SubContent = subContent,
            Price = price,
            Unknown = unknown,
            Reserved = reserved
        };
    }
}

/// <summary>
///     0x4F subtype 4 - rename an already-open shop. Tail <c>[string8 ShopName]</c>.
/// </summary>
public sealed record PlayerShopRenamePacket : PlayerShopPacket
{
    /// <summary>The shop's new display name.</summary>
    public required string ShopName { get; init; }

    /// <inheritdoc />
    public override PlayerShopType ShopActionType => PlayerShopType.Rename;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WriteHeader(writer);
        writer.WriteString8(ShopName);
    }
}

/// <summary>
///     One entry in a <see cref="PlayerShopFullStatePacket" /> (or the single record in a
///     <see cref="PlayerShopAddItemPacket" />). A leading <c>u32</c> id discriminates the two on-wire shapes:
///     a non-zero id is a <see cref="PlayerShopItemListing" /> (a real listing), a zero id is a
///     <see cref="PlayerShopEmptySlot" /> (a sparse-slot marker; not emitted by typical servers).
/// </summary>
public abstract record PlayerShopListing
{
    /// <summary>Writes this listing in its on-wire form (including the leading id).</summary>
    internal abstract void Write(IPacketWriter writer);

    /// <summary>Reads one listing, dispatching on the leading <c>u32</c> id (0 -> empty slot).</summary>
    internal static PlayerShopListing Read(ref PacketReader reader)
    {
        var listingId = reader.ReadUInt32();

        return listingId == 0
            ? new PlayerShopEmptySlot { SparseField = reader.ReadUInt32() }
            : PlayerShopItemListing.ReadBody(listingId, ref reader);
    }

    /// <summary>Writes the optional sub-content block: <c>[u8 1][string8]</c> when present, else <c>[u8 0]</c>.
    ///     Shared with the extended record (<see cref="PlayerShopUpdateItemPacket" />).</summary>
    internal static void WriteSubContent(IPacketWriter writer, string? subContent)
    {
        if (subContent is null)
        {
            writer.WriteByte(0);
        }
        else
        {
            writer.WriteByte(1);
            writer.WriteString8(subContent);
        }
    }

    /// <summary>Reads the optional sub-content block; returns <see langword="null" /> when the flag byte is 0.</summary>
    internal static string? ReadSubContent(ref PacketReader reader)
        => reader.ReadByte() != 0 ? reader.ReadString8() : null;
}

/// <summary>
///     A filled shop listing (non-zero id). On-wire
///     <c>[u32 BE ListingId][u16 BE Sprite][u8 Color][string8 Name][subcontent][u8 pad][u32 BE Quantity][u32 BE Price][u32 BE Unknown]</c>.
/// </summary>
public sealed record PlayerShopItemListing : PlayerShopListing
{
    /// <summary>The listing's id (always non-zero for a real listing; 0 is reserved for the empty-slot marker).</summary>
    public required uint ListingId { get; init; }

    /// <summary>The item's display sprite, raw on-wire (<c>u16 BE</c>).</summary>
    public required ushort Sprite { get; init; }

    /// <summary>The item's color.</summary>
    public required byte Color { get; init; }

    /// <summary>The item's display name (<c>string8</c>).</summary>
    public required string Name { get; init; }

    /// <summary>Optional sub-content string: present on the wire as <c>[u8 1][string8]</c>, absent as a single
    ///     <c>0</c> flag byte. The standard listing reads but discards it (only the extended subtype-3 record
    ///     retains it).</summary>
    public string? SubContent { get; init; }

    /// <summary>The quantity shown in the shop grid (<c>u32 BE</c>).</summary>
    public required uint Quantity { get; init; }

    /// <summary>The listing's price (<c>u32 BE</c>).</summary>
    public required uint Price { get; init; }

    /// <summary>
    ///     A trailing <c>u32</c> that is carried but inert (no consumer reads it back). Preserved for
    ///     round-tripping.
    /// </summary>
    public uint Unknown { get; init; }

    /// <inheritdoc />
    internal override void Write(IPacketWriter writer)
    {
        writer.WriteUInt32(ListingId);
        writer.WriteUInt16(Sprite);
        writer.WriteByte(Color);
        writer.WriteString8(Name);
        WriteSubContent(writer, SubContent);
        writer.WriteByte(0); // padding byte
        writer.WriteUInt32(Quantity);
        writer.WriteUInt32(Price);
        writer.WriteUInt32(Unknown);
    }

    internal static PlayerShopItemListing ReadBody(uint listingId, ref PacketReader reader)
    {
        var sprite = reader.ReadUInt16();
        var color = reader.ReadByte();
        var name = reader.ReadString8();
        var subContent = ReadSubContent(ref reader);
        reader.ReadByte(); // padding byte
        var quantity = reader.ReadUInt32();
        var price = reader.ReadUInt32();
        var unknown = reader.ReadUInt32();

        return new PlayerShopItemListing
        {
            ListingId = listingId,
            Sprite = sprite,
            Color = color,
            Name = name,
            SubContent = subContent,
            Quantity = quantity,
            Price = price,
            Unknown = unknown
        };
    }
}

/// <summary>
///     A sparse empty-slot marker (id 0). On-wire <c>[u32 BE 0][u32 BE SparseField]</c>. This short form is
///     supported by the listing parser; not emitted by typical servers.
/// </summary>
public sealed record PlayerShopEmptySlot : PlayerShopListing
{
    /// <summary>
    ///     The empty slot's trailing <c>u32</c>, carried but inert. Preserved for round-tripping.
    /// </summary>
    public uint SparseField { get; init; }

    /// <inheritdoc />
    internal override void Write(IPacketWriter writer)
    {
        writer.WriteUInt32(0); // empty-slot marker id
        writer.WriteUInt32(SparseField);
    }
}
