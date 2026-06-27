using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Wire;

public class CrcCcittTests
{
    [Fact]
    public void Compute_OnEmptyInput_ShouldReturnZero()
    {
        CrcCcitt.Compute(ReadOnlySpan<byte>.Empty).Should().Be((ushort)0);
    }

    [Fact]
    public void Compute_OnHandVerifiedVectors_ShouldReturnExpectedValues()
    {
        // Note: this is NOT standard CRC-16/XMODEM despite the poly+init match.
        // This variant's step:
        //   crc' = table[(crc>>8) & 0xff] ^ (crc<<8) ^ data
        // Standard XMODEM step:
        //   crc' = table[((crc>>8) ^ data) & 0xff] ^ (crc<<8)
        // Because of the variant, the standard XMODEM check value 0x31C3 for
        // "123456789" does not apply. These vectors are hand-computed for the
        // variant and serve as a regression check.
        CrcCcitt.Compute([0x31]).Should().Be((ushort)0x0031);             // table[0]=0, so the byte just XORs in
        CrcCcitt.Compute([0x31, 0x32]).Should().Be((ushort)0x3132);       // ditto, still in the table[0] regime
        CrcCcitt.Compute([0x31, 0x32, 0x33]).Should().Be((ushort)0x1441); // table[0x31] (=0x2672) kicks in
    }

    [Fact]
    public void Compute_ShouldEqualIteratedStep()
    {
        var data = new byte[] { 0xAB, 0xCD, 0xEF, 0x12, 0x34, 0x56, 0x78 };

        var iterated = (ushort)0;
        foreach (var b in data)
            iterated = CrcCcitt.Step(iterated, b);

        CrcCcitt.Compute(data).Should().Be(iterated);
    }

    [Fact]
    public void Compute_WithNonZeroInitial_ShouldOffsetResult()
    {
        var data = new byte[] { 0x00 };

        // First step with initial=0xFFFF: table[0xFF] ^ (0xFFFF << 8) ^ 0x00.
        var withZero = CrcCcitt.Compute(data, initial: 0x0000);
        var withFFFF = CrcCcitt.Compute(data, initial: 0xFFFF);

        withZero.Should().NotBe(withFFFF);
    }

    [Fact]
    public void HeartbeatHash_ShouldFeedLowByteFirstThenHighByte()
    {
        // Heartbeat hash: the server's 0x3B two bytes are read as u16-BE
        // (val = (a << 8) | b), then LOW (b) is fed first and HIGH (a) second into the CRC.
        const byte a = 0x12;
        const byte b = 0x34;

        var manual = CrcCcitt.Step(CrcCcitt.Step(0, b), a);

        CrcCcitt.HeartbeatHash(a, b).Should().Be(manual);
    }

    [Theory]
    [InlineData((byte)0x00, (byte)0x00)]
    [InlineData((byte)0xFF, (byte)0x00)]
    [InlineData((byte)0x00, (byte)0xFF)]
    [InlineData((byte)0xAB, (byte)0xCD)]
    [InlineData((byte)0x12, (byte)0x34)]
    public void HeartbeatHash_ShouldBeDeterministic(byte a, byte b)
    {
        var first = CrcCcitt.HeartbeatHash(a, b);
        var second = CrcCcitt.HeartbeatHash(a, b);

        second.Should().Be(first);
    }

    [Fact]
    public void Step_OnZeroState_WithSingleByte_ShouldXorInTheByte()
    {
        // step(0, x) = table[0] ^ 0 ^ x = 0 ^ 0 ^ x = x.
        // Table[0] is 0x0000 by construction (no shift-and-xor iterations fire when
        // the seed byte is 0).
        CrcCcitt.Step(0, 0xAB).Should().Be((ushort)0x00AB);
        CrcCcitt.Step(0, 0x00).Should().Be((ushort)0x0000);
        CrcCcitt.Step(0, 0xFF).Should().Be((ushort)0x00FF);
    }
}
