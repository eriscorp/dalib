using DALib.Definitions;
using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

public class UserAppearancePacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_PinsKnownLayout()
    {
        var packet = new UserAppearancePacket
        {
            Id = 0x12345678,
            Direction = 2,
            Unknown1 = 0,
            Class = 4,
            Flags = 0,
            Gender = Gender.Male,
        };

        // [Id u32 BE 12 34 56 78][Dir 02][Unknown1 00][Class 04][Flags 00][Gender 01]
        packet.ToBody().Should().Equal(
            (byte)0x12, (byte)0x34, (byte)0x56, (byte)0x78,
            (byte)0x02,
            (byte)0x00,
            (byte)0x04,
            (byte)0x00,
            (byte)0x01);
    }

    [Fact]
    public void WriteBody_NineByteLength()
    {
        // The body is exactly 9 bytes.
        var packet = new UserAppearancePacket
        {
            Id = 1, Direction = 0, Unknown1 = 0, Class = 0, Flags = 0, Gender = Gender.Neutral,
        };

        packet.ToBody().Length.Should().Be(9);
    }

    [Fact]
    public void RoundTrip_PreservesAllFields()
    {
        var original = new UserAppearancePacket
        {
            Id = 0xDEADBEEF,
            Direction = 3,
            Unknown1 = 0x42,
            Class = 7,
            Flags = UserAppearancePacket.FlagDoNotCacheSelfId,
            Gender = Gender.Female,
        };

        var parsed = UserAppearancePacket.Parse(original.ToBody());

        parsed.Id.Should().Be(0xDEADBEEF);
        parsed.Direction.Should().Be(3);
        parsed.Unknown1.Should().Be(0x42);
        parsed.Class.Should().Be(7);
        parsed.Flags.Should().Be(UserAppearancePacket.FlagDoNotCacheSelfId);
        parsed.Gender.Should().Be(Gender.Female);
    }

    [Fact]
    public void BuildUpStyle_OmittedFieldsDefaultToZero()
    {
        // Pins the build-as-you-go API shape: only Id is required at construction;
        // remaining fields can be mutated after the fact or left at their zero defaults.
        var packet = new UserAppearancePacket { Id = 1 };
        packet.Direction = 2;

        packet.ToBody().Should().Equal(
            (byte)0x00, (byte)0x00, (byte)0x00, (byte)0x01,
            (byte)0x02,
            (byte)0x00,
            (byte)0x00,
            (byte)0x00,
            (byte)0x00);
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new UserAppearancePacket
        {
            Id = 999,
            Direction = 1,
            Unknown1 = 0,
            Class = 5,
            Flags = 0,
            Gender = Gender.Neutral,
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<UserAppearancePacket>().Subject;
        typed.Id.Should().Be(999);
        typed.Direction.Should().Be(1);
        typed.Class.Should().Be(5);
        typed.Gender.Should().Be(Gender.Neutral);
    }
}
