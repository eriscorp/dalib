using DALib.Definitions;
using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x04 CreateCharFinalize (C->S) - pins the three appearance bytes in
///     [HairStyle][Gender][HairColor] order and the round-trip.
/// </summary>
public class CreateCharFinalizePacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsHairStyleGenderHairColorOrder()
    {
        var packet = new CreateCharFinalizePacket
        {
            HairStyle = 7,
            Gender = Gender.Female,
            HairColor = 11,
        };

        // Gender.Female encodes as the wire byte 0x02.
        packet.ToBody().Should().Equal((byte)7, (byte)2, (byte)11);
    }

    [Theory]
    [InlineData(1, Gender.Neutral, 0)]
    [InlineData(1, Gender.Male, 0)]
    [InlineData(17, Gender.Female, 13)]
    public void RoundTrip_PreservesAllFields(byte hairStyle, Gender gender, byte hairColor)
    {
        var original = new CreateCharFinalizePacket
        {
            HairStyle = hairStyle,
            Gender = gender,
            HairColor = hairColor,
        };

        var parsed = CreateCharFinalizePacket.Parse(original.ToBody());

        parsed.HairStyle.Should().Be(hairStyle);
        parsed.Gender.Should().Be(gender);
        parsed.HairColor.Should().Be(hairColor);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new CreateCharFinalizePacket
        {
            HairStyle = 4,
            Gender = Gender.Male,
            HairColor = 9,
        };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<CreateCharFinalizePacket>().Subject;
        typed.HairStyle.Should().Be(4);
        typed.Gender.Should().Be(Gender.Male);
        typed.HairColor.Should().Be(9);
    }
}
