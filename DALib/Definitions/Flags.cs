using System;

namespace DALib.Definitions;

/// <summary>
///     Tils flags as used in sotp.dat
/// </summary>
[Flags]
public enum TileFlags : byte
{
    /// <summary>
    ///     Tile is a normal tile
    /// </summary>
    None = 0,

    /// <summary>
    ///     Tile is a wall
    /// </summary>
    Wall = 15,

    /// <summary>
    ///     Tile has luminosity based transparency (dark = more transparent)
    /// </summary>
    Transparent = 128
}