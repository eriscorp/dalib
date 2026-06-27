using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Wire;

public class PacketWriterTests
{
    [Fact]
    public void NewWriter_BytesWritten_ShouldBeZero()
    {
        var writer = new PacketWriter();

        writer.BytesWritten.Should().Be(0);
        writer.WrittenSpan.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void WriteByte_ShouldEmitOneByteAndAdvance()
    {
        var writer = new PacketWriter();

        writer.WriteByte(0xAB);

        writer.BytesWritten.Should().Be(1);
        writer.ToArray().Should().Equal((byte)0xAB);
    }

    [Fact]
    public void WriteSByte_ShouldRoundTripNegativeValuesAsTwosComplement()
    {
        var writer = new PacketWriter();

        writer.WriteSByte(-1);
        writer.WriteSByte(-128);
        writer.WriteSByte(127);

        writer.ToArray().Should().Equal((byte)0xFF, (byte)0x80, (byte)0x7F);
    }

    [Fact]
    public void WriteBoolean_ShouldWriteOneOrZero()
    {
        var writer = new PacketWriter();

        writer.WriteBoolean(true);
        writer.WriteBoolean(false);

        writer.ToArray().Should().Equal((byte)0x01, (byte)0x00);
    }

    [Fact]
    public void WriteUInt16_ShouldEmitBigEndian()
    {
        var writer = new PacketWriter();

        writer.WriteUInt16(0x1234);

        writer.ToArray().Should().Equal((byte)0x12, (byte)0x34);
    }

    [Fact]
    public void WriteUInt16LE_ShouldEmitLittleEndian()
    {
        var writer = new PacketWriter();

        writer.WriteUInt16LE(0x1234);

        writer.ToArray().Should().Equal((byte)0x34, (byte)0x12);
    }

    [Fact]
    public void WriteInt16_ShouldEmitBigEndianTwosComplement()
    {
        var writer = new PacketWriter();

        writer.WriteInt16(-1);

        writer.ToArray().Should().Equal((byte)0xFF, (byte)0xFF);
    }

    [Fact]
    public void WriteUInt32_ShouldEmitBigEndian()
    {
        var writer = new PacketWriter();

        writer.WriteUInt32(0x11223344);

        writer.ToArray().Should().Equal((byte)0x11, (byte)0x22, (byte)0x33, (byte)0x44);
    }

    [Fact]
    public void WriteUInt32LE_ShouldEmitLittleEndian()
    {
        var writer = new PacketWriter();

        writer.WriteUInt32LE(0x11223344);

        writer.ToArray().Should().Equal((byte)0x44, (byte)0x33, (byte)0x22, (byte)0x11);
    }

    [Fact]
    public void WriteInt32_ShouldEmitBigEndianTwosComplement()
    {
        var writer = new PacketWriter();

        writer.WriteInt32(-1);

        writer.ToArray().Should().Equal((byte)0xFF, (byte)0xFF, (byte)0xFF, (byte)0xFF);
    }

    [Fact]
    public void WriteBytes_ShouldAppendVerbatim()
    {
        var writer = new PacketWriter();
        var payload = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

        writer.WriteBytes(payload);

        writer.ToArray().Should().Equal(payload);
    }

    [Fact]
    public void WriteBytes_OnEmptySpan_ShouldNotAdvance()
    {
        var writer = new PacketWriter();

        writer.WriteBytes(ReadOnlySpan<byte>.Empty);

        writer.BytesWritten.Should().Be(0);
    }

    [Fact]
    public void WriteString8_ShouldEmitLengthPrefixThenLatin1Bytes()
    {
        var writer = new PacketWriter();

        writer.WriteString8("hi");

        writer.ToArray().Should().Equal((byte)0x02, (byte)'h', (byte)'i');
    }

    [Fact]
    public void WriteString8_OnEmptyString_ShouldEmitZeroLength()
    {
        var writer = new PacketWriter();

        writer.WriteString8(string.Empty);

        writer.ToArray().Should().Equal((byte)0x00);
    }

    [Fact]
    public void WriteString8_OnOversizedString_ShouldThrow()
    {
        var writer = new PacketWriter();
        var tooLong = new string('a', 256);

        var act = () => writer.WriteString8(tooLong);

        act.Should().Throw<ArgumentException>().WithMessage("*256*");
    }

    [Fact]
    public void WriteString16_ShouldEmitU16BeLengthThenLatin1Bytes()
    {
        var writer = new PacketWriter();

        writer.WriteString16("hi");

        writer.ToArray().Should().Equal((byte)0x00, (byte)0x02, (byte)'h', (byte)'i');
    }

    [Fact]
    public void WriteCString_ShouldEmitBytesThenNullTerminator()
    {
        var writer = new PacketWriter();

        writer.WriteCString("hi");

        writer.ToArray().Should().Equal((byte)'h', (byte)'i', (byte)0x00);
    }

    [Fact]
    public void WriteCString_OnInputContainingNull_ShouldThrow()
    {
        var writer = new PacketWriter();

        var act = () => writer.WriteCString("hi\0there");

        act.Should().Throw<ArgumentException>().WithMessage("*null character*");
    }

    [Fact]
    public void Latin1_HighRangeBytes_ShouldRoundTripExactly()
    {
        // Latin-1 (ISO-8859-1) maps every byte 0..255 to a char with the same code point.
        // This is the property the wire format relies on for single-byte string handling.
        var writer = new PacketWriter();
        var roundTrip = new string([(char)0x80, (char)0xA3, (char)0xFF]);

        writer.WriteCString(roundTrip);

        writer.ToArray().Should().Equal((byte)0x80, (byte)0xA3, (byte)0xFF, (byte)0x00);
    }

    [Fact]
    public void ManyWrites_ShouldGrowBufferAndPreserveContent()
    {
        var writer = new PacketWriter(initialCapacity: 4);

        for (var i = 0; i < 1024; i++)
            writer.WriteByte((byte)(i & 0xFF));

        writer.BytesWritten.Should().Be(1024);

        var result = writer.ToArray();

        for (var i = 0; i < 1024; i++)
            result[i].Should().Be((byte)(i & 0xFF));
    }

    [Fact]
    public void WrittenSpan_ShouldReflectExactlyWhatHasBeenWritten()
    {
        var writer = new PacketWriter(initialCapacity: 64);

        writer.WriteByte(0x01);
        writer.WriteByte(0x02);
        writer.WriteByte(0x03);

        writer.WrittenSpan.Length.Should().Be(3);
        writer.WrittenSpan.ToArray().Should().Equal((byte)0x01, (byte)0x02, (byte)0x03);
    }

    [Fact]
    public void Constructor_WithNegativeCapacity_ShouldThrow()
    {
        var act = () => new PacketWriter(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
