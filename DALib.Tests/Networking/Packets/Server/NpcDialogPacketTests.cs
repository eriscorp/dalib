using System;
using DALib.Networking.Packets.Server;

namespace DALib.Tests.Networking.Packets.Server;

public class NpcDialogPacketTests
{
    private static NpcDialogPacket Roundtrip(NpcDialogPacket original) => NpcDialogPacket.Parse(original.ToBody());

    [Fact]
    public void WriteBody_Prefix_MatchesGroundedLayout()
    {
        // DialogType first; then objType, source id, the inert unknown, sprite/color, the inert
        // unknown+sprite2+color2, pursuit id, dialog id, the two nav flags, the trailing byte, name
        // (string8), the text prompt (string16, present for Options), then the body.
        var packet = new NpcDialogPacket
        {
            DialogType = NpcDialogType.Options,
            ObjectType = NpcDialogPacket.ObjectTypeCreature,
            SourceId = 0xDEADBEEF,
            Sprite = 0x0102,
            Color = 0x07,
            Sprite2 = 0x0304,
            Color2 = 0x09,
            PursuitId = 0x1234,
            DialogId = 0x0005,
            HasPreviousButton = true,
            HasNextButton = false,
            Name = "Ann",
            Text = "Hi",
            Body = new OptionsDialog { Options = ["Yes", "No"] },
        };

        packet.ToBody().Should().Equal(
            (byte)0x02,                                     // DialogType = Options (2)
            (byte)0x01,                                     // ObjectType = Creature
            (byte)0xDE, (byte)0xAD, (byte)0xBE, (byte)0xEF, // SourceId (BE)
            (byte)0x00,                                     // Unknown1 (inert)
            (byte)0x01, (byte)0x02,                         // Sprite (BE)
            (byte)0x07,                                     // Color
            (byte)0x00,                                     // Unknown2 (inert)
            (byte)0x03, (byte)0x04,                         // Sprite2 (inert)
            (byte)0x09,                                     // Color2 (inert)
            (byte)0x12, (byte)0x34,                         // PursuitId (BE)
            (byte)0x00, (byte)0x05,                         // DialogId (BE)
            (byte)0x01,                                     // HasPreviousButton
            (byte)0x00,                                     // HasNextButton
            (byte)0x00,                                     // Unknown3 (trailing)
            (byte)0x03, (byte)0x41, (byte)0x6E, (byte)0x6E, // Name "Ann" (string8)
            (byte)0x00, (byte)0x02, (byte)0x48, (byte)0x69, // Text "Hi" (string16)
            (byte)0x02,                                     // option count
            (byte)0x03, (byte)0x59, (byte)0x65, (byte)0x73, // "Yes"
            (byte)0x02, (byte)0x4E, (byte)0x6F);            // "No"
    }

    [Fact]
    public void RoundTrip_Normal_TextNoBody()
    {
        var packet = new NpcDialogPacket
        {
            DialogType = NpcDialogType.Normal,
            SourceId = 1,
            Name = "Sign",
            Text = "Welcome to Mileth.",
            Body = new TextDialog(),
        };

        var parsed = Roundtrip(packet);
        parsed.DialogType.Should().Be(NpcDialogType.Normal);
        parsed.Text.Should().Be("Welcome to Mileth.");
        parsed.Body.Should().BeOfType<TextDialog>();
    }

    [Fact]
    public void RoundTrip_Normal_EmptyText()
    {
        // Regression pin: an empty prompt writes a zero-length string16 (the length prefix is
        // always present) and round-trips to empty, never desyncing the field order.
        var packet = new NpcDialogPacket
        {
            DialogType = NpcDialogType.Normal,
            SourceId = 1,
            Name = "Sign",
            Text = "",
            Body = new TextDialog(),
        };

        var parsed = Roundtrip(packet);
        parsed.Text.Should().BeEmpty();
        parsed.Name.Should().Be("Sign");
        parsed.Body.Should().BeOfType<TextDialog>();
    }

    [Fact]
    public void RoundTrip_Options_CarriesText()
    {
        var packet = new NpcDialogPacket
        {
            DialogType = NpcDialogType.Options,
            Name = "Guard",
            Text = "How can I help?",
            HasNextButton = true,
            Body = new OptionsDialog { Options = ["Buy", "Sell", "Leave"] },
        };

        var parsed = Roundtrip(packet);
        parsed.Text.Should().Be("How can I help?");
        parsed.HasNextButton.Should().BeTrue();
        parsed.Body.Should().BeOfType<OptionsDialog>().Which.Options.Should().Equal("Buy", "Sell", "Leave");
    }

    [Fact]
    public void RoundTrip_SimpleOptions_OmitsTextPrompt()
    {
        // SimpleOptions (3) carries no text prompt: a Text set on the packet must not reach the wire.
        var packet = new NpcDialogPacket
        {
            DialogType = NpcDialogType.SimpleOptions,
            Text = "ignored",
            Body = new OptionsDialog { Options = ["A", "B"] },
        };

        NpcDialogPacket.CarriesTextPrompt(NpcDialogType.SimpleOptions).Should().BeFalse();

        var parsed = Roundtrip(packet);
        parsed.Text.Should().BeEmpty();
        parsed.Body.Should().BeOfType<OptionsDialog>().Which.Options.Should().Equal("A", "B");
    }

    [Fact]
    public void RoundTrip_TextInput_TopLenBottom()
    {
        var packet = new NpcDialogPacket
        {
            DialogType = NpcDialogType.TextInput,
            Name = "Banker",
            Text = "Name your deposit",
            Body = new TextInputDialog { TopCaption = "Amount", InputLength = 12, BottomCaption = "gold" },
        };

        var body = Roundtrip(packet).Body.Should().BeOfType<TextInputDialog>().Subject;
        body.TopCaption.Should().Be("Amount");
        body.InputLength.Should().Be(12);
        body.BottomCaption.Should().Be("gold");
    }

    [Fact]
    public void RoundTrip_SimpleTextInput_OmitsTextPrompt()
    {
        var packet = new NpcDialogPacket
        {
            DialogType = NpcDialogType.SimpleTextInput,
            Text = "ignored",
            Body = new TextInputDialog { TopCaption = "Top", InputLength = 5, BottomCaption = "Bot" },
        };

        var parsed = Roundtrip(packet);
        parsed.Text.Should().BeEmpty();
        parsed.Body.Should().BeOfType<TextInputDialog>().Which.InputLength.Should().Be(5);
    }

    [Fact]
    public void Close_IsTypeOnly()
    {
        var packet = new NpcDialogPacket { DialogType = NpcDialogType.Close, Body = new CloseDialog() };

        packet.ToBody().Should().Equal((byte)0x0A);

        // A close may carry trailing bytes; parse must tolerate them.
        var withTrailing = NpcDialogPacket.Parse([0x0A, 0x00]);
        withTrailing.DialogType.Should().Be(NpcDialogType.Close);
        withTrailing.Body.Should().BeOfType<CloseDialog>();
    }

    [Fact]
    public void WriteBody_IncompatibleBody_Throws()
    {
        var packet = new NpcDialogPacket
        {
            DialogType = NpcDialogType.TextInput,
            Body = new OptionsDialog { Options = ["x"] },
        };

        var act = () => packet.ToBody();
        act.Should().Throw<InvalidOperationException>();
    }
}
