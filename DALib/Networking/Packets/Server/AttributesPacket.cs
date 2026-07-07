using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x08 (S->C) - character stat updates. A single flag byte carries section-presence bits
///     for up to four optional sections (<see cref="Primary" />, <see cref="Current" />,
///     <see cref="Experience" />, <see cref="Secondary" />) plus standalone signal bits
///     (<see cref="MovementMode" />, <see cref="UnreadMail" />). The wire size varies from
///     1 byte (flag only) to 74 bytes (flag + all four sections).
/// </summary>
/// <remarks>
///     <para>
///         <strong>Flag byte layout</strong> (bit positions):
///         <list type="table">
///             <listheader><term>Bit(s)</term><description>Purpose</description></listheader>
///             <item><term>0x01</term><description><see cref="UnreadMail" /> - gates the MailStatus nibble decode in Secondary.</description></item>
///             <item><term>0x02</term><description><see cref="ReservedFlag" /> - preserved for round-trip; no observable effect.</description></item>
///             <item><term>0x04</term><description>Secondary section present.</description></item>
///             <item><term>0x08</term><description>Experience section present.</description></item>
///             <item><term>0x10</term><description>Current section present.</description></item>
///             <item><term>0x20</term><description>Primary section present.</description></item>
///             <item><term>0xC0</term><description><see cref="MovementMode" /> (high pair as a 0-3 value).</description></item>
///         </list>
///     </para>
///     <para>
///         Section presence is driven by the corresponding nested-record property being
///         non-null. Set a section to <c>null</c> to omit it; set it to <c>new()</c> or a
///         populated instance to include it. This supports both whole-character emits (all four
///         sections populated) and narrow updates (e.g. just <see cref="Current" /> after damage).
///     </para>
/// </remarks>
[ServerOpcode(ServerOpcode.Attributes)]
public sealed record AttributesPacket : ServerPacket
{
    private const byte FlagUnreadMail = 0x01;
    private const byte FlagReserved = 0x02;
    private const byte FlagSecondary = 0x04;
    private const byte FlagExperience = 0x08;
    private const byte FlagCurrent = 0x10;
    private const byte FlagPrimary = 0x20;
    private const byte FlagMovementModeMask = 0xC0;
    private const int FlagMovementModeShift = 6;

    /// <summary>
    ///     2-bit movement mode (0-3) encoded in flag bits 6-7. The value selects the player's
    ///     movement behavior. Leave 0 for normal emits.
    /// </summary>
    public byte MovementMode { get; set; }

    /// <summary>
    ///     Flag bit 0x01 - when set, the MailStatus nibbles in <see cref="Secondary" /> are
    ///     decoded into the hasParcel/hasMail indicators. Only that decode step is gated; the
    ///     byte itself is still present when Secondary is.
    /// </summary>
    public bool UnreadMail { get; set; }

    /// <summary>
    ///     Flag bit 0x02 - no observable effect. Preserved as a settable field for wire
    ///     round-trip fidelity; leave false for normal emits.
    /// </summary>
    public bool ReservedFlag { get; set; }

    /// <summary>Primary section (28 bytes). <c>null</c> = section absent on the wire.</summary>
    public PrimaryAttributes? Primary { get; set; }

    /// <summary>Current section (8 bytes). <c>null</c> = section absent on the wire.</summary>
    public CurrentAttributes? Current { get; set; }

    /// <summary>Experience section (24 bytes). <c>null</c> = section absent on the wire.</summary>
    public ExperienceAttributes? Experience { get; set; }

    /// <summary>Secondary section (13 bytes). <c>null</c> = section absent on the wire.</summary>
    public SecondaryAttributes? Secondary { get; set; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.Attributes;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        if (MovementMode > 3)
            throw new InvalidOperationException(
                $"AttributesPacket: MovementMode {MovementMode} exceeds the 2-bit wire range (0-3).");

        byte flag = 0;
        if (UnreadMail) flag |= FlagUnreadMail;
        if (ReservedFlag) flag |= FlagReserved;
        if (Secondary is not null) flag |= FlagSecondary;
        if (Experience is not null) flag |= FlagExperience;
        if (Current is not null) flag |= FlagCurrent;
        if (Primary is not null) flag |= FlagPrimary;
        flag |= (byte)(MovementMode << FlagMovementModeShift);

        writer.WriteByte(flag);

        Primary?.Write(writer);
        Current?.Write(writer);
        Experience?.Write(writer);
        Secondary?.Write(writer);
    }

    /// <inheritdoc />
    public static AttributesPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var flag = reader.ReadByte();

        var packet = new AttributesPacket
        {
            MovementMode = (byte)((flag & FlagMovementModeMask) >> FlagMovementModeShift),
            UnreadMail = (flag & FlagUnreadMail) != 0,
            ReservedFlag = (flag & FlagReserved) != 0,
        };

        if ((flag & FlagPrimary) != 0)
            packet.Primary = PrimaryAttributes.Parse(ref reader);

        if ((flag & FlagCurrent) != 0)
            packet.Current = CurrentAttributes.Parse(ref reader);

        if ((flag & FlagExperience) != 0)
            packet.Experience = ExperienceAttributes.Parse(ref reader);

        if ((flag & FlagSecondary) != 0)
            packet.Secondary = SecondaryAttributes.Parse(ref reader);

        //DOOMVAS encryption appends an inner-pad byte (0x00, or 0x00+opcode for MD5Key) before the rand
        //footer; DecryptServer strips only the footer, so a trailing pad may remain. Tolerate it rather
        //than reject the packet (see DecryptServer inner-pad strip, the proper fix).

        return packet;
    }
}
