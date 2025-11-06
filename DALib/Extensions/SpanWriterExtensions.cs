using DALib.Memory;
using DALib.Utility;
using SkiaSharp;

namespace DALib.Extensions;

/// <summary>
///     Provides extension methods for SpanWriters
/// </summary>
public static class SpanWriterExtensions
{
    /// <summary>
    ///     Writes the given SKColor as a 16-bit RGB555 encoded color to the SpanWriter.
    /// </summary>
    /// <param name="writer">
    ///     The SpanWriter to write the color to.
    /// </param>
    /// <param name="color">
    ///     The RGB888 color to encode and write
    /// </param>
    public static void WriteRgb555Color(ref this SpanWriter writer, SKColor color) => writer.WriteUInt16(ColorCodec.EncodeRgb555(color));

    /// <summary>
    ///     Writes the given SKColor as a 16-bit RGB565 encoded color to the SpanWriter.
    /// </summary>
    /// <param name="writer">
    ///     The SpanWriter to write the color to.
    /// </param>
    /// <param name="color">
    ///     The RGB888 color to encode and write
    /// </param>
    public static void WriteRgb565Color(ref this SpanWriter writer, SKColor color) => writer.WriteUInt16(ColorCodec.EncodeRgb565(color));
}