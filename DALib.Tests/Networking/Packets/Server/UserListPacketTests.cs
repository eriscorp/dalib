using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x36 UserList (S->C) - the online-user list. Pins the wire layout (the two u16
///     counts: all-shards total then local rows, mirrored when TotalUserCount is unset); verifies
///     accumulation and round-trips through the codec.
/// </summary>
public class UserListPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    // ---- layout pins --------------------------------------------------------------------------

    [Fact]
    public void WriteBody_PinsLayout_WithDuplicatedCount()
    {
        new UserListPacket
            {
                Users =
                [
                    new UserListEntry(
                        Class: 1, Color: 84, SocialStatus: SocialStatus.Grouped,
                        Title: "Sir", IsMaster: true, Name: "Bob")
                ]
            }
            .ToBody().Should().Equal(
                0x00, 0x01, 0x00, 0x01,                    // count, count (both = 1)
                0x01,                                      // Class
                0x54,                                      // Color (84 = guild-mate)
                0x04,                                      // SocialStatus = Grouped
                0x03, 0x53, 0x69, 0x72,                    // Title "Sir"
                0x01,                                      // IsMaster
                0x03, 0x42, 0x6F, 0x62);                   // Name "Bob"
    }

    [Fact]
    public void WriteBody_EmptyList_IsJustDoubledZeroCount()
    {
        new UserListPacket().ToBody().Should().Equal(0x00, 0x00, 0x00, 0x00);
    }

    [Fact]
    public void WriteBody_TotalUserCount_WritesDistinctFirstCount()
    {
        new UserListPacket
            {
                TotalUserCount = 0x0203,
                Users =
                [
                    new UserListEntry(
                        Class: 1, Color: 84, SocialStatus: SocialStatus.Grouped,
                        Title: "", IsMaster: false, Name: "Bob")
                ]
            }
            .ToBody().Take(4).Should().Equal(0x02, 0x03, 0x00, 0x01); // all-shards total, local count
    }

    [Fact]
    public void WriteBody_TotalUserCountBelowLocalCount_Throws()
    {
        var packet = new UserListPacket
        {
            TotalUserCount = 0,
            Users =
            [
                new UserListEntry(
                    Class: 1, Color: 84, SocialStatus: SocialStatus.Grouped,
                    Title: "", IsMaster: false, Name: "Bob")
            ]
        };

        packet.Invoking(p => p.ToBody()).Should().Throw<InvalidOperationException>();
    }

    // ---- parse --------------------------------------------------------------------------------

    [Fact]
    public void Parse_ReadsEntries()
    {
        var parsed = UserListPacket.Parse(
        [
            0x0F, 0xA0, 0x00, 0x02, // 4000 online across all shards, 2 local rows
            0x01, 0x54, 0x04, 0x03, 0x53, 0x69, 0x72, 0x01, 0x03, 0x42, 0x6F, 0x62, // Bob, master
            0x02, 0xFF, 0x00, 0x00, 0x00, 0x03, 0x41, 0x6E, 0x6E                     // Ann, no title, not master
        ]);

        parsed.TotalUserCount.Should().Be(4000);
        parsed.Users.Should().HaveCount(2);
        parsed.Users[0].Should().Be(new UserListEntry(1, 0x54, SocialStatus.Grouped, "Sir", true, "Bob"));
        parsed.Users[1].Should().Be(new UserListEntry(2, 0xFF, SocialStatus.Awake, "", false, "Ann"));
    }

    [Fact]
    public void Users_AreAccumulable_AfterConstruction()
    {
        var packet = new UserListPacket();

        foreach (var (entry, visible) in new[]
                 {
                     (new UserListEntry(1, 0x54, SocialStatus.Grouped, "", true, "Bob"), true),
                     (new UserListEntry(2, 0xFF, SocialStatus.Awake, "", false, "Hidden"), false),
                     (new UserListEntry(3, 0x97, SocialStatus.NeedGroup, "", false, "Ann"), true),
                 })
            if (visible)
                packet.Users.Add(entry);

        packet.Users.Should().HaveCount(2);
        packet.ToBody().Take(4).Should().Equal(0x00, 0x02, 0x00, 0x02);
    }

    // ---- round-trip through the codec ---------------------------------------------------------

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesUsers()
    {
        var original = new UserListPacket
        {
            TotalUserCount = 1234,
            Users =
            [
                new UserListEntry(1, 0x54, SocialStatus.GroupHunting, "the Bold", true, "Angelique"),
                new UserListEntry(4, 0x97, SocialStatus.NeedHelp, "", false, "Comhaigne"),
                new UserListEntry(2, 0xFF, SocialStatus.LoneHunter, "Wanderer", false, "Aether")
            ]
        };

        var codec = new PacketCodec();
        var crypto = MakeCrypto();

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        parsed.Should().BeOfType<UserListPacket>();
        parsed.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void RoundTrip_NullTotalUserCount_ParsesAsMirroredLocalCount()
    {
        var original = new UserListPacket
        {
            Users =
            [
                new UserListEntry(1, 0x54, SocialStatus.Grouped, "", true, "Bob"),
                new UserListEntry(2, 0xFF, SocialStatus.Awake, "", false, "Ann")
            ]
        };

        var parsed = UserListPacket.Parse(original.ToBody());

        parsed.TotalUserCount.Should().Be(2);
        parsed.Users.Should().Equal(original.Users);
    }
}
