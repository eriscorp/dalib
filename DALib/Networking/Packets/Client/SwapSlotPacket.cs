using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x30 (C->S) - swap two slots within a panel (inventory, spellbook, or skillbook). Body:
///     <c>[u8 Window][u8 Slot1][u8 Slot2]</c>.
/// </summary>
/// <remarks>
///     <see cref="Window" /> selects the panel: <c>0</c> inventory, <c>1</c> spellbook, <c>2</c>
///     skillbook (values above 2 are ignored). <see cref="Slot1" /> and <see cref="Slot2" /> are the
///     1-based slots whose contents are exchanged.
/// </remarks>
[ClientOpcode(ClientOpcode.SwapSlot)]
public sealed record SwapSlotPacket : ClientPacket
{
    /// <summary>The panel: 0 inventory, 1 spellbook, 2 skillbook.</summary>
    public required byte Window { get; init; }

    /// <summary>The first slot to swap (1-based).</summary>
    public required byte Slot1 { get; init; }

    /// <summary>The second slot to swap (1-based).</summary>
    public required byte Slot2 { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.SwapSlot;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(Window);
        writer.WriteByte(Slot1);
        writer.WriteByte(Slot2);
    }

    /// <inheritdoc />
    public static SwapSlotPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var window = reader.ReadByte();
        var slot1 = reader.ReadByte();
        var slot2 = reader.ReadByte();

        return new SwapSlotPacket
        {
            Window = window,
            Slot1 = slot1,
            Slot2 = slot2,
        };
    }
}
