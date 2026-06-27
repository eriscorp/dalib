using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x3F (S->C) - start a cooldown sweep on a spell- or skill-pane slot (the radial "recharge"
///     animation). The body is <c>[u8 IsSkill][u8 Slot][u32 BE Seconds]</c>: <see cref="Slot" /> is the
///     pane slot to sweep, <see cref="Seconds" /> the cooldown duration in whole seconds.
/// </summary>
[ServerOpcode(ServerOpcode.Cooldown)]
public sealed record CooldownPacket : ServerPacket
{
    /// <summary>True for a skill-pane slot, false for a spell-pane slot.</summary>
    public required bool IsSkill { get; init; }

    /// <summary>The pane slot to apply the cooldown sweep to.</summary>
    public required byte Slot { get; init; }

    /// <summary>The cooldown duration in whole seconds.</summary>
    public required uint Seconds { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.Cooldown;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteBoolean(IsSkill);
        writer.WriteByte(Slot);
        writer.WriteUInt32(Seconds);
    }

    /// <inheritdoc />
    public static CooldownPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var isSkill = reader.ReadBoolean();
        var slot = reader.ReadByte();
        var seconds = reader.ReadUInt32();

        return new CooldownPacket
        {
            IsSkill = isSkill,
            Slot = slot,
            Seconds = seconds,
        };
    }
}
