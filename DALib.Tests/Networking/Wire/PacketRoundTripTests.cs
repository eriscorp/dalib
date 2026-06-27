using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Wire;

/// <summary>
///     Verifies that <see cref="PacketWriter" /> output decodes cleanly through
///     <see cref="PacketReader" /> for every primitive. These are the bedrock
///     symmetry tests every packet implementation will rest on.
/// </summary>
public class PacketRoundTripTests
{
    [Theory]
    [InlineData((byte)0x00)]
    [InlineData((byte)0x01)]
    [InlineData((byte)0x7F)]
    [InlineData((byte)0x80)]
    [InlineData((byte)0xFF)]
    public void Byte_RoundTrip(byte value)
    {
        var writer = new PacketWriter();
        writer.WriteByte(value);
        var reader = new PacketReader(writer.WrittenSpan);

        reader.ReadByte().Should().Be(value);
    }

    [Theory]
    [InlineData((sbyte)0)]
    [InlineData((sbyte)127)]
    [InlineData((sbyte)-1)]
    [InlineData((sbyte)-128)]
    public void SByte_RoundTrip(sbyte value)
    {
        var writer = new PacketWriter();
        writer.WriteSByte(value);
        var reader = new PacketReader(writer.WrittenSpan);

        reader.ReadSByte().Should().Be(value);
    }

    [Theory]
    [InlineData((ushort)0x0000)]
    [InlineData((ushort)0x00FF)]
    [InlineData((ushort)0x1234)]
    [InlineData((ushort)0xFFFF)]
    public void UInt16_BigEndian_RoundTrip(ushort value)
    {
        var writer = new PacketWriter();
        writer.WriteUInt16(value);
        var reader = new PacketReader(writer.WrittenSpan);

        reader.ReadUInt16().Should().Be(value);
    }

    [Theory]
    [InlineData((ushort)0x0000)]
    [InlineData((ushort)0x1234)]
    [InlineData((ushort)0xFFFF)]
    public void UInt16_LittleEndian_RoundTrip(ushort value)
    {
        var writer = new PacketWriter();
        writer.WriteUInt16LE(value);
        var reader = new PacketReader(writer.WrittenSpan);

        reader.ReadUInt16LE().Should().Be(value);
    }

    [Theory]
    [InlineData((short)0)]
    [InlineData((short)32767)]
    [InlineData((short)-1)]
    [InlineData((short)-32768)]
    public void Int16_BigEndian_RoundTrip(short value)
    {
        var writer = new PacketWriter();
        writer.WriteInt16(value);
        var reader = new PacketReader(writer.WrittenSpan);

        reader.ReadInt16().Should().Be(value);
    }

    [Theory]
    [InlineData(0x00000000u)]
    [InlineData(0x000000FFu)]
    [InlineData(0x11223344u)]
    [InlineData(0xFFFFFFFFu)]
    public void UInt32_BigEndian_RoundTrip(uint value)
    {
        var writer = new PacketWriter();
        writer.WriteUInt32(value);
        var reader = new PacketReader(writer.WrittenSpan);

        reader.ReadUInt32().Should().Be(value);
    }

    [Theory]
    [InlineData(0x00000000u)]
    [InlineData(0x11223344u)]
    [InlineData(0xFFFFFFFFu)]
    public void UInt32_LittleEndian_RoundTrip(uint value)
    {
        var writer = new PacketWriter();
        writer.WriteUInt32LE(value);
        var reader = new PacketReader(writer.WrittenSpan);

        reader.ReadUInt32LE().Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(int.MaxValue)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Int32_BigEndian_RoundTrip(int value)
    {
        var writer = new PacketWriter();
        writer.WriteInt32(value);
        var reader = new PacketReader(writer.WrittenSpan);

        reader.ReadInt32().Should().Be(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("a")]
    public void String8_RoundTrip(string value)
    {
        var writer = new PacketWriter();
        writer.WriteString8(value);
        var reader = new PacketReader(writer.WrittenSpan);

        reader.ReadString8().Should().Be(value);
    }

    [Fact]
    public void String8_AtMaxLength_RoundTrip()
    {
        var value = new string('a', 255);
        var writer = new PacketWriter();
        writer.WriteString8(value);
        var reader = new PacketReader(writer.WrittenSpan);

        reader.ReadString8().Should().Be(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("the quick brown fox jumps over the lazy dog")]
    public void String16_RoundTrip(string value)
    {
        var writer = new PacketWriter();
        writer.WriteString16(value);
        var reader = new PacketReader(writer.WrittenSpan);

        reader.ReadString16().Should().Be(value);
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("a")]
    public void CString_RoundTrip(string value)
    {
        var writer = new PacketWriter();
        writer.WriteCString(value);
        var reader = new PacketReader(writer.WrittenSpan);

        reader.ReadCString().Should().Be(value);
    }

    [Fact]
    public void MixedSequence_RoundTrip()
    {
        // Synthetic packet body: u8 type, u16-BE id, string8 name, u32-BE flags.
        // This shape is representative of many real packets.
        var writer = new PacketWriter();
        writer.WriteByte(0x02);
        writer.WriteUInt16(0xABCD);
        writer.WriteString8("Aisling");
        writer.WriteUInt32(0xDEADBEEF);

        var reader = new PacketReader(writer.WrittenSpan);

        reader.ReadByte().Should().Be(0x02);
        reader.ReadUInt16().Should().Be(0xABCD);
        reader.ReadString8().Should().Be("Aisling");
        reader.ReadUInt32().Should().Be(0xDEADBEEFu);
        reader.Remaining.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void ToBody_OnClientPacket_ShouldReturnExactlyWhatWriteBodyEmits()
    {
        var packet = new TestClientPacket { Marker = 0x42 };

        packet.ToBody().Should().Equal((byte)0x42, (byte)'o', (byte)'k');
    }

    [Fact]
    public void ToBodyMemory_OnServerPacket_ShouldReturnSameBytesAsToBody()
    {
        var packet = new TestServerPacket { Marker = 0xAA };

        var array = packet.ToBody();
        var memory = packet.ToBodyMemory();

        memory.ToArray().Should().Equal(array);
    }

    private sealed record TestClientPacket : ClientPacket
    {
        public byte Marker { get; init; }
        public override byte Opcode => 0xFE;

        public override void WriteBody(IPacketWriter writer)
        {
            writer.WriteByte(Marker);
            writer.WriteByte((byte)'o');
            writer.WriteByte((byte)'k');
        }
    }

    private sealed record TestServerPacket : ServerPacket
    {
        public byte Marker { get; init; }
        public override byte Opcode => 0xFE;

        public override void WriteBody(IPacketWriter writer)
        {
            writer.WriteByte(Marker);
            writer.WriteUInt16(0xBEEF);
        }
    }
}
