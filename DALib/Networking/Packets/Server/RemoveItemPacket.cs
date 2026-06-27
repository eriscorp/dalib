using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x10 (S->C) - clear an inventory-pane slot. The canonical body is <c>[u8 Slot]</c>; some
///     servers append three trailing zero bytes that carry no meaning.
/// </summary>
/// <remarks>
///     The optional <see cref="TrailingSlack" /> round-trips either form byte-faithfully. The
///     inverse operation is 0x0F <see cref="AddItemPacket" />.
/// </remarks>
[ServerOpcode(ServerOpcode.RemoveItem)]
public sealed record RemoveItemPacket : ServerPacket
{
    /// <summary>The inventory-pane slot to clear.</summary>
    public required byte Slot { get; init; }

    /// <summary>
    ///     Optional trailing bytes that carry no meaning, kept for byte-faithful round-trips.
    ///     <see langword="null" /> (the default) writes nothing; some servers emit three zero bytes.
    /// </summary>
    public byte[]? TrailingSlack { get; set; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.RemoveItem;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(Slot);

        if (TrailingSlack is { Length: > 0 })
            writer.WriteBytes(TrailingSlack);
    }

    /// <inheritdoc />
    public static RemoveItemPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var slot = reader.ReadByte();

        var trailingSlack = reader.Remaining.Length > 0 ? reader.Remaining.ToArray() : null;

        return new RemoveItemPacket
        {
            Slot = slot,
            TrailingSlack = trailingSlack,
        };
    }
}
