using System;
using System.Numerics;
using DALib.Extensions;

namespace DALib.Utility;

/// <summary>
///     An extension of the Math class, providing additional math-related methods
/// </summary>
public static class MathEx
{
    /// <summary>
    ///     Scales a number from one range to another range.
    /// </summary>
    /// <param name="num">
    ///     The input number to be scaled.
    /// </param>
    /// <param name="min">
    ///     The lower bound of the original range.
    /// </param>
    /// <param name="max">
    ///     The upper bound of the original range.
    /// </param>
    /// <param name="newMin">
    ///     The lower bound of the new range.
    /// </param>
    /// <param name="newMax">
    ///     The upper bound of the new range.
    /// </param>
    /// <returns>
    ///     The scaled number in the new range.
    /// </returns>
    /// <remarks>
    ///     This method assumes that the input number is within the original range. No clamping or checking is performed.
    /// </remarks>
    public static T2 ScaleRange<T1, T2>(
        T1 num,
        T1 min,
        T1 max,
        T2 newMin,
        T2 newMax) where T1: INumber<T1>
                   where T2: INumber<T2>
    {
        if (min.Equals(max))
            throw new ArgumentOutOfRangeException(nameof(min), "Min and max cannot be the same value");

        // Compute the ratio as double for higher precision
        var ratio = double.CreateChecked(num - min) / double.CreateChecked(max - min);

        // Compute the scaled value
        var scaledValue = ratio * double.CreateChecked(newMax - newMin) + double.CreateChecked(newMin);

        // Determine if T2 is an integer type
        if (NumberExtensions<T2>.IsIntegerType)
        {
            // Round the scaled value to the nearest integer
            var roundedValue = Math.Round(scaledValue, MidpointRounding.AwayFromZero);

            return T2.CreateChecked(roundedValue);
        }

        // For floating-point types, return the scaled value directly
        return T2.CreateChecked(scaledValue);
    }

    /// <summary>
    ///     Scales a number from one range to another range.
    /// </summary>
    /// <param name="num">
    ///     The input number to be scaled.
    /// </param>
    /// <param name="min">
    ///     The lower bound of the original range.
    /// </param>
    /// <param name="max">
    ///     The upper bound of the original range.
    /// </param>
    /// <param name="newMin">
    ///     The lower bound of the new range.
    /// </param>
    /// <param name="newMax">
    ///     The upper bound of the new range.
    /// </param>
    /// <returns>
    ///     The scaled number in the new range.
    /// </returns>
    /// <remarks>
    ///     This method assumes that the input number is within the original range. No clamping or checking is performed.
    /// </remarks>
    public static byte ScaleRangeByteOptimized(
        byte num,
        byte min,
        byte max,
        byte newMin,
        byte newMax)
    {
        if (min == max)
            throw new ArgumentOutOfRangeException(nameof(min), "Min and max cannot be the same value");

        // Cast to float (or double) to avoid truncation
        var ratio = (float)(num - min) / (max - min);
        var newValue = (newMax - newMin) * ratio + newMin;

        return (byte)Math.Round(newValue);
    }
}