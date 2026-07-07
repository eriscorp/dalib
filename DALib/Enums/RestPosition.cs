namespace DALib.Enums;

/// <summary>
///     Aisling resting / reclining pose. Byte values match the retail wire encoding.
/// </summary>
public enum RestPosition : byte
{
    /// <summary>Standing - not resting.</summary>
    None = 0,

    /// <summary>Kneeling.</summary>
    Kneel = 1,

    /// <summary>Laying down.</summary>
    Lay = 2,

    /// <summary>Sprawled.</summary>
    Sprawl = 3
}
