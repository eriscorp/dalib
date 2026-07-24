using System.Buffers.Binary;
using SkiaSharp;

namespace Foscail.Conversion;

/// <summary>
///     Assembles an animated PNG from same-sized frames. SkiaSharp cannot write APNG, but APNG is
///     ordinary PNG plus three chunk types (acTL, fcTL, fdAT) — so each frame is encoded with Skia
///     and its IDAT payload spliced into the animation stream. All modern browsers and image viewers
///     play the result; decoders that predate APNG show the first frame.
/// </summary>
internal static class ApngEncoder
{
    private static readonly uint[] CrcTable = BuildCrcTable();

    /// <summary>Writes an infinitely-looping APNG. Frames must all have identical dimensions.</summary>
    public static void Write(string path, IReadOnlyList<SKImage> frames, int delayMs)
    {
        var encoded = new List<(byte[] Header, byte[] IdatData)>(frames.Count);

        foreach (var frame in frames)
        {
            using var data = frame.Encode(SKEncodedImageFormat.Png, 100);

            encoded.Add(SplitPng(data.ToArray()));
        }

        using var fs = File.Create(path);
        using var writer = new BinaryWriter(fs);

        writer.Write((ReadOnlySpan<byte>) [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        WriteChunk(writer, "IHDR"u8, encoded[0].Header);

        // acTL: frame count + 0 plays (loop forever)
        var actl = new byte[8];
        BinaryPrimitives.WriteUInt32BigEndian(actl, (uint)frames.Count);
        WriteChunk(writer, "acTL"u8, actl);

        var sequence = 0u;

        for (var i = 0; i < frames.Count; i++)
        {
            WriteChunk(writer, "fcTL"u8, BuildFctl(sequence++, frames[i].Width, frames[i].Height, delayMs));

            if (i == 0)
                WriteChunk(writer, "IDAT"u8, encoded[i].IdatData);
            else
            {
                // fdAT is an IDAT payload prefixed with its own sequence number
                var fdat = new byte[4 + encoded[i].IdatData.Length];
                BinaryPrimitives.WriteUInt32BigEndian(fdat, sequence++);
                encoded[i].IdatData.CopyTo(fdat, 4);
                WriteChunk(writer, "fdAT"u8, fdat);
            }
        }

        WriteChunk(writer, "IEND"u8, []);
    }

    private static byte[] BuildFctl(uint sequence, int width, int height, int delayMs)
    {
        var fctl = new byte[26];

        BinaryPrimitives.WriteUInt32BigEndian(fctl.AsSpan(0), sequence);
        BinaryPrimitives.WriteUInt32BigEndian(fctl.AsSpan(4), (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(fctl.AsSpan(8), (uint)height);

        // x/y offset 0; full-canvas frames with blend SOURCE fully replace the previous frame
        BinaryPrimitives.WriteUInt16BigEndian(fctl.AsSpan(20), (ushort)delayMs);
        BinaryPrimitives.WriteUInt16BigEndian(fctl.AsSpan(22), 1000);
        fctl[24] = 0; // dispose: none
        fctl[25] = 0; // blend: source

        return fctl;
    }

    /// <summary>Extracts the IHDR payload and the concatenated IDAT payloads from an encoded PNG.</summary>
    private static (byte[] Header, byte[] IdatData) SplitPng(byte[] png)
    {
        byte[]? header = null;
        using var idat = new MemoryStream();

        var pos = 8;

        while (pos + 8 <= png.Length)
        {
            var length = (int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(pos));
            var type = png.AsSpan(pos + 4, 4);

            if (type.SequenceEqual("IHDR"u8))
                header = png.AsSpan(pos + 8, length).ToArray();
            else if (type.SequenceEqual("IDAT"u8))
                idat.Write(png, pos + 8, length);

            pos += 12 + length;
        }

        return (header ?? throw new InvalidDataException("encoded PNG has no IHDR"), idat.ToArray());
    }

    private static void WriteChunk(BinaryWriter writer, ReadOnlySpan<byte> type, byte[] data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(length, (uint)data.Length);
        writer.Write(length);
        writer.Write(type);
        writer.Write(data);

        var crc = 0xFFFFFFFFu;

        foreach (var b in type)
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);

        foreach (var b in data)
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);

        Span<byte> crcBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc ^ 0xFFFFFFFFu);
        writer.Write(crcBytes);
    }

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];

        for (var n = 0u; n < 256; n++)
        {
            var c = n;

            for (var k = 0; k < 8; k++)
                c = (c & 1) == 1 ? 0xEDB88320 ^ (c >> 1) : c >> 1;

            table[n] = c;
        }

        return table;
    }
}
