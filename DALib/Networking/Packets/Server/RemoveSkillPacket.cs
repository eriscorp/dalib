using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x2D (S->C) - clear a skill-pane slot. The body is <c>[u8 Slot]</c>, fully consumed.
/// </summary>
/// <remarks>
///     The inverse operation is 0x2C <see cref="AddSkillPacket" />.
/// </remarks>
[ServerOpcode(ServerOpcode.RemoveSkill)]
public sealed record RemoveSkillPacket : ServerPacket
{
    /// <summary>The skill-pane slot to clear.</summary>
    public required byte Slot { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.RemoveSkill;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => writer.WriteByte(Slot);

    /// <inheritdoc />
    public static RemoveSkillPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var slot = reader.ReadByte();

        return new RemoveSkillPacket
        {
            Slot = slot,
        };
    }
}
