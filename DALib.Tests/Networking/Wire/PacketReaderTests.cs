using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Wire;

public class PacketReaderTests
{
    [Fact]
    public void NewReader_ShouldStartAtPositionZero()
    {
        var reader = new PacketReader(new byte[] { 0x01, 0x02 });

        reader.Position.Should().Be(0);
        reader.Length.Should().Be(2);
        reader.Remaining.Length.Should().Be(2);
    }

    [Fact]
    public void ReadByte_ShouldReturnValueAndAdvance()
    {
        var reader = new PacketReader(new byte[] { 0xAB, 0xCD });

        reader.ReadByte().Should().Be(0xAB);
        reader.Position.Should().Be(1);
    }

    [Fact]
    public void ReadSByte_ShouldInterpretAsTwosComplement()
    {
        var reader = new PacketReader(new byte[] { 0xFF, 0x80, 0x7F });

        reader.ReadSByte().Should().Be(-1);
        reader.ReadSByte().Should().Be(-128);
        reader.ReadSByte().Should().Be(127);
    }

    [Fact]
    public void ReadBoolean_ShouldTreatAnyNonZeroAsTrue()
    {
        var reader = new PacketReader(new byte[] { 0x00, 0x01, 0xFF });

        reader.ReadBoolean().Should().BeFalse();
        reader.ReadBoolean().Should().BeTrue();
        reader.ReadBoolean().Should().BeTrue();
    }

    [Fact]
    public void ReadUInt16_ShouldDecodeBigEndian()
    {
        var reader = new PacketReader(new byte[] { 0x12, 0x34 });

        reader.ReadUInt16().Should().Be(0x1234);
        reader.Position.Should().Be(2);
    }

    [Fact]
    public void ReadUInt16LE_ShouldDecodeLittleEndian()
    {
        var reader = new PacketReader(new byte[] { 0x34, 0x12 });

        reader.ReadUInt16LE().Should().Be(0x1234);
    }

    [Fact]
    public void ReadInt16_ShouldDecodeBigEndianTwosComplement()
    {
        var reader = new PacketReader(new byte[] { 0xFF, 0xFF });

        reader.ReadInt16().Should().Be(-1);
    }

    [Fact]
    public void ReadUInt32_ShouldDecodeBigEndian()
    {
        var reader = new PacketReader(new byte[] { 0x11, 0x22, 0x33, 0x44 });

        reader.ReadUInt32().Should().Be(0x11223344u);
    }

    [Fact]
    public void ReadUInt32LE_ShouldDecodeLittleEndian()
    {
        var reader = new PacketReader(new byte[] { 0x44, 0x33, 0x22, 0x11 });

        reader.ReadUInt32LE().Should().Be(0x11223344u);
    }

    [Fact]
    public void ReadInt32_ShouldDecodeBigEndianTwosComplement()
    {
        var reader = new PacketReader(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });

        reader.ReadInt32().Should().Be(-1);
    }

    [Fact]
    public void ReadBytes_ShouldReturnSliceWithoutCopying()
    {
        var source = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var reader = new PacketReader(source);

        var slice = reader.ReadBytes(3);

        slice.Length.Should().Be(3);
        slice[0].Should().Be(0xDE);
        slice[1].Should().Be(0xAD);
        slice[2].Should().Be(0xBE);
        reader.Position.Should().Be(3);
    }

    [Fact]
    public void ReadBytes_WithNegativeCount_ShouldThrow()
    {
        var reader = new PacketReader(new byte[] { 0x01 });

        // Compiler refuses lambdas that capture a ref struct directly; wrap in a delegate
        // explicitly. Doing the read inline lets the ref struct stay on the stack.
        try
        {
            reader.ReadBytes(-1);
            Assert.Fail("Expected ArgumentOutOfRangeException");
        }
        catch (ArgumentOutOfRangeException)
        {
            // expected
        }
    }

    [Fact]
    public void ReadString8_ShouldDecodeLengthPrefixedString()
    {
        var reader = new PacketReader(new byte[] { 0x02, (byte)'h', (byte)'i' });

        reader.ReadString8().Should().Be("hi");
        reader.Position.Should().Be(3);
    }

    [Fact]
    public void ReadString8_OnZeroLength_ShouldReturnEmptyString()
    {
        var reader = new PacketReader(new byte[] { 0x00 });

        reader.ReadString8().Should().BeEmpty();
    }

    [Fact]
    public void ReadString16_ShouldDecodeU16BeLengthPrefixedString()
    {
        var reader = new PacketReader(new byte[] { 0x00, 0x02, (byte)'h', (byte)'i' });

        reader.ReadString16().Should().Be("hi");
        reader.Position.Should().Be(4);
    }

    [Fact]
    public void ReadCString_ShouldDecodeUpToNullTerminator()
    {
        var reader = new PacketReader(new byte[] { (byte)'h', (byte)'i', 0x00, 0xAA });

        reader.ReadCString().Should().Be("hi");
        reader.Position.Should().Be(3);
        reader.Remaining.Length.Should().Be(1);
        reader.Remaining[0].Should().Be(0xAA);
    }

    [Fact]
    public void ReadCString_WhenNoTerminatorPresent_ShouldThrow()
    {
        var reader = new PacketReader(new byte[] { (byte)'h', (byte)'i' });

        try
        {
            reader.ReadCString();
            Assert.Fail("Expected InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
            // expected
        }
    }

    [Fact]
    public void ReadByte_PastEnd_ShouldThrow()
    {
        var reader = new PacketReader(new byte[] { 0x01 });
        reader.ReadByte();

        try
        {
            reader.ReadByte();
            Assert.Fail("Expected InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
            // expected
        }
    }

    [Fact]
    public void Remaining_ShouldShrinkAsBytesAreConsumed()
    {
        var reader = new PacketReader(new byte[] { 0x01, 0x02, 0x03, 0x04 });

        reader.Remaining.Length.Should().Be(4);
        reader.ReadByte();
        reader.Remaining.Length.Should().Be(3);
        reader.ReadUInt16();
        reader.Remaining.Length.Should().Be(1);
    }

    [Fact]
    public void Latin1_HighRangeBytes_ShouldRoundTripExactly()
    {
        var reader = new PacketReader(new byte[] { 0x80, 0xA3, 0xFF, 0x00 });

        var value = reader.ReadCString();

        ((int)value[0]).Should().Be(0x80);
        ((int)value[1]).Should().Be(0xA3);
        ((int)value[2]).Should().Be(0xFF);
    }
}
