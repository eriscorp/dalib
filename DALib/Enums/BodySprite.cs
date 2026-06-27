namespace DALib.Enums;

/// <summary>
///     Aisling body sprite base. Byte values match the retail wire encoding; the
///     low nibble carries variant/animation offsets, so members step by 16.
/// </summary>
public enum BodySprite : byte
{
    /// <summary>No body sprite.</summary>
    None = 0,

    /// <summary>Male body.</summary>
    Male = 16,

    /// <summary>Female body.</summary>
    Female = 32,

    /// <summary>Male ghost (deceased) body.</summary>
    MaleGhost = 48,

    /// <summary>Female ghost (deceased) body.</summary>
    FemaleGhost = 64,

    /// <summary>Invisible male body.</summary>
    MaleInvis = 80,

    /// <summary>Invisible female body.</summary>
    FemaleInvis = 96,

    /// <summary>Male jester body.</summary>
    MaleJester = 112,

    /// <summary>Male head-only body.</summary>
    MaleHead = 128,

    /// <summary>Female head-only body.</summary>
    FemaleHead = 144,

    /// <summary>Blank male body.</summary>
    BlankMale = 160,

    /// <summary>Blank female body.</summary>
    BlankFemale = 176
}
