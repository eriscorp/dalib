using System;
using System.Collections.Generic;
using System.Linq;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x34 (S->C) - another player's profile pane, shown when clicking on an aisling. Carries the
///     target's serial, a fixed equipment-display block, social/guild identity, public legend marks,
///     and the portrait + profile-text blob. This is the <em>other-player</em> profile; the player's
///     own pane is 0x39 <see cref="SelfProfilePacket" />.
/// </summary>
/// <remarks>
///     <para>
///         Body: <c>[u32 Id]</c>, then 18 <see cref="ProfileEquipmentSlot" /> entries
///         (<c>[u16 sprite][u8 color]</c> each, <see cref="EquipmentSlotCount" /> total),
///         <c>[u8 SocialStatus][string8 Name][u8 NationFlag][string8 Title][bool GroupOpen]
///         [string8 GuildRank][string8 ClassName][string8 GuildName][u8 legendCount]</c>, then each
///         legend mark (<c>[u8 icon][u8 color][string8 prefix][string8 text]</c>), then the trailing
///         portrait/text blob: <c>[u16 remaining][u16 portraitLen][portrait bytes][string16 ProfileText]</c>
///         where <c>remaining = portraitLen + ProfileText.Length + 4</c> (the byte count of everything
///         after the <c>remaining</c> field itself; derived on write, read-and-ignored on parse).
///     </para>
///     <para>
///         The 18-slot equipment block is in a fixed <em>display</em> order that is not the 1-18 pane-slot
///         numbering. Each slot is named by an <see cref="EquipmentSlot" /> in
///         <see cref="EquipmentDisplayOrder" />; <see cref="EquipmentSlot.Accessory1" /> precedes
///         <see cref="EquipmentSlot.Boots" /> (positions 13/14 on the wire), the reverse of their slot
///         numbers. <see cref="WriteBody" /> validates that <see cref="Equipment" /> carries exactly these
///         slots in this order.
///     </para>
///     <para>
///         The legend count on the wire always matches <see cref="Legend" /> exactly, so callers should
///         pre-filter <see cref="Legend" /> to the marks they intend to send.
///     </para>
/// </remarks>
[ServerOpcode(ServerOpcode.Profile)]
public sealed record ProfilePacket : ServerPacket
{
    /// <summary>The number of equipment slots in the profile display block (fixed by the wire).</summary>
    public const int EquipmentSlotCount = 18;

    /// <summary>Hard wire limit on the number of legend marks; the count is a u8.</summary>
    public const int MaxLegendMarks = byte.MaxValue;

    /// <summary>
    ///     The 18 equipment slots in the exact order they appear on the wire - the profile
    ///     <em>display</em> order, which is <strong>not</strong> the 1-18 pane numbering:
    ///     <see cref="EquipmentSlot.Accessory1" /> precedes <see cref="EquipmentSlot.Boots" /> (the
    ///     reverse of their slot numbers).
    /// </summary>
    public static readonly IReadOnlyList<EquipmentSlot> EquipmentDisplayOrder =
    [
        EquipmentSlot.Weapon, EquipmentSlot.Armor, EquipmentSlot.Shield, EquipmentSlot.Helmet,
        EquipmentSlot.Earrings, EquipmentSlot.Necklace, EquipmentSlot.LeftRing, EquipmentSlot.RightRing,
        EquipmentSlot.LeftGauntlet, EquipmentSlot.RightGauntlet, EquipmentSlot.Belt, EquipmentSlot.Greaves,
        EquipmentSlot.Accessory1, EquipmentSlot.Boots, EquipmentSlot.Overcoat, EquipmentSlot.OverHelm,
        EquipmentSlot.Accessory2, EquipmentSlot.Accessory3
    ];

    /// <summary>The profiled player's serial.</summary>
    public uint Id { get; set; }

    /// <summary>
    ///     The equipment-display block: exactly <see cref="EquipmentSlotCount" /> slots whose
    ///     <see cref="ProfileEquipmentSlot.Slot" /> values match <see cref="EquipmentDisplayOrder" /> in
    ///     order (a sprite + color per slot). Defaults to that sequence with empty sprites; build a
    ///     profile by setting the sprite/color of each entry. <see cref="WriteBody" /> throws if the
    ///     block is not the canonical 18 slots in the canonical order.
    /// </summary>
    public IList<ProfileEquipmentSlot> Equipment { get; set; } = CreateEmptySlots();

    /// <summary>The player's social / grouping-availability status indicator.</summary>
    public SocialStatus SocialStatus { get; set; }

    /// <summary>The player's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Nation icon byte.</summary>
    public byte NationFlag { get; set; }

    /// <summary>Currently displayed character title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Whether the "request to join group" option is offered for this character.</summary>
    public bool GroupOpen { get; set; }

    /// <summary>Display rank string within the player's guild.</summary>
    public string GuildRank { get; set; } = string.Empty;

    /// <summary>Display name of the character's class (e.g. <c>"Warrior"</c>, <c>"Master"</c>).</summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>The player's guild name.</summary>
    public string GuildName { get; set; } = string.Empty;

    /// <summary>
    ///     Public legend marks shown on the profile. Wire emits a u8 count followed by each mark's
    ///     body - capped at <see cref="MaxLegendMarks" />.
    /// </summary>
    public IList<LegendMark> Legend { get; set; } = [];

    /// <summary>
    ///     The character portrait image bytes (the portrait image format). Emitted as
    ///     <c>[u16 length][bytes]</c>; an empty array means "no portrait".
    /// </summary>
    public byte[] Portrait { get; set; } = [];

    /// <summary>The free-form profile text (<c>string16</c>).</summary>
    public string ProfileText { get; set; } = string.Empty;

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.Profile;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        if (Equipment.Count != EquipmentSlotCount)
            throw new InvalidOperationException(
                $"Profile: equipment block must contain exactly {EquipmentSlotCount} slots (got {Equipment.Count}).");

        for (var i = 0; i < EquipmentSlotCount; i++)
            if (Equipment[i].Slot != EquipmentDisplayOrder[i])
                throw new InvalidOperationException(
                    $"Profile: equipment slot {i} must be {EquipmentDisplayOrder[i]} (got {Equipment[i].Slot}); " +
                    "the block is the fixed profile display order (see EquipmentDisplayOrder).");

        if (Legend.Count > MaxLegendMarks)
            throw new InvalidOperationException(
                $"Profile: legend mark count {Legend.Count} exceeds wire u8 limit ({MaxLegendMarks}).");

        if (Portrait.Length > ushort.MaxValue)
            throw new InvalidOperationException(
                $"Profile: portrait length {Portrait.Length} exceeds wire u16 limit ({ushort.MaxValue}).");

        var remaining = Portrait.Length + ProfileText.Length + 4;
        if (remaining > ushort.MaxValue)
            throw new InvalidOperationException(
                $"Profile: portrait + profile text length {remaining} exceeds the wire u16 limit ({ushort.MaxValue}).");

        writer.WriteUInt32(Id);

        foreach (var slot in Equipment)
        {
            writer.WriteUInt16(slot.Sprite);
            writer.WriteByte(slot.Color);
        }

        writer.WriteByte((byte)SocialStatus);
        writer.WriteString8(Name);
        writer.WriteByte(NationFlag);
        writer.WriteString8(Title);
        writer.WriteBoolean(GroupOpen);
        writer.WriteString8(GuildRank);
        writer.WriteString8(ClassName);
        writer.WriteString8(GuildName);
        writer.WriteByte((byte)Legend.Count);

        foreach (var mark in Legend)
            mark.Write(writer);

        writer.WriteUInt16((ushort)remaining);
        writer.WriteUInt16((ushort)Portrait.Length);
        writer.WriteBytes(Portrait);
        writer.WriteString16(ProfileText);
    }

    /// <inheritdoc />
    public static ProfilePacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var id = reader.ReadUInt32();

        var equipment = new List<ProfileEquipmentSlot>(EquipmentSlotCount);
        for (var i = 0; i < EquipmentSlotCount; i++)
            equipment.Add(new ProfileEquipmentSlot(EquipmentDisplayOrder[i], reader.ReadUInt16(), reader.ReadByte()));

        var socialStatus = (SocialStatus)reader.ReadByte();
        var name = reader.ReadString8();
        var nationFlag = reader.ReadByte();
        var title = reader.ReadString8();
        var groupOpen = reader.ReadBoolean();
        var guildRank = reader.ReadString8();
        var className = reader.ReadString8();
        var guildName = reader.ReadString8();
        var legendCount = reader.ReadByte();

        var legend = new List<LegendMark>(legendCount);
        for (var i = 0; i < legendCount; i++)
            legend.Add(LegendMark.Parse(ref reader));

        // [u16 remaining] is the byte count of the portrait/text tail; fully derivable from the two
        // self-describing fields that follow, so it is read and discarded.
        _ = reader.ReadUInt16();

        var portraitLength = reader.ReadUInt16();
        var portrait = reader.ReadBytes(portraitLength).ToArray();
        var profileText = reader.ReadString16();

        return new ProfilePacket
        {
            Id = id,
            Equipment = equipment,
            SocialStatus = socialStatus,
            Name = name,
            NationFlag = nationFlag,
            Title = title,
            GroupOpen = groupOpen,
            GuildRank = guildRank,
            ClassName = className,
            GuildName = guildName,
            Legend = legend,
            Portrait = portrait,
            ProfileText = profileText,
        };
    }

    private static List<ProfileEquipmentSlot> CreateEmptySlots() =>
        [.. EquipmentDisplayOrder.Select(slot => new ProfileEquipmentSlot(slot, 0, 0))];
}

/// <summary>
///     One slot of a <see cref="ProfilePacket" /> equipment-display block: which
///     <see cref="EquipmentSlot" /> it is, its display sprite, and the sprite's palette color. A
///     zero <see cref="Sprite" /> means "nothing equipped there". The slots appear on the wire in
///     <see cref="ProfilePacket.EquipmentDisplayOrder" />.
/// </summary>
public readonly record struct ProfileEquipmentSlot(EquipmentSlot Slot, ushort Sprite, byte Color);
