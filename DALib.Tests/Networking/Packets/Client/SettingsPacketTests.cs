using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x1B Settings (C->S) - pins the single setting-number byte (0 = request all) and
///     the round-trip.
/// </summary>
public class SettingsPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(8)]
    public void WriteBody_IsSingleSettingByte(byte number)
    {
        var packet = new SettingsPacket { SettingNumber = number };

        packet.ToBody().Should().Equal(number);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesSettingNumber()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new SettingsPacket { SettingNumber = 5 };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<SettingsPacket>().Subject;
        typed.SettingNumber.Should().Be((byte)5);
    }
}
