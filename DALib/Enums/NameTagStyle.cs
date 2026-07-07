namespace DALib.Enums;

/// <summary>
///     How an Aisling's name tag is presented (color / hover behavior). Byte values
///     match the retail wire encoding carried in the appearance packet.
/// </summary>
public enum NameTagStyle : byte
{
    /// <summary>Neutral, shown only on hover.</summary>
    NeutralHover = 0,

    /// <summary>Hostile (always shown, hostile coloring).</summary>
    Hostile = 1,

    /// <summary>Friendly, shown only on hover.</summary>
    FriendlyHover = 2,

    /// <summary>Neutral (always shown).</summary>
    Neutral = 3
}
