using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x24 (C->S) - drop an amount of gold onto a tile. The body is
///     <c>[u32 BE Amount][u16 BE X][u16 BE Y]</c>.
/// </summary>
/// <remarks>
///     The server validates that the player holds at least <see cref="Amount" />, that the target
///     tile is within drop distance, in bounds, and passable. Coordinates are unsigned tile indices.
/// </remarks>
[ClientOpcode(ClientOpcode.DropGold)]
public sealed record DropGoldPacket : ClientPacket
{
    /// <summary>Amount of gold to drop.</summary>
    public required uint Amount { get; init; }

    /// <summary>X of the tile to drop onto.</summary>
    public required ushort X { get; init; }

    /// <summary>Y of the tile to drop onto.</summary>
    public required ushort Y { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.DropGold;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteUInt32(Amount);
        writer.WriteUInt16(X);
        writer.WriteUInt16(Y);
    }

    /// <inheritdoc />
    public static DropGoldPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var amount = reader.ReadUInt32();
        var x = reader.ReadUInt16();
        var y = reader.ReadUInt16();

        return new DropGoldPacket
        {
            Amount = amount,
            X = x,
            Y = y,
        };
    }
}
