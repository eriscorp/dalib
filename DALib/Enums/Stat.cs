using System;

namespace DALib.Enums;

/// <summary>
///     The five primary attributes, as a bit flag. Used by the raise-stat (C->S 0x47)
///     request and wherever a single attribute is identified.
/// </summary>
[Flags]
public enum Stat
{
    /// <summary>Strength.</summary>
    STR = 1,

    /// <summary>Dexterity.</summary>
    DEX = 2,

    /// <summary>Intelligence.</summary>
    INT = 4,

    /// <summary>Wisdom.</summary>
    WIS = 8,

    /// <summary>Constitution.</summary>
    CON = 16
}
