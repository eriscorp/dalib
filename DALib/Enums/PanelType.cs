namespace DALib.Enums;

/// <summary>
///     Which paged panel an operation targets. Byte values match the retail wire
///     encoding (e.g. the C->S swap-slot request).
/// </summary>
public enum PanelType : byte
{
    /// <summary>Inventory panel.</summary>
    Inventory = 0,

    /// <summary>Spell book panel.</summary>
    SpellBook = 1,

    /// <summary>Skill book panel.</summary>
    SkillBook = 2,

    /// <summary>Equipment panel.</summary>
    Equipment = 3
}
