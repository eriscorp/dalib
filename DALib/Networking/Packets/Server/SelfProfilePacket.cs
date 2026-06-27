using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x39 (S->C) - the player's own profile and stats panel content: nation, guild
///     affiliation, group status, class, legend marks.
/// </summary>
/// <remarks>
///     <see cref="Parse" /> silently ignores any trailing bytes after the legend loop, so it
///     round-trips bodies that carry extra slack; <see cref="WriteBody" /> emits no slack.
/// </remarks>
[ServerOpcode(ServerOpcode.SelfProfile)]
public sealed record SelfProfilePacket : ServerPacket
{
    /// <summary>
    ///     Conventional <see cref="GroupStatusText" /> value indicating "not grouped". Any string is
    ///     accepted; it is rendered verbatim into the group-status text widget.
    /// </summary>
    public const string GroupStatusSolo = "Adventuring Alone";

    /// <summary>
    ///     Hard wire limit on the number of legend marks; the count is a u8.
    ///     Constructing a packet with more marks throws on <see cref="WriteBody" />.
    /// </summary>
    public const int MaxLegendMarks = byte.MaxValue;

    /// <summary>Nation icon byte.</summary>
    public byte NationFlag { get; set; }

    /// <summary>Display rank string within the player's guild.</summary>
    public string GuildRank { get; set; } = string.Empty;

    /// <summary>Currently displayed character title.</summary>
    public string CurrentTitle { get; set; } = string.Empty;

    /// <summary>
    ///     Free-form text rendered in the group-status panel. A specific format also populates the
    ///     group-roster widget - see <see cref="FormatGroupRoster" /> for a helper that produces it
    ///     and <see cref="GroupStatusSolo" /> for the solo-state constant. Any text is accepted; only
    ///     roster-widget rendering depends on the specific format.
    /// </summary>
    public string GroupStatusText { get; set; } = string.Empty;

    /// <summary>Whether the "request to join group" option is offered for this character.</summary>
    public bool CanGroup { get; set; }

    /// <summary>
    ///     Group-recruitment block. <c>null</c> = no recruit; the "has recruit" byte
    ///     on the wire is auto-derived from this property's null/non-null state.
    /// </summary>
    public GroupRecruitInfo? Recruit { get; set; }

    /// <summary>Character class.</summary>
    public byte Class { get; set; }

    /// <summary>
    ///     Meaning unverified; plausibly a master/grandmaster flag, faith rating, or path
    ///     progression. Conventionally emits 0.
    /// </summary>
    public byte Unknown1 { get; set; }

    /// <summary>Meaning unverified; see <see cref="Unknown1" /> for context. Conventionally emits 0.</summary>
    public byte Unknown2 { get; set; }

    /// <summary>
    ///     Display name of the character's class (e.g. <c>"Warrior"</c>, <c>"Master"</c>).
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>The player's guild name.</summary>
    public string GuildName { get; set; } = string.Empty;

    /// <summary>
    ///     Legend marks shown on the character's profile. Wire emits a u8 count
    ///     followed by each mark's body - capped at <see cref="MaxLegendMarks" />.
    /// </summary>
    public IList<LegendMark> Legend { get; set; } = [];

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.SelfProfile;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        if (Legend.Count > MaxLegendMarks)
            throw new InvalidOperationException(
                $"SelfProfile: legend mark count {Legend.Count} exceeds wire u8 limit ({MaxLegendMarks}).");

        writer.WriteByte(NationFlag);
        writer.WriteString8(GuildRank);
        writer.WriteString8(CurrentTitle);
        writer.WriteString8(GroupStatusText);
        writer.WriteBoolean(CanGroup);
        writer.WriteBoolean(Recruit is not null);

        Recruit?.Write(writer);

        writer.WriteByte(Class);
        writer.WriteByte(Unknown1);
        writer.WriteByte(Unknown2);
        writer.WriteString8(ClassName);
        writer.WriteString8(GuildName);
        writer.WriteByte((byte)Legend.Count);

        foreach (var mark in Legend)
            mark.Write(writer);
    }

    /// <inheritdoc />
    public static SelfProfilePacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var nationFlag = reader.ReadByte();
        var guildRank = reader.ReadString8();
        var currentTitle = reader.ReadString8();
        var groupStatusText = reader.ReadString8();
        var canGroup = reader.ReadBoolean();
        var hasRecruit = reader.ReadBoolean();

        GroupRecruitInfo? recruit = hasRecruit
            ? GroupRecruitInfo.Parse(ref reader)
            : null;

        var @class = reader.ReadByte();
        var unknown1 = reader.ReadByte();
        var unknown2 = reader.ReadByte();
        var className = reader.ReadString8();
        var guildName = reader.ReadString8();
        var legendCount = reader.ReadByte();

        var legend = new List<LegendMark>(legendCount);
        for (var i = 0; i < legendCount; i++)
            legend.Add(LegendMark.Parse(ref reader));

        // Trailing bytes after the legend loop are silently ignored; see remarks on this type.

        return new SelfProfilePacket
        {
            NationFlag = nationFlag,
            GuildRank = guildRank,
            CurrentTitle = currentTitle,
            GroupStatusText = groupStatusText,
            CanGroup = canGroup,
            Recruit = recruit,
            Class = @class,
            Unknown1 = unknown1,
            Unknown2 = unknown2,
            ClassName = className,
            GuildName = guildName,
            Legend = legend,
        };
    }

    /// <summary>
    ///     Builds a <see cref="GroupStatusText" /> value in the format the group-roster widget expects.
    /// </summary>
    /// <param name="founderName">Founder's character name; rendered with a <c>"* "</c> prefix.</param>
    /// <param name="memberNames">All group members <em>including the founder</em>;
    ///     non-founder names are rendered with a <c>"  "</c> two-space prefix.</param>
    /// <remarks>
    ///     Produces text of the shape:
    ///     <code>
    /// Group members
    /// * Founder
    ///   Other
    ///   Yet Another
    /// Total 3
    ///     </code>
    ///     The <c>* </c> / <c>  </c> prefix discrimination and the trailing
    ///     <c>"Total N"</c> line are load-bearing for the roster widget; deviation
    ///     here breaks the panel rendering.
    /// </remarks>
    public static string FormatGroupRoster(string founderName, IEnumerable<string> memberNames)
    {
        ArgumentNullException.ThrowIfNull(founderName);
        ArgumentNullException.ThrowIfNull(memberNames);

        var names = memberNames as IList<string> ?? memberNames.ToList();

        var sb = new StringBuilder();
        sb.Append("Group members\n");

        foreach (var name in names)
            sb.Append(name == founderName ? "* " : "  ").Append(name).Append('\n');

        sb.Append("Total ").Append(names.Count);

        return sb.ToString();
    }
}
