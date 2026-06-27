using System.IO;
using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x63 Group (S->C) - group invite / recruitment notifications. Pins each form's wire
///     layout, verifies type dispatch over the leading byte (the shared-shape Ask/RecruitAsk prompt
///     forms and the RecruitInfo block), confirms trailing-slack tolerance, and round-trips through the
///     codec.
/// </summary>
public class GroupResponsePacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    private static GroupRecruitInfo SampleRecruit() => new()
    {
        RecruiterName = "Ann",
        GroupName = "Pals",
        Note = "Hi",
        StartingLevel = 1,
        EndingLevel = 99,
        WarriorsWanted = 2, CurrentWarriors = 1,
        WizardsWanted = 1, CurrentWizards = 0,
        RoguesWanted = 0, CurrentRogues = 0,
        PriestsWanted = 1, CurrentPriests = 1,
        MonksWanted = 0, CurrentMonks = 0,
    };

    // ---- layout pins --------------------------------------------------------------------------

    [Fact]
    public void Ask_PinsLayout()
    {
        new GroupPromptPacket { ResponseType = GroupResponseType.Ask, SourceName = "Bob" }
            .ToBody().Should().Equal(0x01, 0x03, 0x42, 0x6F, 0x62);
    }

    [Fact]
    public void RecruitAsk_PinsLayout()
    {
        new GroupPromptPacket { ResponseType = GroupResponseType.RecruitAsk, SourceName = "Bob" }
            .ToBody().Should().Equal(0x05, 0x03, 0x42, 0x6F, 0x62);
    }

    [Fact]
    public void RecruitInfo_PinsLayout()
    {
        // [04][string8 recruiter][string8 group][string8 note][u8 start][u8 end][5x(u8 wanted, u8 have)]
        new GroupRecruitInfoPacket { ResponseType = GroupResponseType.RecruitInfo, Info = SampleRecruit() }
            .ToBody().Should().Equal(
                0x04,
                0x03, 0x41, 0x6E, 0x6E,                    // "Ann"
                0x04, 0x50, 0x61, 0x6C, 0x73,              // "Pals"
                0x02, 0x48, 0x69,                          // "Hi"
                0x01, 0x63,                                // start 1, end 99
                0x02, 0x01,                                // warriors wanted/have
                0x01, 0x00,                                // wizards
                0x00, 0x00,                                // rogues
                0x01, 0x01,                                // priests
                0x00, 0x00);                               // monks
    }

    [Fact]
    public void WriteBody_RejectsTypeOutsideForm()
    {
        // RecruitInfo type on the single-name prompt form is invalid
        var act = () => new GroupPromptPacket
            { ResponseType = GroupResponseType.RecruitInfo, SourceName = "x" }.ToBody();

        act.Should().Throw<System.InvalidOperationException>();
    }

    // ---- type dispatch ------------------------------------------------------------------------

    [Fact]
    public void Parse_Type1_IsAskPrompt()
    {
        var parsed = GroupResponsePacket.Parse([0x01, 0x03, 0x42, 0x6F, 0x62])
            .Should().BeOfType<GroupPromptPacket>().Subject;

        parsed.ResponseType.Should().Be(GroupResponseType.Ask);
        parsed.SourceName.Should().Be("Bob");
    }

    [Fact]
    public void Parse_Type5_IsRecruitAskPrompt()
    {
        var parsed = GroupResponsePacket.Parse([0x05, 0x03, 0x42, 0x6F, 0x62])
            .Should().BeOfType<GroupPromptPacket>().Subject;

        parsed.ResponseType.Should().Be(GroupResponseType.RecruitAsk);
        parsed.SourceName.Should().Be("Bob");
    }

    [Fact]
    public void Parse_Type4_IsRecruitInfo()
    {
        var parsed = GroupResponsePacket.Parse(
                new GroupRecruitInfoPacket { ResponseType = GroupResponseType.RecruitInfo, Info = SampleRecruit() }
                    .ToBody())
            .Should().BeOfType<GroupRecruitInfoPacket>().Subject;

        parsed.Info.RecruiterName.Should().Be("Ann");
        parsed.Info.GroupName.Should().Be("Pals");
        parsed.Info.EndingLevel.Should().Be((byte)99);
        parsed.Info.PriestsWanted.Should().Be((byte)1);
    }

    [Fact]
    public void Parse_AskWithTrailingSlack_IgnoresIt()
    {
        // Two trailing NUL bytes after the Ask name are tolerated and ignored.
        var parsed = GroupResponsePacket.Parse([0x01, 0x03, 0x42, 0x6F, 0x62, 0x00, 0x00])
            .Should().BeOfType<GroupPromptPacket>().Subject;

        parsed.SourceName.Should().Be("Bob");
    }

    [Fact]
    public void Parse_UnknownType_Throws()
    {
        var act = () => GroupResponsePacket.Parse([0x02]);

        act.Should().Throw<InvalidDataException>();
    }

    // ---- round-trip through the codec ---------------------------------------------------------

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void RoundTrip_ThroughCodec_PreservesForm(GroupResponsePacket original)
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        parsed.Should().BeOfType(original.GetType());
        parsed.Should().BeEquivalentTo(original, opts => opts.RespectingRuntimeTypes());
    }

    public static TheoryData<GroupResponsePacket> RoundTripCases() =>
    [
        new GroupPromptPacket { ResponseType = GroupResponseType.Ask, SourceName = "Angelique" },
        new GroupPromptPacket { ResponseType = GroupResponseType.RecruitAsk, SourceName = "Comhaigne" },
        new GroupRecruitInfoPacket { ResponseType = GroupResponseType.RecruitInfo, Info = SampleRecruit() },
    ];
}
