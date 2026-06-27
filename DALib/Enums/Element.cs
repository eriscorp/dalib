namespace DALib.Enums;

/// <summary>
///     Offensive / defensive element. Byte values are the retail DOOMVAS wire encoding,
///     and the labels are retail-canonical (verified against the <c>Darkages.exe</c>
///     element name table) - retail uses <see cref="Light" />/<see cref="Dark" /> (not
///     Holy/Darkness) and <see cref="Undead" /> at 9.
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

    /// <summary>Light (retail label; Chaos calls this Holy).</summary>
    Light = 5,

    /// <summary>Dark (retail label; Chaos calls this Darkness).</summary>
    Dark = 6,

    /// <summary>Wood.</summary>
    Wood = 7,

    /// <summary>Metal.</summary>
    Metal = 8,

    /// <summary>Undead.</summary>
    Undead = 9
}
