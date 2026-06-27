namespace DALib.Enums;

/// <summary>
///     Character gender. The byte values match the DOOMVAS wire encoding (character creation
///     0x04 sends 1 = Male, 2 = Female) and are identical to the ecosystem's Hybrasyl.Xml
///     <c>Gender</c> enum. <see cref="Neutral" /> (0) is the genderless default used by item
///     restrictions and unset appearances; the character-creation client only ever emits
///     <see cref="Male" /> or <see cref="Female" />.
/// </summary>
public enum Gender : byte
{
    /// <summary>Genderless / unset. The zero default; not emitted by character creation.</summary>
    Neutral = 0,

    /// <summary>Male.</summary>
    Male = 1,

    /// <summary>Female.</summary>
    Female = 2
}
