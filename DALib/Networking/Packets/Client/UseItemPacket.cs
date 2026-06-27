using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x1C (C->S) - use (invoke) an item from an inventory slot. Body: <c>[u8 Slot]</c>, a 1-based
///     inventory slot (0 is invalid). The server resolves the item and applies its behavior:
///     consumables are invoked and decremented, equipment is equipped, unusable items are refused.
/// </summary>
[ClientOpcode(ClientOpcode.UseItem)]
public sealed record UseItemPacket : ClientPacket
{
    /// <summary>The 1-based inventory slot of the item to use (0 is invalid).</summary>
    public required byte Slot { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.UseItem;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => writer.WriteByte(Slot);

    /// <inheritdoc />
    public static UseItemPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        return new UseItemPacket
        {
            Slot = reader.ReadByte(),
        };
    }
}
