using System;
using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x64 MiniGame (S->C), discriminated on [u8 Type]: types 3/4/8 carry [u8], type 7
///     carries [u32 BE][u32 BE], other types are bare. Modeled for protocol completeness; not emitted by
///     typical servers.
/// </summary>
public class MiniGamePacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(8)]
    public void ByteForm_WriteAndParse(byte type)
    {
        new MiniGamePacket { Type = type, Value = 0x2A }.ToBody().Should().Equal(type, (byte)0x2A);

        var parsed = MiniGamePacket.Parse([type, 0x2A]);
        parsed.Type.Should().Be(type);
        parsed.Value.Should().Be((byte)0x2A);
        parsed.First.Should().BeNull();
    }

    [Fact]
    public void PairForm_WriteAndParse()
    {
        new MiniGamePacket { Type = 7, First = 0x01020304, Second = 0x05060708 }
            .ToBody().Should().Equal(
                (byte)0x07,
                (byte)0x01, (byte)0x02, (byte)0x03, (byte)0x04,
                (byte)0x05, (byte)0x06, (byte)0x07, (byte)0x08);

        var parsed = MiniGamePacket.Parse([0x07, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08]);
        parsed.Type.Should().Be((byte)7);
        parsed.First.Should().Be(0x01020304u);
        parsed.Second.Should().Be(0x05060708u);
        parsed.Value.Should().BeNull();
    }

    [Fact]
    public void BareForm_WriteAndParse()
    {
        new MiniGamePacket { Type = 1 }.ToBody().Should().Equal((byte)0x01);

        var parsed = MiniGamePacket.Parse([0x01]);
        parsed.Type.Should().Be((byte)1);
        parsed.Value.Should().BeNull();
        parsed.First.Should().BeNull();
        parsed.Second.Should().BeNull();
    }

    [Fact]
    public void ByteForm_MissingValue_Throws()
    {
        var act = () => new MiniGamePacket { Type = 3 }.ToBody();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void PairForm_MissingFields_Throws()
    {
        var act = () => new MiniGamePacket { Type = 7, First = 1 }.ToBody();
        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void RoundTrip_ThroughCodec(MiniGamePacket original)
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();

        codec.ParseServerPacket(codec.EncodeServer(original, crypto), crypto).Should().Be(original);
    }

    public static TheoryData<MiniGamePacket> RoundTripCases() =>
    [
        new MiniGamePacket { Type = 4, Value = 0xFF },
        new MiniGamePacket { Type = 7, First = 0xDEADBEEF, Second = 0x12345678 },
        new MiniGamePacket { Type = 0 },
    ];
}
