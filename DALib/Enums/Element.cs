namespace DALib.Enums;

/// <summary>
///     Offensive / defensive element. Byte values are the DOOMVAS wire encoding.
/// </summary>
public enum Element : byte
{
    /// <summary>No element.</summary>
    None = 0,

    /// <summary>Fire.</summary>
    Fire = 1,

    /// <summary>Water.</summary>
    Water = 2,

    /// <summary>Wind.</summary>
    Wind = 3,

    /// <summary>Earth.</summary>
    Earth = 4,

    /// <summary>Light.</summary>
    Light = 5,

    /// <summary>Dark.</summary>
    Dark = 6,

    /// <summary>Wood.</summary>
    Wood = 7,

    /// <summary>Metal.</summary>
    Metal = 8,

    /// <summary>Undead.</summary>
    Undead = 9
}
