using System;
using System.IO;
using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x2E GroupRequest (C->S) - pins the simple form (stages 2/3/6/7) and the Groupbox
///     form (stage 4), round-trips every stage, and rejects unknown stages.
/// </summary>
public class GroupRequestPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    [Fact]
    public void WriteBody_TryInvite_PinsSimpleForm()
    {
        // Wire bytes: 2E 02 07 "Trocair" 00 (then crypto padding).
        var packet = GroupRequestPacket.TryInvite("Trocair");

        packet.ToBody().Should().Equal(
            (byte)0x02,
            (byte)0x07, (byte)'T', (byte)'r', (byte)'o', (byte)'c', (byte)'a', (byte)'i', (byte)'r',
            (byte)0x00);
    }

    [Fact]
    public void WriteBody_RemoveGroupBox_PinsSimpleForm()
    {
        // Wire bytes: 2E 06 06 "Kedian" 00 (then crypto padding).
        var packet = GroupRequestPacket.RemoveGroupBox("Kedian");

        packet.ToBody().Should().Equal(
            (byte)0x06,
            (byte)0x06, (byte)'K', (byte)'e', (byte)'d', (byte)'i', (byte)'a', (byte)'n',
            (byte)0x00);
    }

    [Fact]
    public void WriteBody_Groupbox_PinsCaptureBytes()
    {
        // Distinct caps Warrior 1 / Wizard 2 / Monk 3 / Priest 4 / Rogue 5 appear on the wire as
        // 01 02 03 04 05, proving the order W, Wiz, Monk, Priest, Rogue.
        // 04 06"Kedian" 04"AAAA" 04"BBBB" 0D 25 01 02 03 04 05 (no trailing byte).
        var packet = GroupRequestPacket.Groupbox(
            leader: "Kedian",
            title: "AAAA",
            note: "BBBB",
            minLevel: 0x0D,
            maxLevel: 0x25,
            maxWarrior: 1,
            maxWizard: 2,
            maxRogue: 5,
            maxPriest: 4,
            maxMonk: 3);

        packet.ToBody().Should().Equal(
            (byte)0x04,
            (byte)0x06, (byte)'K', (byte)'e', (byte)'d', (byte)'i', (byte)'a', (byte)'n',
            (byte)0x04, (byte)'A', (byte)'A', (byte)'A', (byte)'A',
            (byte)0x04, (byte)'B', (byte)'B', (byte)'B', (byte)'B',
            (byte)0x0D, (byte)0x25,
            (byte)0x01, (byte)0x02, (byte)0x03, (byte)0x04, (byte)0x05);
    }

    [Fact]
    public void Parse_Groupbox_FromCaptureBytes_DecodesAllFields()
    {
        // The decoded body (codec has already stripped the [00][opcode] padding).
        // Caps on the wire are 01 02 03 04 05 -> Warrior 1, Wizard 2, Monk 3, Priest 4, Rogue 5.
        byte[] body =
        [
            0x04,
            0x06, (byte)'K', (byte)'e', (byte)'d', (byte)'i', (byte)'a', (byte)'n',
            0x04, (byte)'A', (byte)'A', (byte)'A', (byte)'A',
            0x04, (byte)'B', (byte)'B', (byte)'B', (byte)'B',
            0x0D, 0x25,
            0x01, 0x02, 0x03, 0x04, 0x05,
        ];

        var packet = GroupRequestPacket.Parse(body);

        packet.Stage.Should().Be(GroupRequestPacket.StageGroupbox);
        packet.Leader.Should().Be("Kedian");
        packet.Title.Should().Be("AAAA");
        packet.Note.Should().Be("BBBB");
        packet.MinLevel.Should().Be((byte)0x0D);
        packet.MaxLevel.Should().Be((byte)0x25);
        packet.MaxWarrior.Should().Be((byte)0x01);
        packet.MaxWizard.Should().Be((byte)0x02);
        packet.MaxMonk.Should().Be((byte)0x03);
        packet.MaxPriest.Should().Be((byte)0x04);
        packet.MaxRogue.Should().Be((byte)0x05);
    }

    [Theory]
    [InlineData(GroupRequestPacket.StageTryInvite)]
    [InlineData(GroupRequestPacket.StageAcceptInvite)]
    [InlineData(GroupRequestPacket.StageRemoveGroupBox)]
    [InlineData(GroupRequestPacket.StageRecruitJoin)]
    public void RoundTrip_SimpleStages_PreserveName(byte stage)
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new GroupRequestPacket { Stage = stage, Name = "Trocair" };

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<GroupRequestPacket>().Subject;
        typed.Stage.Should().Be(stage);
        typed.Name.Should().Be("Trocair");
        typed.Leader.Should().BeNull();
    }

    [Fact]
    public void RoundTrip_Groupbox_PreservesAllFields()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = GroupRequestPacket.Groupbox(
            "Kedian", "AAAA", "BBBB", 0x0D, 0x25,
            maxWarrior: 1, maxWizard: 2, maxRogue: 5, maxPriest: 4, maxMonk: 3);

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<GroupRequestPacket>().Subject;
        typed.Stage.Should().Be(GroupRequestPacket.StageGroupbox);
        typed.Leader.Should().Be("Kedian");
        typed.Title.Should().Be("AAAA");
        typed.Note.Should().Be("BBBB");
        typed.MinLevel.Should().Be((byte)0x0D);
        typed.MaxLevel.Should().Be((byte)0x25);
        typed.MaxWarrior.Should().Be((byte)1);
        typed.MaxWizard.Should().Be((byte)2);
        typed.MaxMonk.Should().Be((byte)3);
        typed.MaxPriest.Should().Be((byte)4);
        typed.MaxRogue.Should().Be((byte)5);
        typed.Name.Should().BeNull();
    }

    [Theory]
    [InlineData((byte)0x01)] // not a recognized stage.
    [InlineData((byte)0x05)] // gap in the enum - does not exist.
    [InlineData((byte)0x00)]
    public void Parse_UnknownStage_Throws(byte stage)
    {
        var act = () => GroupRequestPacket.Parse([stage, 0x00]);

        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void WriteBody_SimpleStageWithNullName_Throws()
    {
        var packet = new GroupRequestPacket { Stage = GroupRequestPacket.StageTryInvite, Name = null };

        var act = () => packet.ToBody();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void WriteBody_GroupboxWithNullField_Throws()
    {
        var packet = new GroupRequestPacket
        {
            Stage = GroupRequestPacket.StageGroupbox,
            Leader = "Kedian",
            Title = "AAAA",
            Note = null, // missing
            MinLevel = 0, MaxLevel = 0,
            MaxWarrior = 0, MaxWizard = 0, MaxRogue = 0, MaxPriest = 0, MaxMonk = 0,
        };

        var act = () => packet.ToBody();

        act.Should().Throw<InvalidOperationException>();
    }
}
