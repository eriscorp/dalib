using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SkiaSharp;

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace DALib.Utility;

/// <summary>
///     Represents a disposable collection of SKImages.
/// </summary>
public sealed class SKImageCollection(IEnumerable<SKImage> images) : Collection<SKImage>(
                                                                         images.Where(frame => frame is not null)
                                                                               .ToList()),
                                                                     IDisposable
{
    /// <inheritdoc />
    public void Dispose()
    {
        for (var i = 0; i < Items.Count; i++)
        {
            var item = Items[i];
            item.Dispose();
        }

        Items.Clear();
    }
}