using System;
using System.IO;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x4A (C->S) - a step in a player-to-player trade. The body opens with a shared prefix
///     <c>[u8 stage][u32 BE OtherUserId]</c>; the stage byte (an <see cref="ExchangeRequestType" />)
///     selects the form and any tail. The concrete forms are the sealed records deriving from this
///     base (<see cref="StartExchangePacket" />, <see cref="AddExchangeItemPacket" />,
///     <see cref="AddExchangeStackableItemPacket" />, <see cref="SetExchangeGoldPacket" />,
///     <see cref="CancelExchangePacket" />, <see cref="AcceptExchangePacket" />). The
///     <c>u32 OtherUserId</c> follows the stage byte on every stage: the trade target on
///     <see cref="ExchangeRequestType.StartExchange" />, the partner on the rest. Gold is a
///     <c>u32</c>.
/// </summary>
[ClientOpcode(ClientOpcode.Exchange)]
public abstract record ExchangePacket : ClientPacket
{
    /// <summary>The stage byte that leads the body and selects this variant's form.</summary>
    public abstract ExchangeRequestType RequestType { get; }

    /// <summary>
    ///     The other party's object id - the trade target on
    ///     <see cref="ExchangeRequestType.StartExchange" />, the partner on every other stage.
    /// </summary>
    public required uint OtherUserId { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.Exchange;

    /// <summary>Writes the shared <c>[u8 stage][u32 OtherUserId]</c> prefix. Variants call this,
    ///     then append their tail.</summary>
    protected void WritePrefix(IPacketWriter writer)
    {
        writer.WriteByte((byte)RequestType);
        writer.WriteUInt32(OtherUserId);
    }

    /// <summary>
    ///     Parses a 0x4A body, dispatching on the leading <see cref="ExchangeRequestType" /> stage
    ///     byte to the matching variant. This is the standalone entry and what
    ///     <see cref="ClientOpcodeAttribute" /> dispatch binds.
    /// </summary>
    public static ExchangePacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var type = (ExchangeRequestType)reader.ReadByte();
        var otherUserId = reader.ReadUInt32();

        return type switch
        {
            ExchangeRequestType.StartExchange => new StartExchangePacket { OtherUserId = otherUserId },
            ExchangeRequestType.AddItem => new AddExchangeItemPacket
                { OtherUserId = otherUserId, SourceSlot = reader.ReadByte() },
            ExchangeRequestType.AddStackableItem => new AddExchangeStackableItemPacket
                { OtherUserId = otherUserId, SourceSlot = reader.ReadByte(), ItemCount = reader.ReadByte() },
            ExchangeRequestType.SetGold => new SetExchangeGoldPacket
                { OtherUserId = otherUserId, GoldAmount = reader.ReadUInt32() },
            ExchangeRequestType.Cancel => new CancelExchangePacket { OtherUserId = otherUserId },
            ExchangeRequestType.Accept => new AcceptExchangePacket { OtherUserId = otherUserId },
            _ => throw new InvalidDataException(
                $"0x4A Exchange: unknown request type 0x{(byte)type:X2}.")
        };
    }
}

/// <summary>
///     0x4A stage 0 - begin a trade with a target player. Prefix only;
///     <see cref="ExchangePacket.OtherUserId" /> is the target.
/// </summary>
public sealed record StartExchangePacket : ExchangePacket
{
    /// <inheritdoc />
    public override ExchangeRequestType RequestType => ExchangeRequestType.StartExchange;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => WritePrefix(writer);
}

/// <summary>
///     0x4A stage 1 - offer an item from an inventory slot. Tail <c>[u8 SourceSlot]</c>. If the
///     item stacks, the server answers with a quantity prompt and a follow-up
///     <see cref="AddExchangeStackableItemPacket" /> supplies the count.
/// </summary>
public sealed record AddExchangeItemPacket : ExchangePacket
{
    /// <summary>The inventory slot of the offered item.</summary>
    public required byte SourceSlot { get; init; }

    /// <inheritdoc />
    public override ExchangeRequestType RequestType => ExchangeRequestType.AddItem;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WritePrefix(writer);
        writer.WriteByte(SourceSlot);
    }
}

/// <summary>
///     0x4A stage 2 - offer a counted quantity of a stackable item. Tail
///     <c>[u8 SourceSlot][u8 ItemCount]</c>.
/// </summary>
public sealed record AddExchangeStackableItemPacket : ExchangePacket
{
    /// <summary>The inventory slot of the offered stack.</summary>
    public required byte SourceSlot { get; init; }

    /// <summary>How many to offer.</summary>
    public required byte ItemCount { get; init; }

    /// <inheritdoc />
    public override ExchangeRequestType RequestType => ExchangeRequestType.AddStackableItem;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WritePrefix(writer);
        writer.WriteByte(SourceSlot);
        writer.WriteByte(ItemCount);
    }
}

/// <summary>
///     0x4A stage 3 - set the gold offered. Tail <c>[u32 BE GoldAmount]</c>.
/// </summary>
public sealed record SetExchangeGoldPacket : ExchangePacket
{
    /// <summary>The amount of gold offered.</summary>
    public required uint GoldAmount { get; init; }

    /// <inheritdoc />
    public override ExchangeRequestType RequestType => ExchangeRequestType.SetGold;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WritePrefix(writer);
        writer.WriteUInt32(GoldAmount);
    }
}

/// <summary>
///     0x4A stage 4 - cancel the trade. Prefix only. May also appear as an auto-decline of an
///     invite, carrying the requestor's id.
/// </summary>
public sealed record CancelExchangePacket : ExchangePacket
{
    /// <inheritdoc />
    public override ExchangeRequestType RequestType => ExchangeRequestType.Cancel;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => WritePrefix(writer);
}

/// <summary>
///     0x4A stage 5 - confirm/accept the trade. Prefix only.
/// </summary>
public sealed record AcceptExchangePacket : ExchangePacket
{
    /// <inheritdoc />
    public override ExchangeRequestType RequestType => ExchangeRequestType.Accept;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => WritePrefix(writer);
}
