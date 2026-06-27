namespace DALib.Networking.Packets.Server;

/// <summary>
///     The color of a status-effect icon in a <see cref="StatusBarPacket" /> (0x3A) - typically used as
///     a remaining-duration indicator. Values 1-6 select a color; 0 removes the icon.
/// </summary>
public enum StatusBarColor : byte
{
    /// <summary>0 - no color; removes the icon from the status bar.</summary>
    None = 0,

    /// <summary>1 - blue.</summary>
    Blue = 1,

    /// <summary>2 - green.</summary>
    Green = 2,

    /// <summary>3 - yellow.</summary>
    Yellow = 3,

    /// <summary>4 - orange.</summary>
    Orange = 4,

    /// <summary>5 - red.</summary>
    Red = 5,

    /// <summary>6 - white.</summary>
    White = 6,
}
