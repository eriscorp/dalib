using System;
using System.IO;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x54 (C->S) - an action taken in an open player-run shop (the employee/consignment shop window). The
///     C->S counterpart to S->C 0x4F <see cref="DALib.Networking.Packets.Server.PlayerShopPacket" />: the
///     server pushes the shop's state with 0x4F, and the client drives it with 0x54. The body opens with a
///     shared prefix <c>[u8 0x01 gate][u32 BE ShopId][u8 action]</c>; the action byte (a
///     <see cref="PlayerShopActionType" />) selects the form and any tail. The concrete forms are the sealed
///     records deriving from this base (<see cref="WithdrawShopGoldPacket" />, <see cref="AddShopItemPacket" />,
///     <see cref="UpdateShopListingPacket" />, <see cref="RemoveShopListingPacket" />,
///     <see cref="CloseShopPacket" />, <see cref="ShopOpenedPacket" />).
/// </summary>
/// <remarks>
///     <para>
///         The gate byte is a hard-coded <c>0x01</c> (the same gate the S->C 0x4F validator checks);
///         <see cref="Parse" /> rejects anything else. The <see cref="ShopId" /> is the session token echoed
///         from the 0x4F that opened the window.
///     </para>
///     <para>
///         No reference server parses 0x54 today (Hybrasyl has no handler), so every form here is modeled for
///         wire completeness. The wire <em>structure</em> of each form is binary-verified in both the 7.41 and
///         5.51 retail clients, where the layouts are byte-for-byte identical; the field <em>semantics</em>
///         are inferred from the client's builder call sites and the traced 0x4F consumer, and the fields that
///         are not pinned to the binary say so.
///     </para>
/// </remarks>
[ClientOpcode(ClientOpcode.PlayerShopAction)]
public abstract record PlayerShopActionPacket : ClientPacket
{
    /// <summary>The hard-coded gate byte that leads every 0x54 body; the server drops a body whose gate byte
    ///     is not <c>0x01</c>.</summary>
    public const byte GateByte = 0x01;

    /// <summary>The action byte that selects this variant's form.</summary>
    public abstract PlayerShopActionType ShopActionType { get; }

    /// <summary>The shop's id, the session token echoed from the S->C 0x4F that opened the window.</summary>
    public required uint ShopId { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.PlayerShopAction;

    /// <summary>Writes the shared <c>[u8 0x01 gate][u32 BE ShopId][u8 action]</c> prefix. Variants call this,
    ///     then append their tail.</summary>
    protected void WritePrefix(IPacketWriter writer)
    {
        writer.WriteByte(GateByte);
        writer.WriteUInt32(ShopId);
        writer.WriteByte((byte)ShopActionType);
    }

    /// <summary>
    ///     Parses a 0x54 body, validating the gate byte and dispatching on the action byte to the matching
    ///     variant. This is the standalone entry and what <see cref="ClientOpcodeAttribute" /> dispatch binds.
    /// </summary>
    public static PlayerShopActionPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var gate = reader.ReadByte();

        if (gate != GateByte)
            throw new InvalidDataException(
                $"0x54 PlayerShopAction: expected gate byte 0x{GateByte:X2}, got 0x{gate:X2}.");

        var shopId = reader.ReadUInt32();
        var action = (PlayerShopActionType)reader.ReadByte();

        return action switch
        {
            PlayerShopActionType.WithdrawGold => new WithdrawShopGoldPacket
                { ShopId = shopId, Selector = reader.ReadByte(), Amount = reader.ReadUInt32() },
            PlayerShopActionType.AddItem => new AddShopItemPacket
            {
                ShopId = shopId,
                Operand1 = reader.ReadUInt32(),
                Operand2 = reader.ReadUInt32(),
                Reserved1 = reader.ReadUInt16(),
                Reserved2 = reader.ReadUInt16()
            },
            PlayerShopActionType.UpdateListing => new UpdateShopListingPacket
            {
                ShopId = shopId,
                ListingId = reader.ReadUInt32(),
                Price = reader.ReadUInt32(),
                Count = reader.ReadUInt32()
            },
            PlayerShopActionType.RemoveListing => new RemoveShopListingPacket
                { ShopId = shopId, ListingId = reader.ReadUInt32() },
            PlayerShopActionType.CloseShop => new CloseShopPacket { ShopId = shopId },
            PlayerShopActionType.ShopOpened => new ShopOpenedPacket { ShopId = shopId },
            _ => throw new InvalidDataException(
                $"0x54 PlayerShopAction: unknown action 0x{(byte)action:X2}.")
        };
    }
}

/// <summary>
///     0x54 action 0 - withdraw gold from the shop's till. Tail <c>[u8 Selector][u32 BE Amount]</c>.
/// </summary>
public sealed record WithdrawShopGoldPacket : PlayerShopActionPacket
{
    /// <summary>
    ///     A leading selector byte the client sources from the low byte of a UI value. Its exact role is not
    ///     pinned to the binary; preserved for round-tripping.
    /// </summary>
    public byte Selector { get; init; }

    /// <summary>The amount of gold to withdraw (inferred - the withdraw dialog's <c>u32</c> operand).</summary>
    public required uint Amount { get; init; }

    /// <inheritdoc />
    public override PlayerShopActionType ShopActionType => PlayerShopActionType.WithdrawGold;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WritePrefix(writer);
        writer.WriteByte(Selector);
        writer.WriteUInt32(Amount);
    }
}

/// <summary>
///     0x54 action 1 - add an item to the shop. Tail
///     <c>[u32 BE Operand1][u32 BE Operand2][u16 BE 0][u16 BE 0]</c>.
/// </summary>
/// <remarks>
///     The two u32 operands carry the added item's reference and terms (an item/slot reference and a
///     price/quantity, in some order); their exact roles are not pinned to the binary. The two trailing u16s
///     are always sent as <c>0</c> by the client - modeled as settable <see cref="Reserved1" /> /
///     <see cref="Reserved2" /> so a non-zero value round-trips.
/// </remarks>
public sealed record AddShopItemPacket : PlayerShopActionPacket
{
    /// <summary>First add-item operand (an item/slot reference or its terms; exact role not pinned).</summary>
    public required uint Operand1 { get; init; }

    /// <summary>Second add-item operand (an item/slot reference or its terms; exact role not pinned).</summary>
    public required uint Operand2 { get; init; }

    /// <summary>A trailing u16 the client always sends as 0. Preserved for round-tripping.</summary>
    public ushort Reserved1 { get; init; }

    /// <summary>A trailing u16 the client always sends as 0. Preserved for round-tripping.</summary>
    public ushort Reserved2 { get; init; }

    /// <inheritdoc />
    public override PlayerShopActionType ShopActionType => PlayerShopActionType.AddItem;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WritePrefix(writer);
        writer.WriteUInt32(Operand1);
        writer.WriteUInt32(Operand2);
        writer.WriteUInt16(Reserved1);
        writer.WriteUInt16(Reserved2);
    }
}

/// <summary>
///     0x54 action 2 - update a listing's terms. Tail <c>[u32 BE ListingId][u32 BE Price][u32 BE Count]</c>.
///     The client sources <see cref="Price" /> and <see cref="Count" /> from the item-property dialog's
///     edited fields.
/// </summary>
public sealed record UpdateShopListingPacket : PlayerShopActionPacket
{
    /// <summary>The id of the listing to update.</summary>
    public required uint ListingId { get; init; }

    /// <summary>The listing's new price.</summary>
    public required uint Price { get; init; }

    /// <summary>The listing's new count/quantity.</summary>
    public required uint Count { get; init; }

    /// <inheritdoc />
    public override PlayerShopActionType ShopActionType => PlayerShopActionType.UpdateListing;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WritePrefix(writer);
        writer.WriteUInt32(ListingId);
        writer.WriteUInt32(Price);
        writer.WriteUInt32(Count);
    }
}

/// <summary>
///     0x54 action 3 - remove a listing from the shop, by id. Tail <c>[u32 BE ListingId]</c>.
/// </summary>
public sealed record RemoveShopListingPacket : PlayerShopActionPacket
{
    /// <summary>The id of the listing to remove.</summary>
    public required uint ListingId { get; init; }

    /// <inheritdoc />
    public override PlayerShopActionType ShopActionType => PlayerShopActionType.RemoveListing;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WritePrefix(writer);
        writer.WriteUInt32(ListingId);
    }
}

/// <summary>
///     0x54 action 4 - close the shop window (the Dismiss button). Prefix only.
/// </summary>
public sealed record CloseShopPacket : PlayerShopActionPacket
{
    /// <inheritdoc />
    public override PlayerShopActionType ShopActionType => PlayerShopActionType.CloseShop;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => WritePrefix(writer);
}

/// <summary>
///     0x54 action 5 - the shop-opened handshake, auto-sent from the shop window's constructor when it opens.
///     Prefix only.
/// </summary>
public sealed record ShopOpenedPacket : PlayerShopActionPacket
{
    /// <inheritdoc />
    public override PlayerShopActionType ShopActionType => PlayerShopActionType.ShopOpened;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => WritePrefix(writer);
}
