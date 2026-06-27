using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x18 (S->C) - clear a spell-pane slot. The body is <c>[u8 Slot]</c>, fully consumed.
/// </summary>
/// <remarks>
///     The inverse operation is 0x17 <see cref="AddSpellPacket" />.
/// </remarks>
[ServerOpcode(ServerOpcode.RemoveSpell)]
public sealed record RemoveSpellPacket : ServerPacket
{
    /// <summary>The spell-pane slot to clear.</summary>
    public required byte Slot { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.RemoveSpell;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => writer.WriteByte(Slot);

    /// <inheritdoc />
    public static RemoveSpellPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var slot = reader.ReadByte();

        return new RemoveSpellPacket
        {
            Slot = slot,
        };
    }
}
