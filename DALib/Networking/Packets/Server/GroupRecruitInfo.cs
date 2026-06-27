using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     Group-recruitment block. Carries the recruit pitch (recruiter name, group name,
///     note), the desired level range, and a wanted-vs-current count for each of the five
///     DA classes. Embedded in <see cref="SelfProfilePacket" /> when the character is
///     actively recruiting, and carried whole as the body of a 0x63
///     <see cref="GroupRecruitInfoPacket" /> (the recruitment-info pane).
/// </summary>
/// <remarks>
///     Wire size: 3 string8 fields + 12 bytes. In <see cref="SelfProfilePacket" /> it is
///     present only when <see cref="SelfProfilePacket.Recruit" /> is non-null (the boolean
///     "has recruit" byte that precedes it is auto-derived at write-time); in
///     <see cref="GroupRecruitInfoPacket" /> it follows the <c>[04]</c> type byte directly.
/// </remarks>
public sealed record GroupRecruitInfo
{
    /// <summary>Name of the recruiting player.</summary>
    public string RecruiterName { get; set; } = string.Empty;

    /// <summary>Group's display name.</summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>Free-form recruit note.</summary>
    public string Note { get; set; } = string.Empty;

    /// <summary>Minimum eligible level.</summary>
    public byte StartingLevel { get; set; }

    /// <summary>Maximum eligible level.</summary>
    public byte EndingLevel { get; set; }

    /// <summary>How many warriors the group is looking for.</summary>
    public byte WarriorsWanted { get; set; }

    /// <summary>Warriors currently in the group.</summary>
    public byte CurrentWarriors { get; set; }

    /// <summary>How many wizards the group is looking for.</summary>
    public byte WizardsWanted { get; set; }

    /// <summary>Wizards currently in the group.</summary>
    public byte CurrentWizards { get; set; }

    /// <summary>How many rogues the group is looking for.</summary>
    public byte RoguesWanted { get; set; }

    /// <summary>Rogues currently in the group.</summary>
    public byte CurrentRogues { get; set; }

    /// <summary>How many priests the group is looking for.</summary>
    public byte PriestsWanted { get; set; }

    /// <summary>Priests currently in the group.</summary>
    public byte CurrentPriests { get; set; }

    /// <summary>How many monks the group is looking for.</summary>
    public byte MonksWanted { get; set; }

    /// <summary>Monks currently in the group.</summary>
    public byte CurrentMonks { get; set; }

    internal void Write(IPacketWriter writer)
    {
        writer.WriteString8(RecruiterName);
        writer.WriteString8(GroupName);
        writer.WriteString8(Note);
        writer.WriteByte(StartingLevel);
        writer.WriteByte(EndingLevel);
        writer.WriteByte(WarriorsWanted);
        writer.WriteByte(CurrentWarriors);
        writer.WriteByte(WizardsWanted);
        writer.WriteByte(CurrentWizards);
        writer.WriteByte(RoguesWanted);
        writer.WriteByte(CurrentRogues);
        writer.WriteByte(PriestsWanted);
        writer.WriteByte(CurrentPriests);
        writer.WriteByte(MonksWanted);
        writer.WriteByte(CurrentMonks);
    }

    internal static GroupRecruitInfo Parse(ref PacketReader reader) => new()
    {
        RecruiterName = reader.ReadString8(),
        GroupName = reader.ReadString8(),
        Note = reader.ReadString8(),
        StartingLevel = reader.ReadByte(),
        EndingLevel = reader.ReadByte(),
        WarriorsWanted = reader.ReadByte(),
        CurrentWarriors = reader.ReadByte(),
        WizardsWanted = reader.ReadByte(),
        CurrentWizards = reader.ReadByte(),
        RoguesWanted = reader.ReadByte(),
        CurrentRogues = reader.ReadByte(),
        PriestsWanted = reader.ReadByte(),
        CurrentPriests = reader.ReadByte(),
        MonksWanted = reader.ReadByte(),
        CurrentMonks = reader.ReadByte(),
    };
}
