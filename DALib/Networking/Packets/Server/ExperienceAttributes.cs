using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     The "Experience" stat-update section of <see cref="AttributesPacket" /> (S->C 0x08) -
///     six u32 progression fields. Sent on XP gain, gold change, etc.
/// </summary>
/// <remarks>
///     Wire size: 24 bytes (six u32 BE). Present when bit 0x08 of the flag byte is set; null
///     on <see cref="AttributesPacket.Experience" /> means the section is absent.
/// </remarks>
public sealed record ExperienceAttributes
{
    /// <summary>Current experience.</summary>
    public uint Experience { get; set; }

    /// <summary>Experience required for next level.</summary>
    public uint ExpToLevel { get; set; }

    /// <summary>Ability (master) experience.</summary>
    public uint AbilityExp { get; set; }

    /// <summary>Experience required for next ability (master) level.</summary>
    public uint NextAB { get; set; }

    /// <summary>"GP" field; meaning uncertain. Commonly emitted as 0.</summary>
    public uint Gp { get; set; }

    /// <summary>Carried gold.</summary>
    public uint Gold { get; set; }

    internal void Write(IPacketWriter writer)
    {
        writer.WriteUInt32(Experience);
        writer.WriteUInt32(ExpToLevel);
        writer.WriteUInt32(AbilityExp);
        writer.WriteUInt32(NextAB);
        writer.WriteUInt32(Gp);
        writer.WriteUInt32(Gold);
    }

    internal static ExperienceAttributes Parse(ref PacketReader reader) => new()
    {
        Experience = reader.ReadUInt32(),
        ExpToLevel = reader.ReadUInt32(),
        AbilityExp = reader.ReadUInt32(),
        NextAB = reader.ReadUInt32(),
        Gp = reader.ReadUInt32(),
        Gold = reader.ReadUInt32(),
    };
}
