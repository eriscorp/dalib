namespace DALib.Networking.Packets.Server;

/// <summary>
///     The paper-texture selector carried by S->C 0x35 <see cref="ReadonlyPaperPacket" /> and 0x1B
///     <see cref="EditablePaperPacket" /> - which background graphic the paper/scroll is drawn on.
/// </summary>
/// <remarks>
///     A closed set (0-4). Values of 5 or greater are out of range for the texture table; senders must
///     keep the byte in 0-4. Modeled as an enum because the valid range is bounded.
/// </remarks>
public enum PaperType : byte
{
    /// <summary>A plain brown paper.</summary>
    Brown = 0,

    /// <summary>A misrendered blue texture (source art absent from the standard install).</summary>
    GlitchedBlue1 = 1,

    /// <summary>A second misrendered blue texture (source art absent from the standard install).</summary>
    GlitchedBlue2 = 2,

    /// <summary>A gold/orange paper.</summary>
    Orange = 3,

    /// <summary>A white paper.</summary>
    White = 4
}
