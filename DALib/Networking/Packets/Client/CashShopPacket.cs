using System;
using System.IO;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x6C (C->S) - drive the cash-shop / item-mall window. The body opens with a
///     <see cref="CashShopActionType" /> subtype byte that selects the form and any tail. The concrete forms
///     are the sealed records deriving from this base (<see cref="OpenCashShopPacket" />,
///     <see cref="PurchaseCashShopItemPacket" />, <see cref="CloseCashShopPacket" />).
/// </summary>
/// <remarks>
///     Binary-verified send (three construction sites); not emitted by Hybrasyl or Chaos. Present in both
///     the USDA and LoD client binaries. Modeled for wire completeness.
/// </remarks>
[ClientOpcode(ClientOpcode.CashShop)]
public abstract record CashShopPacket : ClientPacket
{
    /// <summary>The subtype byte that selects this variant's form.</summary>
    public abstract CashShopActionType ActionType { get; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.CashShop;

    /// <summary>Writes the leading <c>[u8 subtype]</c>. Variants call this, then append their tail.</summary>
    protected void WritePrefix(IPacketWriter writer) => writer.WriteByte((byte)ActionType);

    /// <summary>
    ///     Parses a 0x6C body, dispatching on the leading subtype byte to the matching variant. This is the
    ///     standalone entry and what <see cref="ClientOpcodeAttribute" /> dispatch binds.
    /// </summary>
    public static CashShopPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var type = (CashShopActionType)reader.ReadByte();

        return type switch
        {
            CashShopActionType.Open => new OpenCashShopPacket(),
            CashShopActionType.Purchase => new PurchaseCashShopItemPacket
                { Slot = reader.ReadByte(), ItemId = reader.ReadUInt32() },
            CashShopActionType.Close => new CloseCashShopPacket(),
            _ => throw new InvalidDataException(
                $"0x6C CashShop: unknown subtype 0x{(byte)type:X2}.")
        };
    }
}

/// <summary>
///     0x6C subtype 0 - open/initialize the cash-shop window. Prefix only.
/// </summary>
public sealed record OpenCashShopPacket : CashShopPacket
{
    /// <inheritdoc />
    public override CashShopActionType ActionType => CashShopActionType.Open;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => WritePrefix(writer);
}

/// <summary>
///     0x6C subtype 1 - purchase an item from the cash shop. Tail <c>[u8 Slot][u32 BE ItemId]</c>.
/// </summary>
public sealed record PurchaseCashShopItemPacket : CashShopPacket
{
    /// <summary>The shop slot being purchased.</summary>
    public required byte Slot { get; init; }

    /// <summary>The purchased item's id.</summary>
    public required uint ItemId { get; init; }

    /// <inheritdoc />
    public override CashShopActionType ActionType => CashShopActionType.Purchase;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WritePrefix(writer);
        writer.WriteByte(Slot);
        writer.WriteUInt32(ItemId);
    }
}

/// <summary>
///     0x6C subtype 2 - close the cash-shop window. Prefix only.
/// </summary>
public sealed record CloseCashShopPacket : CashShopPacket
{
    /// <inheritdoc />
    public override CashShopActionType ActionType => CashShopActionType.Close;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => WritePrefix(writer);
}
