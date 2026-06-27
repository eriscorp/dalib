using System.IO;
using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

public class NpcMenuPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    private static NpcMenuPacket Roundtrip(NpcMenuPacket original) => NpcMenuPacket.Parse(original.ToBody());

    [Fact]
    public void WriteBody_Prefix_MatchesGroundedLayout()
    {
        // MenuType is the first byte; then entity, source id, the two unknowns, both sprite/color
        // pairs, illustration index, name (string8), text (string16), then the body.
        var packet = new NpcMenuPacket
        {
            MenuType = NpcMenuType.TextEntry,
            EntityType = NpcMenuPacket.EntityTypeMerchant,
            SourceId = 0xDEADBEEF,
            Sprite = 0x0102,
            Color = 0x07,
            Sprite2 = 0x0304,
            Color2 = 0x09,
            IllustrationIndex = 0x00,
            Name = "Ann",
            Text = "Hi",
            Menu = new TextEntryMenu { PursuitId = 0x1234 },
        };

        packet.ToBody().Should().Equal(
            (byte)0x02,                                     // MenuType = TextEntry (2)
            (byte)0x01,                                     // EntityType = Merchant
            (byte)0xDE, (byte)0xAD, (byte)0xBE, (byte)0xEF, // SourceId
            (byte)0x00,                                     // Unknown1 (default 0)
            (byte)0x01, (byte)0x02,                         // Sprite
            (byte)0x07,                                     // Color
            (byte)0x01,                                     // Unknown2 (default 1)
            (byte)0x03, (byte)0x04,                         // Sprite2
            (byte)0x09,                                     // Color2
            (byte)0x00,                                     // IllustrationIndex
            (byte)0x03, (byte)0x41, (byte)0x6E, (byte)0x6E, // string8 "Ann"
            (byte)0x00, (byte)0x02, (byte)0x48, (byte)0x69, // string16 "Hi"
            (byte)0x12, (byte)0x34);                        // body: PursuitId
    }

    [Fact]
    public void RoundTrip_PreservesPrefixIncludingUnknowns()
    {
        var original = new NpcMenuPacket
        {
            MenuType = NpcMenuType.Options,
            EntityType = 0x01,
            SourceId = 0x01020304,
            Unknown1 = 0xAB,
            Sprite = 0x4500,
            Color = 0x05,
            Unknown2 = 0xCD,
            Sprite2 = 0x4600,
            Color2 = 0x06,
            IllustrationIndex = 0x02,
            Name = "Mileth Guard",
            Text = "Welcome, traveler.",
            Menu = new OptionsMenu { Options = [new NpcMenuOption("Leave", 0)] },
        };

        var parsed = Roundtrip(original);

        parsed.MenuType.Should().Be(NpcMenuType.Options);
        parsed.EntityType.Should().Be(0x01);
        parsed.SourceId.Should().Be(0x01020304);
        parsed.Unknown1.Should().Be(0xAB);
        parsed.Sprite.Should().Be(0x4500);
        parsed.Color.Should().Be(0x05);
        parsed.Unknown2.Should().Be(0xCD);
        parsed.Sprite2.Should().Be(0x4600);
        parsed.Color2.Should().Be(0x06);
        parsed.IllustrationIndex.Should().Be(0x02);
        parsed.Name.Should().Be("Mileth Guard");
        parsed.Text.Should().Be("Welcome, traveler.");
    }

    [Fact]
    public void RoundTrip_Options()
    {
        var original = new NpcMenuPacket
        {
            MenuType = NpcMenuType.Options,
            Name = "Smith",
            Text = "What do you need?",
            Menu = new OptionsMenu
            {
                Options =
                [
                    new NpcMenuOption("Buy", 0x0010),
                    new NpcMenuOption("Sell", 0x0011),
                    new NpcMenuOption("Repair", 0x0012),
                ],
            },
        };

        var parsed = Roundtrip(original);
        parsed.MenuType.Should().Be(NpcMenuType.Options);
        var menu = parsed.Menu.Should().BeOfType<OptionsMenu>().Subject;
        menu.Options.Should().HaveCount(3);
        menu.Options[0].Should().Be(new NpcMenuOption("Buy", 0x0010));
        menu.Options[2].Should().Be(new NpcMenuOption("Repair", 0x0012));
    }

    [Fact]
    public void RoundTrip_OptionsWithArgument()
    {
        var original = new NpcMenuPacket
        {
            MenuType = NpcMenuType.OptionsWithArgument,
            Menu = new OptionsWithArgumentMenu
            {
                Argument = "ceanntcoir",
                Options = [new NpcMenuOption("Yes", 1), new NpcMenuOption("No", 2)],
            },
        };

        var menu = Roundtrip(original).Menu.Should().BeOfType<OptionsWithArgumentMenu>().Subject;
        menu.Argument.Should().Be("ceanntcoir");
        menu.Options.Should().HaveCount(2);
        menu.Options[1].Pursuit.Should().Be(2);
    }

    [Fact]
    public void RoundTrip_TextEntry()
    {
        var original = new NpcMenuPacket { MenuType = NpcMenuType.TextEntry, Menu = new TextEntryMenu { PursuitId = 0xBEEF } };

        var menu = Roundtrip(original).Menu.Should().BeOfType<TextEntryMenu>().Subject;
        menu.PursuitId.Should().Be(0xBEEF);
    }

    [Fact]
    public void RoundTrip_TextEntryWithArgument()
    {
        var original = new NpcMenuPacket
        {
            MenuType = NpcMenuType.TextEntryWithArgument,
            Menu = new TextEntryWithArgumentMenu { Argument = "How many?", PursuitId = 0x00FF },
        };

        var menu = Roundtrip(original).Menu.Should().BeOfType<TextEntryWithArgumentMenu>().Subject;
        menu.Argument.Should().Be("How many?");
        menu.PursuitId.Should().Be(0x00FF);
    }

    [Fact]
    public void RoundTrip_ItemList()
    {
        var original = new NpcMenuPacket
        {
            MenuType = NpcMenuType.ItemList,
            Menu = new ItemListMenu
            {
                PursuitId = 0x0020,
                Items =
                [
                    new NpcMenuItem(0x8201, 3, 1500u, "Claw Fist", "A claw weapon."),
                    new NpcMenuItem(0x8202, 0, 50u, "Apple", string.Empty),
                ],
            },
        };

        var menu = Roundtrip(original).Menu.Should().BeOfType<ItemListMenu>().Subject;
        menu.PursuitId.Should().Be(0x0020);
        menu.Items.Should().HaveCount(2);
        menu.Items[0].Should().Be(new NpcMenuItem(0x8201, 3, 1500u, "Claw Fist", "A claw weapon."));
        menu.Items[1].Description.Should().BeEmpty();
    }

    [Fact]
    public void RoundTrip_ServerItemMenu_LeafAndBranchRows()
    {
        // The pursuit-0x4B rich layout: a leaf row (no sub-label) and a branch row (with one).
        var original = new NpcMenuPacket
        {
            MenuType = NpcMenuType.ItemList,
            Name = "Storage",
            Menu = new ServerItemMenu
            {
                Items =
                [
                    new NpcServerItem
                    {
                        Handle = 0x11223344, Sprite = 0x8210, Color = 2, Cost = 999u,
                        Available = true, Name = "Sword", LeafBranch = 0,
                        StockRemaining = 0xAABBCCDD, StockMax = 0x01020304,
                    },
                    new NpcServerItem
                    {
                        Handle = 0x55667788, Sprite = 0, Color = 0, Cost = 0u,
                        Available = false, Name = "Weapons", LeafBranch = 1, SubLabel = "Blades",
                        StockRemaining = 0, StockMax = 0,
                    },
                ],
            },
        };

        var parsed = Roundtrip(original);
        parsed.MenuType.Should().Be(NpcMenuType.ItemList);
        var menu = parsed.Menu.Should().BeOfType<ServerItemMenu>().Subject;
        menu.Items.Should().HaveCount(2);

        var leaf = menu.Items[0];
        leaf.Handle.Should().Be(0x11223344);
        leaf.Cost.Should().Be(999u);
        leaf.Available.Should().BeTrue();
        leaf.LeafBranch.Should().Be(0);
        leaf.SubLabel.Should().BeEmpty();
        leaf.StockRemaining.Should().Be(0xAABBCCDD);
        leaf.StockMax.Should().Be(0x01020304);

        var branch = menu.Items[1];
        branch.Handle.Should().Be(0x55667788);
        branch.Name.Should().Be("Weapons");
        branch.Available.Should().BeFalse();
        branch.LeafBranch.Should().Be(1);
        branch.SubLabel.Should().Be("Blades");
    }

    [Fact]
    public void ServerItemMenu_WritesFixed0x4BPursuit()
    {
        // The body's first two bytes are the fixed 0x004B pursuit selector.
        var body = new NpcMenuPacket
        {
            MenuType = NpcMenuType.ItemList,
            Menu = new ServerItemMenu { Items = [] },
        }.ToBody();

        // body[..18] is the prefix (name/text empty); body[18..20] is the pursuit.
        body[18].Should().Be(0x00);
        body[19].Should().Be(0x4B);
    }

    [Fact]
    public void RoundTrip_ServerItemMenu_UnderAlternateType10()
    {
        // Type 10 routes to the same server-item menu parser; pursuit 0x4B still selects rich rows.
        var original = new NpcMenuPacket
        {
            MenuType = NpcMenuType.ItemListAlternate,
            Menu = new ServerItemMenu { Items = [new NpcServerItem { Handle = 1, Name = "Root" }] },
        };

        var parsed = Roundtrip(original);
        parsed.MenuType.Should().Be(NpcMenuType.ItemListAlternate);
        parsed.Menu.Should().BeOfType<ServerItemMenu>().Which.Items.Should().ContainSingle();
    }

    [Fact]
    public void RoundTrip_ItemListAlternate_PreservesType10()
    {
        // The crux of the explicit-MenuType design: a type-10 flat list round-trips as 10, not 4.
        var original = new NpcMenuPacket
        {
            MenuType = NpcMenuType.ItemListAlternate,
            Menu = new ItemListMenu { PursuitId = 0x0021, Items = [new NpcMenuItem(0x8200, 0, 1u, "Thing", "")] },
        };

        var parsed = Roundtrip(original);
        parsed.MenuType.Should().Be(NpcMenuType.ItemListAlternate);
        parsed.Menu.Should().BeOfType<ItemListMenu>().Which.PursuitId.Should().Be(0x0021);
    }

    [Fact]
    public void RoundTrip_PlayerItemList()
    {
        var original = new NpcMenuPacket
        {
            MenuType = NpcMenuType.PlayerItemList,
            Menu = new PlayerItemListMenu { PursuitId = 0x0030, Slots = [1, 5, 17, 42] },
        };

        var menu = Roundtrip(original).Menu.Should().BeOfType<PlayerItemListMenu>().Subject;
        menu.PursuitId.Should().Be(0x0030);
        menu.Slots.Should().Equal((byte)1, (byte)5, (byte)17, (byte)42);
    }

    [Fact]
    public void RoundTrip_PlayerItemListAlternate_PreservesType11()
    {
        var original = new NpcMenuPacket
        {
            MenuType = NpcMenuType.PlayerItemListAlternate,
            Menu = new PlayerItemListMenu { PursuitId = 0x0031, Slots = [9] },
        };

        var parsed = Roundtrip(original);
        parsed.MenuType.Should().Be(NpcMenuType.PlayerItemListAlternate);
        parsed.Menu.Should().BeOfType<PlayerItemListMenu>().Which.Slots.Should().Equal((byte)9);
    }

    [Fact]
    public void RoundTrip_PlayerItemHandleMenu()
    {
        // The pursuit-0x4E twin of ServerItemMenu: each row pairs a slot with a server handle.
        var original = new NpcMenuPacket
        {
            MenuType = NpcMenuType.PlayerItemList,
            Menu = new PlayerItemHandleMenu
            {
                Items =
                [
                    new NpcPlayerItemHandle(3, 0xAABBCCDD),
                    new NpcPlayerItemHandle(17, 0x01020304),
                ],
            },
        };

        var menu = Roundtrip(original).Menu.Should().BeOfType<PlayerItemHandleMenu>().Subject;
        menu.Items.Should().HaveCount(2);
        menu.Items[0].Should().Be(new NpcPlayerItemHandle(3, 0xAABBCCDD));
        menu.Items[1].Should().Be(new NpcPlayerItemHandle(17, 0x01020304));
    }

    [Fact]
    public void PlayerItemHandleMenu_WritesFixed0x4EPursuit()
    {
        // body[..18] is the prefix (name/text empty); body[18..20] is the fixed 0x004E pursuit.
        var body = new NpcMenuPacket
        {
            MenuType = NpcMenuType.PlayerItemList,
            Menu = new PlayerItemHandleMenu { Items = [] },
        }.ToBody();

        body[18].Should().Be(0x00);
        body[19].Should().Be(0x4E);
    }

    [Fact]
    public void PlayerItemListMenu_ReservedPursuit_Throws()
    {
        // Pursuit 0x4E on a bare PlayerItemListMenu would be misparsed as handle rows.
        var packet = new NpcMenuPacket
        {
            MenuType = NpcMenuType.PlayerItemList,
            Menu = new PlayerItemListMenu { PursuitId = PlayerItemHandleMenu.HandlePursuit, Slots = [] },
        };

        var act = () => packet.ToBody();
        act.Should().Throw<System.InvalidOperationException>();
    }

    [Fact]
    public void RoundTrip_SpellList()
    {
        var original = new NpcMenuPacket
        {
            MenuType = NpcMenuType.SpellList,
            Menu = new SpellListMenu
            {
                PursuitId = 0x0040,
                Spells = [new NpcMenuCastable(2, 0x0101, 0, "beag srad"), new NpcMenuCastable(2, 0x0102, 0, "srad")],
            },
        };

        var parsed = Roundtrip(original);
        parsed.MenuType.Should().Be(NpcMenuType.SpellList);
        var menu = parsed.Menu.Should().BeOfType<SpellListMenu>().Subject;
        menu.PursuitId.Should().Be(0x0040);
        menu.Spells.Should().HaveCount(2);
        menu.Spells[0].Should().Be(new NpcMenuCastable(2, 0x0101, 0, "beag srad"));
    }

    [Fact]
    public void RoundTrip_SkillList()
    {
        var original = new NpcMenuPacket
        {
            MenuType = NpcMenuType.SkillList,
            Menu = new SkillListMenu
            {
                PursuitId = 0x0050,
                Skills = [new NpcMenuCastable(3, 0x0201, 0, "assail")],
            },
        };

        var menu = Roundtrip(original).Menu.Should().BeOfType<SkillListMenu>().Subject;
        menu.PursuitId.Should().Be(0x0050);
        menu.Skills.Should().ContainSingle().Which.Should().Be(new NpcMenuCastable(3, 0x0201, 0, "assail"));
    }

    [Fact]
    public void RoundTrip_PlayerSpellList()
    {
        var original = new NpcMenuPacket { MenuType = NpcMenuType.PlayerSpellList, Menu = new PlayerSpellListMenu { PursuitId = 0x0060 } };

        Roundtrip(original).Menu.Should().BeOfType<PlayerSpellListMenu>().Which.PursuitId.Should().Be(0x0060);
    }

    [Fact]
    public void RoundTrip_PlayerSkillList()
    {
        var original = new NpcMenuPacket { MenuType = NpcMenuType.PlayerSkillList, Menu = new PlayerSkillListMenu { PursuitId = 0x0070 } };

        Roundtrip(original).Menu.Should().BeOfType<PlayerSkillListMenu>().Which.PursuitId.Should().Be(0x0070);
    }

    [Fact]
    public void WriteBody_IncompatibleBodyForMenuType_Throws()
    {
        var packet = new NpcMenuPacket { MenuType = NpcMenuType.Options, Menu = new TextEntryMenu { PursuitId = 1 } };

        var act = () => packet.ToBody();
        act.Should().Throw<System.InvalidOperationException>();
    }

    [Fact]
    public void ItemListMenu_ReservedPursuit_Throws()
    {
        // Pursuit 0x4B on a flat ItemListMenu would be misparsed as rich rows.
        var packet = new NpcMenuPacket
        {
            MenuType = NpcMenuType.ItemList,
            Menu = new ItemListMenu { PursuitId = ServerItemMenu.ServerItemPursuit, Items = [] },
        };

        var act = () => packet.ToBody();
        act.Should().Throw<System.InvalidOperationException>();
    }

    [Fact]
    public void Parse_TrailingBytes_Throws()
    {
        // A TextEntry body is exactly 2 bytes; append a stray byte and parsing must reject it.
        var body = new NpcMenuPacket { MenuType = NpcMenuType.TextEntry, Menu = new TextEntryMenu { PursuitId = 1 } }.ToBody();
        var corrupted = new byte[body.Length + 1];
        body.CopyTo(corrupted, 0);

        var act = () => NpcMenuPacket.Parse(corrupted);
        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void RoundTrip_ThroughCodec_PreservesMenu()
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();
        var original = new NpcMenuPacket
        {
            MenuType = NpcMenuType.Options,
            Name = "Wing Merchant",
            Text = "Care to trade?",
            Menu = new OptionsMenu { Options = [new NpcMenuOption("Buy", 0xFF01), new NpcMenuOption("Sell", 0xFF02)] },
        };

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        var typed = parsed.Should().BeOfType<NpcMenuPacket>().Subject;
        typed.Name.Should().Be("Wing Merchant");
        typed.Menu.Should().BeOfType<OptionsMenu>().Which.Options.Should().HaveCount(2);
    }
}
