using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     The "Secondary" stat-update section of <see cref="AttributesPacket" /> (S->C 0x08) -
///     elemental resistances, mail status, AC, combat ratings, and several bytes with no
///     known meaning.
/// </summary>
/// <remarks>
///     Wire size: 13 bytes. Present when bit 0x04 of the flag byte is set; null on
///     <see cref="AttributesPacket.Secondary" /> means "section absent." <see cref="MailStatus" />
///     is nibble-packed: low nibble = parcel pending (0x01), high nibble = mail pending (0x10).
/// </remarks>
public sealed record SecondaryAttributes
{
    /// <summary>Mail-status value indicating a pending parcel; low nibble of <see cref="MailStatus" />.</summary>
    public const byte MailFlagParcel = 0x01;

    /// <summary>Mail-status value indicating pending mail; high nibble of <see cref="MailStatus" />.</summary>
    public const byte MailFlagMail = 0x10;

    /// <summary>Combined-flag convenience for "both parcel and mail pending."</summary>
    public const byte MailFlagBoth = MailFlagParcel | MailFlagMail;

    /// <summary>Blinded-condition indicator: 0x08 when blinded, 0 otherwise.</summary>
    public const byte BlindedActive = 0x08;

    /// <summary>No known meaning; emit 0.</summary>
    public byte Unknown1 { get; set; }

    /// <summary>
    ///     Blinded-condition byte. <see cref="BlindedActive" /> (0x08) when the character is
    ///     blinded, 0 otherwise.
    /// </summary>
    public byte Blinded { get; set; }

    /// <summary>No known meaning; emit 0.</summary>
    public byte Unknown2 { get; set; }

    /// <summary>No known meaning; emit 0.</summary>
    public byte Unknown3 { get; set; }

    /// <summary>No known meaning; emit 0.</summary>
    public byte Unknown4 { get; set; }

    /// <summary>
    ///     Nibble-packed mail-status byte. Low nibble = <see cref="MailFlagParcel" />, high
    ///     nibble = <see cref="MailFlagMail" />.
    /// </summary>
    public byte MailStatus { get; set; }

    /// <summary>Offensive elemental affinity.</summary>
    public byte OffensiveElement { get; set; }

    /// <summary>Defensive elemental affinity.</summary>
    public byte DefensiveElement { get; set; }

    /// <summary>Magic-resistance rating.</summary>
    public byte MrRating { get; set; }

    /// <summary>"Fast move" byte. Meaning unverified; emit 0.</summary>
    public byte FastMove { get; set; }

    /// <summary>
    ///     Armor class (signed). Lower values indicate better armor in the DA tradition.
    /// </summary>
    public sbyte Ac { get; set; }

    /// <summary>Damage rating.</summary>
    public byte DmgRating { get; set; }

    /// <summary>Hit rating.</summary>
    public byte HitRating { get; set; }

    internal void Write(IPacketWriter writer)
    {
        writer.WriteByte(Unknown1);
        writer.WriteByte(Blinded);
        writer.WriteByte(Unknown2);
        writer.WriteByte(Unknown3);
        writer.WriteByte(Unknown4);
        writer.WriteByte(MailStatus);
        writer.WriteByte(OffensiveElement);
        writer.WriteByte(DefensiveElement);
        writer.WriteByte(MrRating);
        writer.WriteByte(FastMove);
        writer.WriteByte(unchecked((byte)Ac));
        writer.WriteByte(DmgRating);
        writer.WriteByte(HitRating);
    }

    internal static SecondaryAttributes Parse(ref PacketReader reader) => new()
    {
        Unknown1 = reader.ReadByte(),
        Blinded = reader.ReadByte(),
        Unknown2 = reader.ReadByte(),
        Unknown3 = reader.ReadByte(),
        Unknown4 = reader.ReadByte(),
        MailStatus = reader.ReadByte(),
        OffensiveElement = reader.ReadByte(),
        DefensiveElement = reader.ReadByte(),
        MrRating = reader.ReadByte(),
        FastMove = reader.ReadByte(),
        Ac = reader.ReadSByte(),
        DmgRating = reader.ReadByte(),
        HitRating = reader.ReadByte(),
    };
}
