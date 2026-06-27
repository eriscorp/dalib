namespace DALib.Enums;

/// <summary>
///     An Aisling's base class. Byte values match the retail wire encoding. The
///     post-99 Medenia progression and the secondary "subclass" axis are separate
///     concepts and are not modeled here.
/// </summary>
public enum Class : byte
{
    /// <summary>Unclassed (pre-insight).</summary>
    Peasant = 0,

    /// <summary>Warrior.</summary>
    Warrior = 1,

    /// <summary>Rogue.</summary>
    Rogue = 2,

    /// <summary>Wizard.</summary>
    Wizard = 3,

    /// <summary>Priest.</summary>
    Priest = 4,

    /// <summary>Monk.</summary>
    Monk = 5
}
