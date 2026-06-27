using System;

namespace DALib.Networking.Wire;

/// <summary>
///     Writer interface used by packets to emit their plaintext body bytes.
/// </summary>
/// <remarks>
///     <para>
///         Multi-byte integers default to big-endian (the on-wire convention for DOOMVAS v1).
///         Little-endian helpers are provided for the handful of fields that use LE
///         (notably parts of the 0x03 Login integrity trailer).
///     </para>
///     <para>
///         Strings are encoded as Latin-1 (ISO-8859-1, a strict superset of ASCII over the
///         single-byte range). The wire is single-byte; multi-byte encodings are not supported.
///     </para>
/// </remarks>
public interface IPacketWriter
{
    /// <summary>
    ///     Number of bytes written so far.
    /// </summary>
    int BytesWritten { get; }

    /// <summary>
    ///     A view over the bytes written so far. Valid only until the next write call.
    /// </summary>
    ReadOnlySpan<byte> WrittenSpan { get; }

    /// <summary>Writes a single byte.</summary>
    void WriteByte(byte value);

    /// <summary>Writes a signed 8-bit value.</summary>
    void WriteSByte(sbyte value);

    /// <summary>Writes a boolean as a single byte (<c>1</c> for true, <c>0</c> for false).</summary>
    void WriteBoolean(bool value);

    /// <summary>Writes a 16-bit unsigned integer in big-endian byte order.</summary>
    void WriteUInt16(ushort value);

    /// <summary>Writes a 16-bit unsigned integer in little-endian byte order.</summary>
    void WriteUInt16LE(ushort value);

    /// <summary>Writes a 16-bit signed integer in big-endian byte order.</summary>
    void WriteInt16(short value);

    /// <summary>Writes a 32-bit unsigned integer in big-endian byte order.</summary>
    void WriteUInt32(uint value);

    /// <summary>Writes a 32-bit unsigned integer in little-endian byte order.</summary>
    void WriteUInt32LE(uint value);

    /// <summary>Writes a 32-bit signed integer in big-endian byte order.</summary>
    void WriteInt32(int value);

    /// <summary>Writes the bytes in <paramref name="bytes" /> verbatim.</summary>
    void WriteBytes(ReadOnlySpan<byte> bytes);

    /// <summary>
    ///     Writes a string as <c>[u8 length][latin-1 bytes]</c>. The string must encode to
    ///     no more than 255 bytes; longer values throw <see cref="ArgumentException" />.
    /// </summary>
    void WriteString8(string value);

    /// <summary>
    ///     Writes a string as <c>[u16-BE length][latin-1 bytes]</c>. The string must encode
    ///     to no more than 65535 bytes; longer values throw <see cref="ArgumentException" />.
    /// </summary>
    void WriteString16(string value);

    /// <summary>
    ///     Writes a string as <c>[latin-1 bytes][0x00 terminator]</c>. The string itself
    ///     must not contain a null byte; doing so throws <see cref="ArgumentException" />.
    /// </summary>
    void WriteCString(string value);
}
