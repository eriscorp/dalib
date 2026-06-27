using System;
using System.IO;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x2E (C->S) - group-request multiplexer. A leading <see cref="Stage" /> byte selects one of
///     five actions. Stages 2/3/6/7 carry a single target name; stage 4 (Groupbox) carries a full
///     recruit-box description. Stages 1 and 5 are not used.
/// </summary>
/// <remarks>
///     <para>
///         Simple form (2/3/6/7): <c>[u8 Stage][string8 Name][u8 0]</c>. The trailing zero is a
///         reserved byte; it is written by <see cref="WriteBody" /> and ignored by
///         <see cref="Parse" />.
///     </para>
///     <para>
///         Groupbox form (4): <c>[string8 Leader][string8 Title][string8 Note][u8 MinLevel]
///         [u8 MaxLevel][u8 MaxWarrior][u8 MaxWizard][u8 MaxRogue][u8 MaxPriest][u8 MaxMonk]</c>.
///         There is no trailing reserved byte. The five class-cap bytes are on the wire in the order
///         Warrior, Wizard, Monk, Priest, Rogue.
///     </para>
/// </remarks>
[ClientOpcode(ClientOpcode.GroupRequest)]
public sealed record GroupRequestPacket : ClientPacket
{
    /// <summary>Stage 2 - invite a player to a group (the initial request).</summary>
    public const byte StageTryInvite = 2;

    /// <summary>Stage 3 - accept an incoming invite (reply "yes" to the 0x63 dialog).</summary>
    public const byte StageAcceptInvite = 3;

    /// <summary>Stage 4 - submit a group recruit box (the rich form).</summary>
    public const byte StageGroupbox = 4;

    /// <summary>Stage 6 - remove/close your own recruit box.</summary>
    public const byte StageRemoveGroupBox = 6;

    /// <summary>Stage 7 - apply to join another player's advertised recruit box.</summary>
    public const byte StageRecruitJoin = 7;

    /// <summary>The action discriminator. One of the <c>Stage*</c> constants.</summary>
    public required byte Stage { get; init; }

    /// <summary>The target name; set only for the simple form (stages 2/3/6/7).</summary>
    public string? Name { get; init; }

    /// <summary>The group leader's name; set only for the Groupbox form (stage 4).</summary>
    public string? Leader { get; init; }

    /// <summary>The recruit-box title line; set only for the Groupbox form (stage 4).</summary>
    public string? Title { get; init; }

    /// <summary>The recruit-box note/description line; set only for the Groupbox form (stage 4).</summary>
    public string? Note { get; init; }

    /// <summary>Minimum level to join; set only for the Groupbox form (stage 4).</summary>
    public byte? MinLevel { get; init; }

    /// <summary>Maximum level to join; set only for the Groupbox form (stage 4).</summary>
    public byte? MaxLevel { get; init; }

    /// <summary>Per-class cap for Warriors; set only for the Groupbox form (stage 4).</summary>
    public byte? MaxWarrior { get; init; }

    /// <summary>Per-class cap for Wizards; set only for the Groupbox form (stage 4).</summary>
    public byte? MaxWizard { get; init; }

    /// <summary>Per-class cap for Rogues; set only for the Groupbox form (stage 4).</summary>
    public byte? MaxRogue { get; init; }

    /// <summary>Per-class cap for Priests; set only for the Groupbox form (stage 4).</summary>
    public byte? MaxPriest { get; init; }

    /// <summary>Per-class cap for Monks; set only for the Groupbox form (stage 4).</summary>
    public byte? MaxMonk { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.GroupRequest;

    /// <summary>Builds a stage-2 TryInvite for <paramref name="name" />.</summary>
    public static GroupRequestPacket TryInvite(string name) =>
        new() { Stage = StageTryInvite, Name = name };

    /// <summary>Builds a stage-3 AcceptInvite for <paramref name="name" /> (the inviter).</summary>
    public static GroupRequestPacket AcceptInvite(string name) =>
        new() { Stage = StageAcceptInvite, Name = name };

    /// <summary>Builds a stage-6 RemoveGroupBox for <paramref name="name" /> (the box owner, self).</summary>
    public static GroupRequestPacket RemoveGroupBox(string name) =>
        new() { Stage = StageRemoveGroupBox, Name = name };

    /// <summary>Builds a stage-7 RecruitJoin applying to the box led by <paramref name="name" />.</summary>
    public static GroupRequestPacket RecruitJoin(string name) =>
        new() { Stage = StageRecruitJoin, Name = name };

    /// <summary>Builds a stage-4 Groupbox recruit-box submission.</summary>
    public static GroupRequestPacket Groupbox(
        string leader,
        string title,
        string note,
        byte minLevel,
        byte maxLevel,
        byte maxWarrior,
        byte maxWizard,
        byte maxRogue,
        byte maxPriest,
        byte maxMonk) =>
        new()
        {
            Stage = StageGroupbox,
            Leader = leader,
            Title = title,
            Note = note,
            MinLevel = minLevel,
            MaxLevel = maxLevel,
            MaxWarrior = maxWarrior,
            MaxWizard = maxWizard,
            MaxRogue = maxRogue,
            MaxPriest = maxPriest,
            MaxMonk = maxMonk,
        };

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(Stage);

        switch (Stage)
        {
            case StageTryInvite:
            case StageAcceptInvite:
            case StageRemoveGroupBox:
            case StageRecruitJoin:
                if (Name is null)
                    throw new InvalidOperationException(
                        $"{nameof(GroupRequestPacket)} with {nameof(Stage)} 0x{Stage:X2} requires a non-null {nameof(Name)}.");

                writer.WriteString8(Name);
                writer.WriteByte(0); // trailing reserved byte, always 0.
                break;

            case StageGroupbox:
                if (Leader is null || Title is null || Note is null)
                    throw new InvalidOperationException(
                        $"{nameof(GroupRequestPacket)} Groupbox requires non-null {nameof(Leader)}, {nameof(Title)}, and {nameof(Note)}.");

                if (MinLevel is null || MaxLevel is null || MaxWarrior is null || MaxWizard is null
                    || MaxRogue is null || MaxPriest is null || MaxMonk is null)
                    throw new InvalidOperationException(
                        $"{nameof(GroupRequestPacket)} Groupbox requires non-null level bounds and all five class caps.");

                writer.WriteString8(Leader);
                writer.WriteString8(Title);
                writer.WriteString8(Note);
                writer.WriteByte(MinLevel.Value);
                writer.WriteByte(MaxLevel.Value);
                writer.WriteByte(MaxWarrior.Value);
                writer.WriteByte(MaxWizard.Value);
                writer.WriteByte(MaxMonk.Value);
                writer.WriteByte(MaxPriest.Value);
                writer.WriteByte(MaxRogue.Value);

                // No trailing reserved byte on this form.
                break;

            default:
                throw new InvalidOperationException(
                    $"{nameof(GroupRequestPacket)} cannot serialize unknown {nameof(Stage)} 0x{Stage:X2}.");
        }
    }

    /// <inheritdoc />
    public static GroupRequestPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var stage = reader.ReadByte();

        switch (stage)
        {
            case StageTryInvite:
            case StageAcceptInvite:
            case StageRemoveGroupBox:
            case StageRecruitJoin:
                // Simple form: name then a trailing reserved 0x00 (left unread, always 0).
                return new GroupRequestPacket { Stage = stage, Name = reader.ReadString8() };

            case StageGroupbox:
                return new GroupRequestPacket
                {
                    Stage = stage,
                    Leader = reader.ReadString8(),
                    Title = reader.ReadString8(),
                    Note = reader.ReadString8(),
                    MinLevel = reader.ReadByte(),
                    MaxLevel = reader.ReadByte(),
                    MaxWarrior = reader.ReadByte(),
                    MaxWizard = reader.ReadByte(),
                    MaxMonk = reader.ReadByte(),
                    MaxPriest = reader.ReadByte(),
                    MaxRogue = reader.ReadByte(),
                };

            default:
                throw new InvalidDataException(
                    $"{nameof(GroupRequestPacket)}: unknown stage 0x{stage:X2}.");
        }
    }
}
