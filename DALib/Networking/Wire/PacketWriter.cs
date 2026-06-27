using System;
using System.Buffers.Binary;
using System.Text;

namespace DALib.Networking.Wire;

/// <summary>
///     Default <see cref="IPacketWriter" /> implementation backed by a growable byte buffer.
/// </summary>
/// <remarks>
///     <para>
///         Instances are intended to be short-lived: a fresh writer per <c>Encode</c> call,
///         or per <see cref="IPacket.ToBody" /> invocation. The backing array is allocated
///         with a small initial capacity and doubled on overflow.
///     </para>
///     <para>
///         Not thread-safe.
///     </para>
/// </remarks>
public sealed class PacketWriter : IPacketWriter
{
    private const int DefaultInitialCapacity = 64;

    private byte[] _buffer;
    private int _count;

    /// <summary>
    ///     Creates a new <see cref="PacketWriter" /> with the default initial capacity.
    /// </summary>
    public PacketWriter()
        : this(DefaultInitialCapacity) { }

    /// <summary>
    ///     Creates a new <see cref="PacketWriter" /> with the specified initial capacity.
    /// </summary>
    public PacketWriter(int initialCapacity)
    {
        if (initialCapacity < 0)
            throw new ArgumentOutOfRangeException(nameof(initialCapacity));

        _buffer = new byte[Math.Max(initialCapacity, 1)];
        _count = 0;
    }

    /// <inheritdoc />
    public int BytesWritten => _count;

    /// <inheritdoc />
    public ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _count);

    /// <inheritdoc />
    public void WriteByte(byte value)
    {
        EnsureCapacity(_count + 1);
        _buffer[_count++] = value;
    }

    /// <inheritdoc />
    public void WriteSByte(sbyte value) => WriteByte(unchecked((byte)value));

    /// <inheritdoc />
    public void WriteBoolean(bool value) => WriteByte(value ? (byte)1 : (byte)0);

    /// <inheritdoc />
    public void WriteUInt16(ushort value)
    {
        EnsureCapacity(_count + 2);
        BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(_count, 2), value);
        _count += 2;
    }

    /// <inheritdoc />
    public void WriteUInt16LE(ushort value)
    {
        EnsureCapacity(_count + 2);
        BinaryPrimitives.WriteUInt16LittleEndian(_buffer.AsSpan(_count, 2), value);
        _count += 2;
    }

    /// <inheritdoc />
    public void WriteInt16(short value)
    {
        EnsureCapacity(_count + 2);
        BinaryPrimitives.WriteInt16BigEndian(_buffer.AsSpan(_count, 2), value);
        _count += 2;
    }

    /// <inheritdoc />
    public void WriteUInt32(uint value)
    {
        EnsureCapacity(_count + 4);
        BinaryPrimitives.WriteUInt32BigEndian(_buffer.AsSpan(_count, 4), value);
        _count += 4;
    }

    /// <inheritdoc />
    public void WriteUInt32LE(uint value)
    {
        EnsureCapacity(_count + 4);
        BinaryPrimitives.WriteUInt32LittleEndian(_buffer.AsSpan(_count, 4), value);
        _count += 4;
    }

    /// <inheritdoc />
    public void WriteInt32(int value)
    {
        EnsureCapacity(_count + 4);
        BinaryPrimitives.WriteInt32BigEndian(_buffer.AsSpan(_count, 4), value);
        _count += 4;
    }

    /// <inheritdoc />
    public void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
            return;

        EnsureCapacity(_count + bytes.Length);
        bytes.CopyTo(_buffer.AsSpan(_count));
        _count += bytes.Length;
    }

    /// <inheritdoc />
    public void WriteString8(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var byteCount = Encoding.Latin1.GetByteCount(value);

        if (byteCount > byte.MaxValue)
            throw new ArgumentException(
                $"String encodes to {byteCount} bytes, which exceeds the 255-byte limit for WriteString8.",
                nameof(value));

        EnsureCapacity(_count + 1 + byteCount);
        _buffer[_count++] = (byte)byteCount;
        Encoding.Latin1.GetBytes(value, _buffer.AsSpan(_count, byteCount));
        _count += byteCount;
    }

    /// <inheritdoc />
    public void WriteString16(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var byteCount = Encoding.Latin1.GetByteCount(value);

        if (byteCount > ushort.MaxValue)
            throw new ArgumentException(
                $"String encodes to {byteCount} bytes, which exceeds the 65535-byte limit for WriteString16.",
                nameof(value));

        EnsureCapacity(_count + 2 + byteCount);
        BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(_count, 2), (ushort)byteCount);
        _count += 2;
        Encoding.Latin1.GetBytes(value, _buffer.AsSpan(_count, byteCount));
        _count += byteCount;
    }

    /// <inheritdoc />
    public void WriteCString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.IndexOf('\0') >= 0)
            throw new ArgumentException(
                "WriteCString input must not contain a null character - it would be indistinguishable from the terminator.",
                nameof(value));

        var byteCount = Encoding.Latin1.GetByteCount(value);
        EnsureCapacity(_count + byteCount + 1);
        Encoding.Latin1.GetBytes(value, _buffer.AsSpan(_count, byteCount));
        _count += byteCount;
        _buffer[_count++] = 0x00;
    }

    /// <summary>
    ///     Returns a fresh <see cref="byte" /> array containing the bytes written so far.
    /// </summary>
    public byte[] ToArray() => WrittenSpan.ToArray();

    /// <summary>
    ///     Returns a <see cref="ReadOnlyMemory{T}" /> over a fresh copy of the bytes written
    ///     so far.
    /// </summary>
    public ReadOnlyMemory<byte> ToMemory() => ToArray();

    private void EnsureCapacity(int required)
    {
        if (required <= _buffer.Length)
            return;

        var next = _buffer.Length * 2;
        while (next < required)
            next *= 2;

        Array.Resize(ref _buffer, next);
    }
}
