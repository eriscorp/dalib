using System;
using System.IO;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x42 (S->C) - the server reports a step in a player-to-player trade. The body opens with a single
///     <c>[u8 action]</c> discriminator (an <see cref="ExchangeResponseType" />); the rest of the body
///     varies by action. The concrete forms are the sealed records deriving from this base
///     (<see cref="StartExchangeResponsePacket" />, <see cref="RequestExchangeAmountPacket" />,
///     <see cref="AddExchangeItemResponsePacket" />, <see cref="SetExchangeGoldResponsePacket" />,
///     <see cref="CancelExchangeResponsePacket" />, <see cref="AcceptExchangeResponsePacket" />). The
///     matching client packet is C->S 0x4A (<see cref="DALib.Networking.Packets.Client.ExchangePacket" />).
///     Only <see cref="ExchangeResponseType.StartExchange" /> carries a user id; actions 2-5 lead with the
///     <see cref="ExchangeSidePacket.RightSide" /> byte instead.
/// </summary>
[ServerOpcode(ServerOpcode.Exchange)]
public abstract record ExchangeResponsePacket : ServerPacket
{
    /// <summary>The action byte that leads the body and selects this variant's form.</summary>
    public abstract ExchangeResponseType ResponseType { get; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.Exchange;

    /// <summary>Writes the leading <c>[u8 action]</c> discriminator. Variants call this, then append their tail.</summary>
    protected void WriteAction(IPacketWriter writer) => writer.WriteByte((byte)ResponseType);

    /// <summary>
    ///     Parses a 0x42 body, dispatching on the leading <see cref="ExchangeResponseType" /> action byte
    ///     to the matching variant. This is what <see cref="ServerOpcodeAttribute" /> dispatch binds.
    /// </summary>
    public static ExchangeResponsePacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var action = (ExchangeResponseType)reader.ReadByte();

        return action switch
        {
            ExchangeResponseType.StartExchange => new StartExchangeResponsePacket
                { OtherUserId = reader.ReadUInt32(), OtherUserName = reader.ReadString8() },
            ExchangeResponseType.RequestAmount => new RequestExchangeAmountPacket
                { SourceSlot = reader.ReadByte() },
            ExchangeResponseType.AddItem => new AddExchangeItemResponsePacket
            {
                RightSide = reader.ReadBoolean(),
                ExchangeIndex = reader.ReadByte(),
                Sprite = reader.ReadUInt16(),
                Color = reader.ReadByte(),
                Name = reader.ReadString8()
            },
            ExchangeResponseType.SetGold => new SetExchangeGoldResponsePacket
                { RightSide = reader.ReadBoolean(), GoldAmount = reader.ReadUInt32() },
            ExchangeResponseType.Cancel => new CancelExchangeResponsePacket
                { RightSide = reader.ReadBoolean(), Message = reader.ReadString8() },
            ExchangeResponseType.Accept => new AcceptExchangeResponsePacket
                { RightSide = reader.ReadBoolean(), Message = reader.ReadString8() },
            _ => throw new InvalidDataException(
                $"0x42 Exchange: unknown response type 0x{(byte)action:X2}.")
        };
    }
}

/// <summary>
///     0x42 action 0 - a trade has opened with another player. Tail
///     <c>[u32 BE OtherUserId][string8 OtherUserName]</c>.
/// </summary>
public sealed record StartExchangeResponsePacket : ExchangeResponsePacket
{
    /// <summary>The other party's object id.</summary>
    public required uint OtherUserId { get; init; }

    /// <summary>The other party's display name.</summary>
    public required string OtherUserName { get; init; }

    /// <inheritdoc />
    public override ExchangeResponseType ResponseType => ExchangeResponseType.StartExchange;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WriteAction(writer);
        writer.WriteUInt32(OtherUserId);
        writer.WriteString8(OtherUserName);
    }
}

/// <summary>
///     0x42 action 1 - prompt the user for how many of a stackable item to offer. Tail
///     <c>[u8 SourceSlot]</c>. Answered with C->S 0x4A stage 2 (AddStackableItem).
/// </summary>
public sealed record RequestExchangeAmountPacket : ExchangeResponsePacket
{
    /// <summary>The inventory slot of the stackable item the prompt is for.</summary>
    public required byte SourceSlot { get; init; }

    /// <inheritdoc />
    public override ExchangeResponseType ResponseType => ExchangeResponseType.RequestAmount;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WriteAction(writer);
        writer.WriteByte(SourceSlot);
    }
}

/// <summary>
///     A 0x42 form whose body leads with the side byte (actions 2-5).
/// </summary>
public abstract record ExchangeSidePacket : ExchangeResponsePacket
{
    /// <summary>
    ///     Which side of the trade window the update applies to (the raw 0/1 wire byte).
    ///     Round-tripped verbatim.
    /// </summary>
    public required bool RightSide { get; init; }
}

/// <summary>
///     0x42 action 2 - an item now sits on one side of the trade window. Tail
///     <c>[bool RightSide][u8 ExchangeIndex][u16 BE Sprite][u8 Color][string8 Name]</c>.
/// </summary>
public sealed record AddExchangeItemResponsePacket : ExchangeSidePacket
{
    /// <summary>
    ///     The item's row index/slot byte on the wire. Ignored for placement (items appear in arrival
    ///     order); preserved for round-tripping.
    /// </summary>
    public required byte ExchangeIndex { get; init; }

    /// <summary>
    ///     The item's display sprite, raw on-wire. Servers commonly set the high bit (<c>0x8000</c>) as an
    ///     item convention; round-tripped verbatim, clear the high bit to recover the sprite id.
    /// </summary>
    public required ushort Sprite { get; init; }

    /// <summary>The item's color.</summary>
    public required byte Color { get; init; }

    /// <summary>The item's display name (<c>string8</c>).</summary>
    public required string Name { get; init; }

    /// <inheritdoc />
    public override ExchangeResponseType ResponseType => ExchangeResponseType.AddItem;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WriteAction(writer);
        writer.WriteBoolean(RightSide);
        writer.WriteByte(ExchangeIndex);
        writer.WriteUInt16(Sprite);
        writer.WriteByte(Color);
        writer.WriteString8(Name);
    }
}

/// <summary>
///     0x42 action 3 - the gold on one side of the trade window changed. Tail
///     <c>[bool RightSide][u32 BE GoldAmount]</c>.
/// </summary>
public sealed record SetExchangeGoldResponsePacket : ExchangeSidePacket
{
    /// <summary>The gold amount now offered on this side.</summary>
    public required uint GoldAmount { get; init; }

    /// <inheritdoc />
    public override ExchangeResponseType ResponseType => ExchangeResponseType.SetGold;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WriteAction(writer);
        writer.WriteBoolean(RightSide);
        writer.WriteUInt32(GoldAmount);
    }
}

/// <summary>
///     0x42 action 4 - the trade was cancelled. Tail <c>[bool RightSide][string8 Message]</c>. The server
///     chooses the message text.
/// </summary>
public sealed record CancelExchangeResponsePacket : ExchangeSidePacket
{
    /// <summary>The cancellation message text.</summary>
    public required string Message { get; init; }

    /// <inheritdoc />
    public override ExchangeResponseType ResponseType => ExchangeResponseType.Cancel;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WriteAction(writer);
        writer.WriteBoolean(RightSide);
        writer.WriteString8(Message);
    }
}

/// <summary>
///     0x42 action 5 - the trade was confirmed/completed. Tail <c>[bool RightSide][string8 Message]</c>.
///     The server chooses the message text.
/// </summary>
public sealed record AcceptExchangeResponsePacket : ExchangeSidePacket
{
    /// <summary>The completion message text.</summary>
    public required string Message { get; init; }

    /// <inheritdoc />
    public override ExchangeResponseType ResponseType => ExchangeResponseType.Accept;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WriteAction(writer);
        writer.WriteBoolean(RightSide);
        writer.WriteString8(Message);
    }
}
