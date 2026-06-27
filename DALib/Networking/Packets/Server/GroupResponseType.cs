namespace DALib.Networking.Packets.Server;

/// <summary>
///     The leading byte of S->C 0x63 <see cref="GroupResponsePacket" /> - which group notification the
///     server is sending. Selects the variant and body layout. Values other than the three below have
///     no effect.
/// </summary>
public enum GroupResponseType : byte
{
    /// <summary>1 - a group invitation prompt naming the inviter.</summary>
    Ask = 1,

    /// <summary>4 - a recruitment-info pane (the recruiter's pitch, level range, and per-class wanted/have counts).</summary>
    RecruitInfo = 4,

    /// <summary>
    ///     5 - a recruitment-pull prompt naming the recruiter; receipt elicits a C->S 0x2E type 2
    ///     (Request) carrying the same name. Modeled for protocol completeness; not emitted by typical
    ///     servers.
    /// </summary>
    RecruitAsk = 5
}
