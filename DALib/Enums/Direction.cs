namespace DALib.Enums;

/// <summary>
///     A cardinal facing direction. The byte values match the DOOMVAS wire encoding used by
///     movement packets (0x06 Walk, 0x11 Turn) and by object-facing fields, and are identical
///     to the ecosystem's Hybrasyl.Xml <c>Direction</c> enum.
/// </summary>
public enum Direction : byte
{
    /// <summary>Facing up / toward the top of the map.</summary>
    North = 0,

    /// <summary>Facing right.</summary>
    East = 1,

    /// <summary>Facing down / toward the bottom of the map.</summary>
    South = 2,

    /// <summary>Facing left.</summary>
    West = 3
}
