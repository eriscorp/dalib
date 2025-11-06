using System;
using System.Collections.Generic;

namespace DALib.Comparers;

/// <summary>
///     Compares two strings and returns a value indicating whether one is less than, equal to, or greater than the other.
///     Underscore will be considered less than any other character
/// </summary>
public sealed class PreferUnderscoreIgnoreCaseStringComparer : IComparer<string>
{
    /// <summary>
    ///     Gets the default instance of the <see cref="PreferUnderscoreIgnoreCaseStringComparer" /> class.
    /// </summary>
    public static IComparer<string> Instance { get; } = new PreferUnderscoreIgnoreCaseStringComparer();

    /// <inheritdoc />
    public int Compare(string? x, string? y)
    {
        if (StringComparer.OrdinalIgnoreCase.Equals(x, y))
            return 0;

        if (x == null)
            return -1;

        if (y == null)
            return 1;

        var xLength = x.Length;
        var yLength = y.Length;
        var minLength = xLength < yLength ? xLength : yLength;

        for (var i = 0; i < minLength; i++)
        {
            var xChar = char.ToUpperInvariant(x[i]);
            var yChar = char.ToUpperInvariant(y[i]);

            if (xChar == yChar)
                continue;

            if (xChar == '_')
                return -1;

            if (yChar == '_')
                return 1;

            return xChar.CompareTo(yChar);
        }

        return xLength.CompareTo(yLength);
    }
}