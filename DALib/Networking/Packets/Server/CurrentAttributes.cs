using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     The "Current" stat-update section of <see cref="AttributesPacket" /> (S->C 0x08) -
///     current HP and MP. Sent on damage / healing / regeneration ticks.
/// </summary>
/// <remarks>
///     Wire size: 8 bytes (two u32 BE). Present when bit 0x10 of the flag byte is set; null
///     on <see cref="AttributesPacket.Current" /> means "section absent."
/// </remarks>
public sealed record CurrentAttributes
{
    /// <summary>Current HP.</summary>
    public uint Hp { get; set; }

    /// <summary>Current MP.</summary>
    public uint Mp { get; set; }

    internal void Write(IPacketWriter writer)
    {
        writer.WriteUInt32(Hp);
        writer.WriteUInt32(Mp);
    }

    internal static CurrentAttributes Parse(ref PacketReader reader) => new()
    {
        Hp = reader.ReadUInt32(),
        Mp = reader.ReadUInt32(),
    };
}
