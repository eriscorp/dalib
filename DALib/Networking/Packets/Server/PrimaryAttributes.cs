using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     The "Primary" stat-update section of <see cref="AttributesPacket" /> (S->C 0x08) -
///     character header, max HP/MP, the five primary stats, unspent stat points, and weight.
/// </summary>
/// <remarks>
///     Wire size: 28 bytes. Present when bit 0x20 of the flag byte is set; null on
///     <see cref="AttributesPacket.Primary" /> means "section absent."
/// </remarks>
public sealed record PrimaryAttributes
{
    /// <summary>
    ///     First magic header byte; typically 1. Override only for protocol probing.
    /// </summary>
    public byte Magic0 { get; set; } = 1;

    /// <summary>Second magic header byte; typically 0. See <see cref="Magic0" />.</summary>
    public byte Magic1 { get; set; }

    /// <summary>Third magic header byte; typically 0. See <see cref="Magic0" />.</summary>
    public byte Magic2 { get; set; }

    /// <summary>Character level.</summary>
    public byte Level { get; set; }

    /// <summary>Ability (master) level.</summary>
    public byte Ability { get; set; }

    /// <summary>Maximum HP.</summary>
    public uint MaxHp { get; set; }

    /// <summary>Maximum MP.</summary>
    public uint MaxMp { get; set; }

    /// <summary>Strength.</summary>
    public byte Str { get; set; }

    /// <summary>Intelligence.</summary>
    public byte Int { get; set; }

    /// <summary>Wisdom.</summary>
    public byte Wis { get; set; }

    /// <summary>Constitution.</summary>
    public byte Con { get; set; }

    /// <summary>Dexterity.</summary>
    public byte Dex { get; set; }

    /// <summary>
    ///     Unspent stat points available to allocate. The "has unspent" flag byte that precedes
    ///     this on the wire is auto-derived: 1 if <see cref="UnspentPoints" /> is non-zero, 0
    ///     otherwise.
    /// </summary>
    public byte UnspentPoints { get; set; }

    /// <summary>Maximum carriable weight.</summary>
    public ushort MaxWeight { get; set; }

    /// <summary>Currently carried weight.</summary>
    public ushort CurrentWeight { get; set; }

    /// <summary>
    ///     Trailing 4-byte field; typically 0. Override only for protocol probing.
    /// </summary>
    public uint UnknownTrailing { get; set; }

    internal void Write(IPacketWriter writer)
    {
        writer.WriteByte(Magic0);
        writer.WriteByte(Magic1);
        writer.WriteByte(Magic2);
        writer.WriteByte(Level);
        writer.WriteByte(Ability);
        writer.WriteUInt32(MaxHp);
        writer.WriteUInt32(MaxMp);
        writer.WriteByte(Str);
        writer.WriteByte(Int);
        writer.WriteByte(Wis);
        writer.WriteByte(Con);
        writer.WriteByte(Dex);
        writer.WriteBoolean(UnspentPoints > 0);
        writer.WriteByte(UnspentPoints);
        writer.WriteUInt16(MaxWeight);
        writer.WriteUInt16(CurrentWeight);
        writer.WriteUInt32(UnknownTrailing);
    }

    internal static PrimaryAttributes Parse(ref PacketReader reader)
    {
        var magic0 = reader.ReadByte();
        var magic1 = reader.ReadByte();
        var magic2 = reader.ReadByte();
        var level = reader.ReadByte();
        var ability = reader.ReadByte();
        var maxHp = reader.ReadUInt32();
        var maxMp = reader.ReadUInt32();
        var str = reader.ReadByte();
        var @int = reader.ReadByte();
        var wis = reader.ReadByte();
        var con = reader.ReadByte();
        var dex = reader.ReadByte();
        _ = reader.ReadByte();
        var unspentPoints = reader.ReadByte();
        var maxWeight = reader.ReadUInt16();
        var currentWeight = reader.ReadUInt16();
        var unknownTrailing = reader.ReadUInt32();

        return new PrimaryAttributes
        {
            Magic0 = magic0,
            Magic1 = magic1,
            Magic2 = magic2,
            Level = level,
            Ability = ability,
            MaxHp = maxHp,
            MaxMp = maxMp,
            Str = str,
            Int = @int,
            Wis = wis,
            Con = con,
            Dex = dex,
            UnspentPoints = unspentPoints,
            MaxWeight = maxWeight,
            CurrentWeight = currentWeight,
            UnknownTrailing = unknownTrailing,
        };
    }
}
