using System.Collections.Generic;
using System.Linq;
using DALib.Enums;
using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x34 Profile (S->C) - another player's profile pane. Pins the wire layout,
///     including the social-status byte before the name, the fixed 18-slot equipment block, and the
///     derived portrait/text tail length; verifies the equipment-count guard; and round-trips through
///     the codec.
/// </summary>
public class ProfilePacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    // ---- layout pins --------------------------------------------------------------------------

    [Fact]
    public void WriteBody_PinsLayout()
    {
        var packet = new ProfilePacket
        {
            Id = 0x01020304,                // default equipment = 18 empty slots
            SocialStatus = SocialStatus.NeedGroup,
            Name = "Bob",
            NationFlag = 2,
            Title = "Sir",
            GroupOpen = true,
            GuildRank = "Lead",
            ClassName = "Wizard",
            GuildName = "Owls",
            Legend = [new LegendMark { Icon = 5, Color = 2, Prefix = "P", Text = "Hi" }],
            Portrait = [0xAA, 0xBB],
            ProfileText = "Hi"
        };

        var expected = new List<byte> { 0x01, 0x02, 0x03, 0x04 };       // Id (BE)
        expected.AddRange(Enumerable.Repeat((byte)0, 54));              // 18 x [u16 sprite][u8 color]
        expected.AddRange(new byte[]
        {
            0x03,                                                       // SocialStatus = NeedGroup
            0x03, 0x42, 0x6F, 0x62,                                     // Name "Bob"
            0x02,                                                       // NationFlag
            0x03, 0x53, 0x69, 0x72,                                     // Title "Sir"
            0x01,                                                       // GroupOpen
            0x04, 0x4C, 0x65, 0x61, 0x64,                               // GuildRank "Lead"
            0x06, 0x57, 0x69, 0x7A, 0x61, 0x72, 0x64,                   // ClassName "Wizard"
            0x04, 0x4F, 0x77, 0x6C, 0x73,                               // GuildName "Owls"
            0x01,                                                       // legend count
            0x05, 0x02, 0x01, 0x50, 0x02, 0x48, 0x69,                   // mark: icon,color,"P","Hi"
            0x00, 0x08,                                                 // remaining = 2 + 2 + 4
            0x00, 0x02,                                                 // portrait length
            0xAA, 0xBB,                                                 // portrait bytes
            0x00, 0x02, 0x48, 0x69                                      // ProfileText "Hi" (string16)
        });

        packet.ToBody().Should().Equal(expected);
    }

    [Fact]
    public void WriteBody_EquipmentSlot_IsSpriteBigEndianThenColor()
    {
        var packet = MinimalPacket();
        packet.Equipment[0] = packet.Equipment[0] with { Sprite = 0x1234, Color = 0x05 };

        packet.Equipment[0].Slot.Should().Be(EquipmentSlot.Weapon); // first display slot

        // first slot sits immediately after the 4-byte Id: [u16 sprite BE][u8 color]
        packet.ToBody().Skip(4).Take(3).Should().Equal(0x12, 0x34, 0x05);
    }

    [Fact]
    public void EquipmentDisplayOrder_PutsAccessory1BeforeBoots()
    {
        // the load-bearing swap: profile display order is NOT the 1-18 pane numbering - Accessory1
        // (slot 14) precedes Boots (slot 13).
        ProfilePacket.EquipmentDisplayOrder[12].Should().Be(EquipmentSlot.Accessory1);
        ProfilePacket.EquipmentDisplayOrder[13].Should().Be(EquipmentSlot.Boots);

        var packet = MinimalPacket();
        packet.Equipment[12].Slot.Should().Be(EquipmentSlot.Accessory1);
        packet.Equipment[13].Slot.Should().Be(EquipmentSlot.Boots);
    }

    [Fact]
    public void WriteBody_RejectsSlotsOutOfCanonicalOrder()
    {
        var packet = MinimalPacket();
        // swap two slots' identities - count is still 18 but the order is wrong
        (packet.Equipment[0], packet.Equipment[1]) = (packet.Equipment[1], packet.Equipment[0]);

        var act = () => packet.ToBody();

        act.Should().Throw<System.InvalidOperationException>();
    }

    [Fact]
    public void WriteBody_DerivesRemainingFromPortraitAndText()
    {
        var packet = MinimalPacket() with { Portrait = [0x01, 0x02, 0x03], ProfileText = "abcd" };

        // remaining = portraitLen(3) + textLen(4) + 4 = 11; tail = [00 0B][00 03][..][00 04 ..]
        var body = packet.ToBody().ToArray();
        var tail = body[^13..]; // [00 0B][00 03][01 02 03][00 04 61 62 63 64]

        tail.Take(2).Should().Equal(0x00, 0x0B);
        tail.Skip(2).Take(2).Should().Equal(0x00, 0x03);
    }

    [Fact]
    public void WriteBody_RejectsWrongEquipmentCount()
    {
        var act = () => (MinimalPacket() with
        {
            Equipment = [new ProfileEquipmentSlot(EquipmentSlot.Weapon, 1, 1)]
        }).ToBody();

        act.Should().Throw<System.InvalidOperationException>();
    }

    // ---- parse --------------------------------------------------------------------------------

    [Fact]
    public void Parse_ReadsAllFields()
    {
        var original = new ProfilePacket
        {
            Id = 99,
            SocialStatus = SocialStatus.Grouped,
            Name = "Aether",
            NationFlag = 3,
            Title = "the Brave",
            GroupOpen = false,
            GuildRank = "Founder",
            ClassName = "Master",
            GuildName = "Owls",
            Legend = [new LegendMark { Icon = 1, Color = 2, Prefix = "Deoch 5", Text = "Reached Master" }],
            Portrait = [0x10, 0x20, 0x30],
            ProfileText = "Hello there."
        };

        var parsed = ProfilePacket.Parse(original.ToBody());

        parsed.Id.Should().Be(99u);
        parsed.SocialStatus.Should().Be(SocialStatus.Grouped);
        parsed.Name.Should().Be("Aether");
        parsed.NationFlag.Should().Be((byte)3);
        parsed.Title.Should().Be("the Brave");
        parsed.GroupOpen.Should().BeFalse();
        parsed.GuildRank.Should().Be("Founder");
        parsed.ClassName.Should().Be("Master");
        parsed.GuildName.Should().Be("Owls");
        parsed.Equipment.Should().HaveCount(ProfilePacket.EquipmentSlotCount);
        parsed.Legend.Should().ContainSingle();
        parsed.Portrait.Should().Equal(0x10, 0x20, 0x30);
        parsed.ProfileText.Should().Be("Hello there.");
    }

    // ---- round-trip through the codec ---------------------------------------------------------

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesProfile()
    {
        var original = new ProfilePacket
        {
            Id = 0xDEADBEEF,
            Equipment = ProfilePacket.EquipmentDisplayOrder
                .Select((slot, i) => new ProfileEquipmentSlot(slot, (ushort)(0x100 + i), (byte)i))
                .ToList(),
            SocialStatus = SocialStatus.LoneHunter,
            Name = "Comhaigne",
            NationFlag = 1,
            Title = "Wanderer",
            GroupOpen = true,
            GuildRank = "Member",
            ClassName = "Rogue",
            GuildName = "Shadows",
            Legend =
            [
                new LegendMark { Icon = 1, Color = 1, Prefix = "Deoch 1", Text = "Born" },
                new LegendMark { Icon = 2, Color = 3, Prefix = "Deoch 9", Text = "Married in Mileth" }
            ],
            Portrait = [0xCA, 0xFE, 0xBA, 0xBE],
            ProfileText = "Ask me about trades."
        };

        var codec = new PacketCodec();
        var crypto = MakeCrypto();

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        parsed.Should().BeOfType<ProfilePacket>();
        parsed.Should().BeEquivalentTo(original);
    }

    // ---- helpers ------------------------------------------------------------------------------

    private static ProfilePacket MinimalPacket() => new()
    {
        Id = 1,
        Name = "X",
        ClassName = "Warrior",
    };
}
