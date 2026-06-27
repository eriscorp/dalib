using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

public class SelfProfilePacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_EmptyPacket_AllFieldsDefault()
    {
        // Minimal packet: every field at default. Wire layout:
        //   [NationFlag 0][GuildRank len=0][CurrentTitle len=0][GroupStatusText len=0]
        //   [CanGroup 0][HasRecruit 0][Class 0][Unknown1 0][Unknown2 0]
        //   [ClassName len=0][GuildName len=0][LegendCount 0]
        var packet = new SelfProfilePacket();

        packet.ToBody().Should().Equal(
            (byte)0x00,
            (byte)0x00,
            (byte)0x00,
            (byte)0x00,
            (byte)0x00,
            (byte)0x00,
            (byte)0x00,
            (byte)0x00,
            (byte)0x00,
            (byte)0x00,
            (byte)0x00,
            (byte)0x00);
    }

    [Fact]
    public void WriteBody_LegendCountAutoDerived()
    {
        var packet = new SelfProfilePacket
        {
            Legend = new[]
            {
                new LegendMark { Icon = 1, Color = 2, Prefix = "A", Text = "B" },
                new LegendMark { Icon = 3, Color = 4, Prefix = "C", Text = "D" },
            },
        };

        var body = packet.ToBody();
        // After the 11 leading default bytes, the LegendCount byte is at index 11.
        body[11].Should().Be(0x02);
    }

    [Fact]
    public void WriteBody_HasRecruitFlagAutoDerived_NullRecruit()
    {
        var packet = new SelfProfilePacket();
        var body = packet.ToBody();
        // After NationFlag + 3 empty string8s + CanGroup byte, HasRecruit is at index 5.
        body[5].Should().Be(0x00);
    }

    [Fact]
    public void WriteBody_HasRecruitFlagAutoDerived_NonNullRecruit()
    {
        var packet = new SelfProfilePacket
        {
            Recruit = new GroupRecruitInfo(),
        };
        var body = packet.ToBody();
        body[5].Should().Be(0x01);
    }

    [Fact]
    public void WriteBody_LegendOverflow_Throws()
    {
        var packet = new SelfProfilePacket
        {
            Legend = new LegendMark[SelfProfilePacket.MaxLegendMarks + 1],
        };

        // Populate with default instances so we don't trip on null refs.
        var marks = new LegendMark[SelfProfilePacket.MaxLegendMarks + 1];
        for (var i = 0; i < marks.Length; i++)
            marks[i] = new LegendMark();
        packet.Legend = marks;

        Action act = () => packet.ToBody();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*legend mark count*");
    }

    [Fact]
    public void RoundTrip_FullyPopulated_PreservesAllFields()
    {
        var original = new SelfProfilePacket
        {
            NationFlag = 3,
            GuildRank = "Acolyte",
            CurrentTitle = "The Brave",
            GroupStatusText = "Adventuring Alone",
            CanGroup = true,
            Recruit = new GroupRecruitInfo
            {
                RecruiterName = "Founder",
                GroupName = "The Group",
                Note = "Looking for fighters",
                StartingLevel = 10,
                EndingLevel = 50,
                WarriorsWanted = 2, CurrentWarriors = 1,
                WizardsWanted = 1, CurrentWizards = 0,
                RoguesWanted = 1, CurrentRogues = 0,
                PriestsWanted = 1, CurrentPriests = 1,
                MonksWanted = 0, CurrentMonks = 0,
            },
            Class = 4,
            Unknown1 = 0,
            Unknown2 = 0,
            ClassName = "Warrior",
            GuildName = "Aisling Knights",
            Legend = new[]
            {
                new LegendMark { Icon = 5, Color = 6, Prefix = "Deoch 1", Text = "Born on a Tuesday" },
                new LegendMark { Icon = 7, Color = 8, Prefix = "Deoch 5", Text = "Slew a great evil" },
            },
        };

        var parsed = SelfProfilePacket.Parse(original.ToBody());

        parsed.NationFlag.Should().Be(3);
        parsed.GuildRank.Should().Be("Acolyte");
        parsed.CurrentTitle.Should().Be("The Brave");
        parsed.GroupStatusText.Should().Be("Adventuring Alone");
        parsed.CanGroup.Should().BeTrue();

        parsed.Recruit.Should().NotBeNull();
        parsed.Recruit!.RecruiterName.Should().Be("Founder");
        parsed.Recruit.GroupName.Should().Be("The Group");
        parsed.Recruit.Note.Should().Be("Looking for fighters");
        parsed.Recruit.StartingLevel.Should().Be(10);
        parsed.Recruit.EndingLevel.Should().Be(50);
        parsed.Recruit.WarriorsWanted.Should().Be(2);
        parsed.Recruit.CurrentPriests.Should().Be(1);

        parsed.Class.Should().Be(4);
        parsed.ClassName.Should().Be("Warrior");
        parsed.GuildName.Should().Be("Aisling Knights");

        parsed.Legend.Should().HaveCount(2);
        parsed.Legend[0].Icon.Should().Be(5);
        parsed.Legend[0].Prefix.Should().Be("Deoch 1");
        parsed.Legend[1].Text.Should().Be("Slew a great evil");
    }

    [Fact]
    public void RoundTrip_NoRecruit_PreservesNull()
    {
        var original = new SelfProfilePacket
        {
            GuildName = "No Guild",
            Recruit = null,
        };

        var parsed = SelfProfilePacket.Parse(original.ToBody());

        parsed.Recruit.Should().BeNull();
        parsed.GuildName.Should().Be("No Guild");
    }

    [Fact]
    public void Parse_TrailingBytesSilentlyIgnored()
    {
        // Build a normal packet, then append a historical 9-byte slack tail:
        //   0x00, u16 PlayerDisplay, 0x02, u32 0x00, 0x00
        var clean = new SelfProfilePacket
        {
            ClassName = "Warrior",
            GuildName = "Guild",
        };

        var cleanBytes = clean.ToBody();
        var withSlack = new byte[cleanBytes.Length + 9];
        cleanBytes.CopyTo(withSlack, 0);
        // Slack bytes: anything goes; parser ignores them.
        withSlack[cleanBytes.Length + 0] = 0x00;
        withSlack[cleanBytes.Length + 1] = 0x12;
        withSlack[cleanBytes.Length + 2] = 0x34;
        withSlack[cleanBytes.Length + 3] = 0x02;
        // Remaining bytes default to 0.

        var parsed = SelfProfilePacket.Parse(withSlack);

        parsed.ClassName.Should().Be("Warrior");
        parsed.GuildName.Should().Be("Guild");
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new SelfProfilePacket
        {
            NationFlag = 2,
            GuildRank = "Knight",
            CurrentTitle = "Hero",
            GroupStatusText = SelfProfilePacket.GroupStatusSolo,
            CanGroup = true,
            Class = 3,
            ClassName = "Wizard",
            GuildName = "Guild",
            Legend = [new LegendMark { Icon = 1, Color = 2, Prefix = "Pref", Text = "Txt" }],
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<SelfProfilePacket>().Subject;
        typed.GuildRank.Should().Be("Knight");
        typed.GroupStatusText.Should().Be(SelfProfilePacket.GroupStatusSolo);
        typed.Legend.Should().HaveCount(1);
        typed.Legend[0].Prefix.Should().Be("Pref");
    }

    [Fact]
    public void FormatGroupRoster_MatchesRetailExpectedShape()
    {
        var rendered = SelfProfilePacket.FormatGroupRoster(
            founderName: "Aoife",
            memberNames: ["Aoife", "Brigid", "Cormac"]);

        rendered.Should().Be(
            "Group members\n* Aoife\n  Brigid\n  Cormac\nTotal 3");
    }

    [Fact]
    public void FormatGroupRoster_FounderOnly()
    {
        var rendered = SelfProfilePacket.FormatGroupRoster(
            founderName: "Solo",
            memberNames: ["Solo"]);

        rendered.Should().Be("Group members\n* Solo\nTotal 1");
    }

    [Fact]
    public void BuildUpStyle_ConstructEmptyThenSetFields()
    {
        // Pin the build-up shape: empty profile, then mutate fields as data resolves.
        var packet = new SelfProfilePacket();
        packet.NationFlag = 1;
        packet.GuildRank = "Apprentice";
        packet.Class = 2;

        var parsed = SelfProfilePacket.Parse(packet.ToBody());
        parsed.NationFlag.Should().Be(1);
        parsed.GuildRank.Should().Be("Apprentice");
        parsed.Class.Should().Be(2);
        parsed.Recruit.Should().BeNull();
        parsed.Legend.Should().BeEmpty();
    }
}
