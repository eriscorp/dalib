using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x3E (C->S) - use a skill from a skill-book slot. Body: <c>[u8 Slot]</c>. The C->S analogue of
///     <see cref="UseItemPacket" /> (0x1C) and <see cref="UseSpellPacket" /> (0x0F); unlike UseSpell,
///     a skill carries no extra arguments.
/// </summary>
[ClientOpcode(ClientOpcode.UseSkill)]
public sealed record UseSkillPacket : ClientPacket
{
    /// <summary>The skill-book slot of the skill to use.</summary>
    public required byte Slot { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.UseSkill;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => writer.WriteByte(Slot);

    /// <inheritdoc />
    public static UseSkillPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        return new UseSkillPacket
        {
            Slot = reader.ReadByte(),
        };
    }
}
