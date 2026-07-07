using System;
using DALib.Enums;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x38 (S->C) - clear an equipment-pane slot. The body is <c>[u8 Slot]</c>, fully consumed.
/// </summary>
/// <remarks>
///     Slot numbering is shared with 0x37 <see cref="AddEquipmentPacket" /> (1-18; see
///     <see cref="EquipmentSlot" /> for the slot map).
/// </remarks>
[ServerOpcode(ServerOpcode.RemoveEquipment)]
public sealed record RemoveEquipmentPacket : ServerPacket
{
    /// <summary>The equipment-pane slot to clear (1-18, see <see cref="EquipmentSlot" />).</summary>
    public required EquipmentSlot Slot { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.RemoveEquipment;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => writer.WriteByte((byte)Slot);

    /// <inheritdoc />
    public static RemoveEquipmentPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var slot = (EquipmentSlot)reader.ReadByte();

        return new RemoveEquipmentPacket
        {
            Slot = slot,
        };
    }
}
