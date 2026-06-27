using System;
using System.Buffers.Binary;
using System.Text;

namespace DALib.Networking.Wire;

/// <summary>
///     Forward-only reader over a packet body, used by <c>Parse</c> implementations.
/// </summary>
/// <remarks>
///     <para>
///         A <see langword="ref struct" /> over <see cref="ReadOnlySpan{T}" />: zero-copy
///         and zero-allocation for the reader itself. Cannot be stored in a class field
///         or cross an <see langword="await" /> boundary.
///     </para>
///     <para>
///         Endianness and string-encoding conventions mirror <see cref="PacketWriter" />:
///         big-endian by default, Latin-1 strings.
///     </para>
///     <para>
///         Reading past the end of the buffer throws <see cref="InvalidOperationException" />.
///     </para>
/// </remarks>
public ref struct PacketReader
{
    private readonly ReadOnlySpan<byte> _buffer;
    private int _position;

    /// <summary>
    ///     Creates a new <see cref="PacketReader" /> positioned at the start of
    ///     <paramref name="body" />.
    /// </summary>
    public PacketReader(ReadOnlySpan<byte> body)
    {
        _buffer = body;
        _position = 0;
    }

    /// <summary>
    ///     The current read offset within the buffer.
    /// </summary>
    public int Position => _position;

    /// <summary>
    ///     The total length of the underlying buffer.
    /// </summary>
    public int Length => _buffer.Length;

    /// <summary>
    ///     A view over the unread bytes (from <see cref="Position" /> to the end).
    /// </summary>
    public ReadOnlySpan<byte> Remaining => _buffer[_position..];

    /// <summary>Reads a single byte and advances.</summary>
    public byte ReadByte()
    {
        EnsureAvailable(1);

        return _buffer[_position++];
    }

    /// <summary>Reads a signed 8-bit value and advances.</summary>
    public sbyte ReadSByte() => unchecked((sbyte)ReadByte());

    /// <summary>Reads a single byte as a boolean (any non-zero value is true) and advances.</summary>
    public bool ReadBoolean() => ReadByte() != 0;

    /// <summary>Reads a 16-bit unsigned integer in big-endian byte order and advances.</summary>
    public ushort ReadUInt16()
    {
        EnsureAvailable(2);
        var value = BinaryPrimitives.ReadUInt16BigEndian(_buffer.Slice(_position, 2));
        _position += 2;

        return value;
    }

    /// <summary>Reads a 16-bit unsigned integer in little-endian byte order and advances.</summary>
    public ushort ReadUInt16LE()
    {
        EnsureAvailable(2);
        var value = BinaryPrimitives.ReadUInt16LittleEndian(_buffer.Slice(_position, 2));
        _position += 2;

        return value;
    }

    /// <summary>Reads a 16-bit signed integer in big-endian byte order and advances.</summary>
    public short ReadInt16()
    {
        EnsureAvailable(2);
        var value = BinaryPrimitives.ReadInt16BigEndian(_buffer.Slice(_position, 2));
        _position += 2;

        return value;
    }

    /// <summary>Reads a 32-bit unsigned integer in big-endian byte order and advances.</summary>
    public uint ReadUInt32()
    {
        EnsureAvailable(4);
        var value = BinaryPrimitives.ReadUInt32BigEndian(_buffer.Slice(_position, 4));
        _position += 4;

        return value;
    }

    /// <summary>Reads a 32-bit unsigned integer in little-endian byte order and advances.</summary>
    public uint ReadUInt32LE()
    {
        EnsureAvailable(4);
        var value = BinaryPrimitives.ReadUInt32LittleEndian(_buffer.Slice(_position, 4));
        _position += 4;

        return value;
    }

    /// <summary>Reads a 32-bit signed integer in big-endian byte order and advances.</summary>
    public int ReadInt32()
    {
        EnsureAvailable(4);
        var value = BinaryPrimitives.ReadInt32BigEndian(_buffer.Slice(_position, 4));
        _position += 4;

        return value;
    }

    /// <summary>
    ///     Reads <paramref name="count" /> bytes and returns them as a slice of the underlying
    ///     buffer (no copy). The returned span is valid only as long as the original buffer is.
    /// </summary>
    public ReadOnlySpan<byte> ReadBytes(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        EnsureAvailable(count);
        var slice = _buffer.Slice(_position, count);
        _position += count;

        return slice;
    }

    /// <summary>
    ///     Reads a string in <c>[u8 length][latin-1 bytes]</c> form.
    /// </summary>
    public string ReadString8()
    {
        var length = ReadByte();
        EnsureAvailable(length);
        var value = Encoding.Latin1.GetString(_buffer.Slice(_position, length));
        _position += length;

        return value;
    }

    /// <summary>
    ///     Reads a string in <c>[u16-BE length][latin-1 bytes]</c> form.
    /// </summary>
    public string ReadString16()
    {
        var length = ReadUInt16();
        EnsureAvailable(length);
        var value = Encoding.Latin1.GetString(_buffer.Slice(_position, length));
        _position += length;

        return value;
    }

    /// <summary>
    ///     Reads a null-terminated Latin-1 string. The terminator is consumed but not
    ///     included in the returned value.
    /// </summary>
    public string ReadCString()
    {
        var terminator = _buffer[_position..].IndexOf((byte)0x00);

        if (terminator < 0)
            throw new InvalidOperationException(
                "PacketReader: no null terminator found before end of buffer.");

        var value = Encoding.Latin1.GetString(_buffer.Slice(_position, terminator));
        _position += terminator + 1;

        return value;
    }

    private void EnsureAvailable(int count)
    {
        if (_position + count > _buffer.Length)
            throw new InvalidOperationException(
                $"PacketReader: attempted to read {count} byte(s) at position {_position}, but only {_buffer.Length - _position} byte(s) remain.");
    }
}
