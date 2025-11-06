using DALib.Definitions;
using SkiaSharp;

namespace DALib.Utility;

/// <summary>
///     Provides methods for encoding and decoding colors.
/// </summary>
public static class ColorCodec
{
    /// <summary>
    ///     Decodes a 16-bit encoded RGB555 color value into an SKColor and scales the color to RGB888
    /// </summary>
    /// <param name="encodedColor">
    ///     The 16-bit encoded RGB555 color value
    /// </param>
    public static SKColor DecodeRgb555(ushort encodedColor)
    {
        var r = (byte)((encodedColor >> 10) & CONSTANTS.FIVE_BIT_MASK);
        var g = (byte)((encodedColor >> 5) & CONSTANTS.FIVE_BIT_MASK);
        var b = (byte)(encodedColor & CONSTANTS.FIVE_BIT_MASK);

        r = MathEx.ScaleRangeByteOptimized(
            r,
            0,
            CONSTANTS.FIVE_BIT_MASK,
            0,
            byte.MaxValue);

        g = MathEx.ScaleRangeByteOptimized(
            g,
            0,
            CONSTANTS.FIVE_BIT_MASK,
            0,
            byte.MaxValue);

        b = MathEx.ScaleRangeByteOptimized(
            b,
            0,
            CONSTANTS.FIVE_BIT_MASK,
            0,
            byte.MaxValue);

        return new SKColor(r, g, b);
    }

    /// <summary>
    ///     Decodes a 16-bit encoded RGB565 color value into an SKColor and scales the color to RGB888
    /// </summary>
    /// <param name="encodedColor">
    ///     The 16-bit encoded RGB555 color value
    /// </param>
    public static SKColor DecodeRgb565(ushort encodedColor)
    {
        var r = (byte)(encodedColor >> 11);
        var g = (byte)((encodedColor >> 5) & CONSTANTS.SIX_BIT_MASK);
        var b = (byte)(encodedColor & CONSTANTS.FIVE_BIT_MASK);

        r = MathEx.ScaleRangeByteOptimized(
            r,
            0,
            CONSTANTS.FIVE_BIT_MASK,
            0,
            byte.MaxValue);

        g = MathEx.ScaleRangeByteOptimized(
            g,
            0,
            CONSTANTS.SIX_BIT_MASK,
            0,
            byte.MaxValue);

        b = MathEx.ScaleRangeByteOptimized(
            b,
            0,
            CONSTANTS.FIVE_BIT_MASK,
            0,
            byte.MaxValue);

        return new SKColor(r, g, b);
    }

    /// <summary>
    ///     Encodes an SKColor into a 16 bit RGB555 encoded color
    /// </summary>
    /// <param name="color">
    ///     The SKColor to encode.
    /// </param>
    public static ushort EncodeRgb555(SKColor color)
    {
        var r = MathEx.ScaleRangeByteOptimized(
            color.Red,
            0,
            byte.MaxValue,
            0,
            CONSTANTS.FIVE_BIT_MASK);

        var g = MathEx.ScaleRangeByteOptimized(
            color.Green,
            0,
            byte.MaxValue,
            0,
            CONSTANTS.FIVE_BIT_MASK);

        var b = MathEx.ScaleRangeByteOptimized(
            color.Blue,
            0,
            byte.MaxValue,
            0,
            CONSTANTS.FIVE_BIT_MASK);

        return (ushort)((r << 10) | (g << 5) | b);
    }

    /// <summary>
    ///     Encodes an SKColor into a 16 bit RGB565 encoded color
    /// </summary>
    /// <param name="color">
    ///     The SKColor to encode.
    /// </param>
    public static ushort EncodeRgb565(SKColor color)
    {
        var r = MathEx.ScaleRangeByteOptimized(
            color.Red,
            0,
            byte.MaxValue,
            0,
            CONSTANTS.FIVE_BIT_MASK);

        var g = MathEx.ScaleRangeByteOptimized(
            color.Green,
            0,
            byte.MaxValue,
            0,
            CONSTANTS.SIX_BIT_MASK);

        var b = MathEx.ScaleRangeByteOptimized(
            color.Blue,
            0,
            byte.MaxValue,
            0,
            CONSTANTS.FIVE_BIT_MASK);

        return (ushort)((r << 11) | (g << 5) | b);
    }
}