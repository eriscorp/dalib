using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x2C (S->C) - place a skill in a skill-pane slot. Body:
///     <c>[u8 Slot][u16 Icon][string8 Name]</c>, fully consumed. Unlike 0x17
///     <see cref="AddSpellPacket" />, skills carry no use-type, prompt, or cast lines.
///     <see cref="Icon" /> is written raw (no 0x8000 offset). The inverse operation is 0x2D
///     <see cref="RemoveSkillPacket" />.
/// </summary>
[ServerOpcode(ServerOpcode.AddSkill)]
public sealed record AddSkillPacket : ServerPacket
{
    /// <summary>The skill-pane slot to place the skill in.</summary>
    public required byte Slot { get; init; }

    /// <summary>The skill-pane icon index (raw, no offset).</summary>
    public required ushort Icon { get; init; }

    /// <summary>The skill's display name.</summary>
    public required string Name { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.AddSkill;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(Slot);
        writer.WriteUInt16(Icon);
        writer.WriteString8(Name);
    }

    /// <inheritdoc />
    public static AddSkillPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var slot = reader.ReadByte();
        var icon = reader.ReadUInt16();
        var name = reader.ReadString8();

        return new AddSkillPacket
        {
            Slot = slot,
            Icon = icon,
            Name = name,
        };
    }
}
