using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace DALib.Utility;

/// <summary>
///     Represents a disposable image cache that stores SKImage instances based on a generic key
/// </summary>
/// <typeparam name="TKey">
///     The type of the cache key.
/// </typeparam>
public sealed class SKImageCache<TKey>(IEqualityComparer<TKey>? comparer = null) : IDisposable where TKey: IEquatable<TKey>
{
    private readonly Dictionary<TKey, SKImage> Cache = new(comparer);

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var key in Cache.Keys)
            CollectionsMarshal.GetValueRefOrNullRef(Cache, key)
                              .Dispose();

        Cache.Clear();
    }

    /// <summary>
    ///     Retrieves an existing SKImage for the specified key from the cache, or creates a new SKImage using the provided
    ///     create function and adds it to the cache.
    /// </summary>
    /// <param name="key">
    ///     The key used to retrieve or create the SKImage.
    /// </param>
    /// <param name="create">
    ///     The function used to create a new SKImage for the specified key.
    /// </param>
    public SKImage GetOrCreate(TKey key, Func<TKey, SKImage> create)
    {
        if (Cache.TryGetValue(key, out var image))
            return image;

        image = create(key);

        Cache.Add(key, image);

        return image;
    }
}