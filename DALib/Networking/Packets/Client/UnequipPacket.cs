using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x44 (C->S) - unequip an item from an equipment slot. Body: <c>[u8 Slot]</c>. The item is
///     moved back to the first free inventory slot; an empty slot is ignored.
/// </summary>
[ClientOpcode(ClientOpcode.Unequip)]
public sealed record UnequipPacket : ClientPacket
{
    /// <summary>The equipment slot to unequip.</summary>
    public required byte Slot { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.Unequip;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => writer.WriteByte(Slot);

    /// <inheritdoc />
    public static UnequipPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        return new UnequipPacket
        {
            Slot = reader.ReadByte(),
        };
    }
}
