using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x6D FamilyName (S->C) - a single string8 body. Modeled for protocol
///     completeness; not emitted by typical servers.
/// </summary>
public class FamilyNamePacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsString8Layout()
    {
        // "AB" -> [len=2]['A']['B']
        new FamilyNamePacket { Name = "AB" }.ToBody().Should().Equal((byte)0x02, (byte)'A', (byte)'B');
    }

    [Fact]
    public void Parse_ReadsString8()
    {
        FamilyNamePacket.Parse([0x02, (byte)'A', (byte)'B']).Name.Should().Be("AB");
    }

    [Theory]
    [InlineData("")]
    [InlineData("Aisling")]
    public void RoundTrip_ThroughCodec_PreservesName(string name)
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new FamilyNamePacket { Name = name };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        parsed.Should().BeOfType<FamilyNamePacket>().Subject.Name.Should().Be(name);
    }
}
