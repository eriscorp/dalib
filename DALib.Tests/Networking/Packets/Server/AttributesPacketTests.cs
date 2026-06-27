using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

public class AttributesPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_EmptyPacket_OneFlagByte()
    {
        // The minimal valid packet: no sections, no flags set.
        var packet = new AttributesPacket();
        packet.ToBody().Should().Equal((byte)0x00);
    }

    [Fact]
    public void WriteBody_UnreadMailOnly_FlagBit0()
    {
        // Mail-only notification: no sections, just the UnreadMail flag.
        var packet = new AttributesPacket { UnreadMail = true };
        packet.ToBody().Should().Equal((byte)0x01);
    }

    [Theory]
    [InlineData(0, 0x00)]
    [InlineData(1, 0x40)]
    [InlineData(2, 0x80)]
    [InlineData(3, 0xC0)]
    public void WriteBody_MovementMode_EncodesInBits6and7(byte mode, byte expectedFlag)
    {
        var packet = new AttributesPacket { MovementMode = mode };
        packet.ToBody().Should().Equal(expectedFlag);
    }

    [Fact]
    public void WriteBody_MovementModeOutOfRange_Throws()
    {
        var packet = new AttributesPacket { MovementMode = 4 };
        Action act = () => packet.ToBody();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MovementMode 4*");
    }

    [Fact]
    public void WriteBody_CurrentOnly_FlagPlus8Bytes()
    {
        // Build-up shape: HP/MP update after damage tick.
        var packet = new AttributesPacket
        {
            Current = new CurrentAttributes { Hp = 100, Mp = 50 },
        };

        // [flag 0x10][Hp u32 BE 00 00 00 64][Mp u32 BE 00 00 00 32]
        packet.ToBody().Should().Equal(
            (byte)0x10,
            (byte)0x00, (byte)0x00, (byte)0x00, (byte)0x64,
            (byte)0x00, (byte)0x00, (byte)0x00, (byte)0x32);
    }

    [Fact]
    public void WriteBody_PrimaryHasUnspentFlagAutoDerived_NonZero()
    {
        var packet = new AttributesPacket
        {
            Primary = new PrimaryAttributes
            {
                Magic0 = 1, Magic1 = 0, Magic2 = 0,
                Level = 50, Ability = 0,
                MaxHp = 1000, MaxMp = 500,
                Str = 20, Int = 15, Wis = 10, Con = 25, Dex = 18,
                UnspentPoints = 5,
                MaxWeight = 100, CurrentWeight = 42,
                UnknownTrailing = 0,
            },
        };

        var body = packet.ToBody();
        // Flag (0x20 = Primary) + 28 byte Primary = 29 bytes total.
        body.Length.Should().Be(29);

        // After 1-byte flag + 18 bytes of Primary header (3 magic + 2 level/ability + 8 max
        // hp/mp + 5 stats), the "has unspent" flag byte is at body[19] and the actual count
        // is at body[20].
        body[19].Should().Be(0x01);
        body[20].Should().Be(0x05);
    }

    [Fact]
    public void WriteBody_PrimaryHasUnspentFlagAutoDerived_Zero()
    {
        var packet = new AttributesPacket
        {
            Primary = new PrimaryAttributes { UnspentPoints = 0 },
        };

        var body = packet.ToBody();
        body[19].Should().Be(0x00);
        body[20].Should().Be(0x00);
    }

    [Fact]
    public void WriteBody_AllSections_LengthIs74()
    {
        // 1 flag + 28 Primary + 8 Current + 24 Experience + 13 Secondary = 74.
        var packet = new AttributesPacket
        {
            Primary = new PrimaryAttributes(),
            Current = new CurrentAttributes(),
            Experience = new ExperienceAttributes(),
            Secondary = new SecondaryAttributes(),
        };

        packet.ToBody().Length.Should().Be(74);
        packet.ToBody()[0].Should().Be(0x3C);  // 0x20 | 0x10 | 0x08 | 0x04
    }

    [Fact]
    public void WriteBody_SecondaryAcSigned_RoundTripsNegative()
    {
        var packet = new AttributesPacket
        {
            Secondary = new SecondaryAttributes { Ac = -50 },
        };

        var parsed = AttributesPacket.Parse(packet.ToBody());
        parsed.Secondary!.Ac.Should().Be(-50);
    }

    [Fact]
    public void RoundTrip_FullyPopulated_PreservesAllFields()
    {
        var original = new AttributesPacket
        {
            MovementMode = 1,
            UnreadMail = true,
            Primary = new PrimaryAttributes
            {
                Level = 99, Ability = 75,
                MaxHp = 5000, MaxMp = 2500,
                Str = 50, Int = 40, Wis = 30, Con = 60, Dex = 45,
                UnspentPoints = 3,
                MaxWeight = 200, CurrentWeight = 150,
            },
            Current = new CurrentAttributes { Hp = 4500, Mp = 2000 },
            Experience = new ExperienceAttributes
            {
                Experience = 1_000_000,
                ExpToLevel = 1_500_000,
                AbilityExp = 500_000,
                NextAB = 0,
                Gp = 0,
                Gold = 12_345_678,
            },
            Secondary = new SecondaryAttributes
            {
                Blinded = SecondaryAttributes.BlindedActive,
                MailStatus = SecondaryAttributes.MailFlagMail,
                OffensiveElement = 3,
                DefensiveElement = 4,
                MrRating = 25,
                Ac = -10,
                DmgRating = 50,
                HitRating = 40,
            },
        };

        var parsed = AttributesPacket.Parse(original.ToBody());

        parsed.MovementMode.Should().Be(1);
        parsed.UnreadMail.Should().BeTrue();

        parsed.Primary!.Level.Should().Be(99);
        parsed.Primary.MaxHp.Should().Be(5000);
        parsed.Primary.Str.Should().Be(50);
        parsed.Primary.UnspentPoints.Should().Be(3);
        parsed.Primary.MaxWeight.Should().Be(200);

        parsed.Current!.Hp.Should().Be(4500);
        parsed.Current.Mp.Should().Be(2000);

        parsed.Experience!.Experience.Should().Be(1_000_000);
        parsed.Experience.Gold.Should().Be(12_345_678);

        parsed.Secondary!.Blinded.Should().Be(SecondaryAttributes.BlindedActive);
        parsed.Secondary.MailStatus.Should().Be(SecondaryAttributes.MailFlagMail);
        parsed.Secondary.Ac.Should().Be(-10);
    }

    [Fact]
    public void RoundTrip_PartialSections_ParsesCorrectly()
    {
        // Just Current - the most common narrow update.
        var original = new AttributesPacket
        {
            Current = new CurrentAttributes { Hp = 250, Mp = 100 },
        };

        var parsed = AttributesPacket.Parse(original.ToBody());

        parsed.Primary.Should().BeNull();
        parsed.Current.Should().NotBeNull();
        parsed.Current!.Hp.Should().Be(250);
        parsed.Current.Mp.Should().Be(100);
        parsed.Experience.Should().BeNull();
        parsed.Secondary.Should().BeNull();
    }

    [Fact]
    public void RoundTrip_ReservedFlag_Preserved()
    {
        // The 0x02 bit has no effect but is round-tripped for wire fidelity.
        var original = new AttributesPacket { ReservedFlag = true };

        var parsed = AttributesPacket.Parse(original.ToBody());

        parsed.ReservedFlag.Should().BeTrue();
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new AttributesPacket
        {
            MovementMode = 2,
            Current = new CurrentAttributes { Hp = 999, Mp = 333 },
            Secondary = new SecondaryAttributes { Ac = -25, MrRating = 7 },
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<AttributesPacket>().Subject;
        typed.MovementMode.Should().Be(2);
        typed.Current!.Hp.Should().Be(999);
        typed.Secondary!.Ac.Should().Be(-25);
        typed.Secondary.MrRating.Should().Be(7);
    }

    [Fact]
    public void BuildUpStyle_AddSectionsIncrementally()
    {
        // Pin the build-up API shape: construct empty, then attach updates.
        var packet = new AttributesPacket();
        packet.Current = new CurrentAttributes { Hp = 100, Mp = 50 };
        packet.UnreadMail = true;

        var parsed = AttributesPacket.Parse(packet.ToBody());
        parsed.Current!.Hp.Should().Be(100);
        parsed.UnreadMail.Should().BeTrue();
        parsed.Primary.Should().BeNull();
    }
}
