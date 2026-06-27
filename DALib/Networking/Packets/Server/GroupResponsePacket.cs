using System;
using System.IO;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x63 (S->C) - a group notification the server pushes to the client. The body opens with a single
///     <c>[u8 type]</c> byte (a <see cref="GroupResponseType" />) selecting the form; the rest varies.
///     The concrete forms are the sealed records deriving from this base
///     (<see cref="GroupPromptPacket" />, <see cref="GroupRecruitInfoPacket" />). The matching client
///     packet for grouping actions is C->S 0x2E
///     (<see cref="DALib.Networking.Packets.Client.GroupRequestPacket" />).
/// </summary>
/// <remarks>
///     <see cref="Parse" /> stops after each form's body and ignores any trailing bytes (some servers
///     emit padding NULs after the <see cref="GroupResponseType.Ask" /> name); the written form is the
///     clean <c>[type][string8 name]</c>.
/// </remarks>
[ServerOpcode(ServerOpcode.Group)]
public abstract record GroupResponsePacket : ServerPacket
{
    /// <summary>The leading byte that selects this form. Validated against the variant's allowed set on write.</summary>
    public required GroupResponseType ResponseType { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.Group;

    /// <summary>Writes the leading <c>[u8 type]</c> byte after validating it is one this variant accepts.</summary>
    private protected void WriteLeadingByte(IPacketWriter writer, params GroupResponseType[] allowed)
    {
        if (Array.IndexOf(allowed, ResponseType) < 0)
            throw new InvalidOperationException(
                $"{GetType().Name}: ResponseType {ResponseType} is not valid for this form (expected one of " +
                $"{string.Join(", ", allowed)}).");

        writer.WriteByte((byte)ResponseType);
    }

    /// <summary>
    ///     Parses a 0x63 body, dispatching on the leading <see cref="GroupResponseType" /> byte to the
    ///     matching variant. This is what <see cref="ServerOpcodeAttribute" /> dispatch binds.
    /// </summary>
    public static GroupResponsePacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var type = (GroupResponseType)reader.ReadByte();

        return type switch
        {
            GroupResponseType.Ask or GroupResponseType.RecruitAsk => GroupPromptPacket.ParseBody(type, ref reader),
            GroupResponseType.RecruitInfo => GroupRecruitInfoPacket.ParseBody(ref reader),
            _ => throw new InvalidDataException(
                $"0x63 Group: unknown response type 0x{(byte)type:X2}.")
        };
    }
}

/// <summary>
///     0x63 types 1/5 - a single-name group prompt: an invitation (<see cref="GroupResponseType.Ask" />,
///     "X invites you to a group") or a recruitment pull (<see cref="GroupResponseType.RecruitAsk" />;
///     answered with C->S 0x2E type 2). Body <c>[type][string8 SourceName]</c>.
/// </summary>
public sealed record GroupPromptPacket : GroupResponsePacket
{
    /// <summary>The name of the inviting / recruiting player.</summary>
    public required string SourceName { get; init; }

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WriteLeadingByte(writer, GroupResponseType.Ask, GroupResponseType.RecruitAsk);
        writer.WriteString8(SourceName);
    }

    internal static GroupPromptPacket ParseBody(GroupResponseType type, ref PacketReader reader)
        => new()
        {
            ResponseType = type,
            SourceName = reader.ReadString8()
        };
}

/// <summary>
///     0x63 type 4 - a recruitment-info pane: the recruiter's pitch, desired level range, and a
///     wanted-vs-current count per class. Body <c>[04]</c> followed by a <see cref="GroupRecruitInfo" />
///     block (the same block carried by 0x39 <see cref="SelfProfilePacket.Recruit" />).
/// </summary>
public sealed record GroupRecruitInfoPacket : GroupResponsePacket
{
    /// <summary>The recruitment details rendered in the pane. Never null.</summary>
    public required GroupRecruitInfo Info { get; init; }

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WriteLeadingByte(writer, GroupResponseType.RecruitInfo);
        Info.Write(writer);
    }

    internal static GroupRecruitInfoPacket ParseBody(ref PacketReader reader)
        => new()
        {
            ResponseType = GroupResponseType.RecruitInfo,
            Info = GroupRecruitInfo.Parse(ref reader)
        };
}
