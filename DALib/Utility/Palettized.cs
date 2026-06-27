using System;
using DALib.Drawing;

namespace DALib.Utility;

/// <summary>
///     Represents a palettized object that associates an entity with a palette.
/// </summary>
/// <typeparam name="T">
///     The type of the entity.
/// </typeparam>
public sealed class Palettized<T> : IDisposable
{
    /// <summary>
    ///     The entity with which the palette is associated.
    /// </summary>
    public required T Entity { get; init; }

    /// <summary>
    ///     The palette with which the entity is associated.
    /// </summary>
    public required Palette Palette { get; init; }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Entity is IDisposable disposable)
            disposable.Dispose();
    }

    /// <summary>
    ///     Deconstructs the Palettized object into its entity and palette.
    /// </summary>
    public void Deconstruct(out T entity, out Palette palette)
    {
        entity = Entity;
        palette = Palette;
    }
}