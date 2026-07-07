using System;
using System.Collections.Generic;
using System.IO;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x30 (S->C) - displays a scripted NPC dialog (plain text, an options menu, or a text-entry
///     prompt). A shared prefix (the source entity, sprite, nav state, name and an optional text prompt)
///     is followed by exactly one body shape; the leading <see cref="DialogType" /> selects which.
///     Answered with C->S 0x3A (<see cref="DALib.Networking.Packets.Client.DialogUsePacket" />).
/// </summary>
/// <remarks>
///     <para>
///         Clicking an NPC (C->S 0x43) yields either 0x2F (<see cref="NpcMenuPacket" />, merchant menus,
///         answered by 0x39) or this packet (scripted dialog, answered by 0x3A).
///     </para>
///     <para>
///         <see cref="DialogType" /> selects the body shape and whether the prefix carries a
///         <see cref="Text" /> prompt (present for every modeled type except the "Simple" variants; see
///         <see cref="CarriesTextPrompt" />). <see cref="NpcDialogType.Close" /> is a type-only packet:
///         no prefix follows the type byte, and any trailing bytes are ignored. Four prefix bytes
///         (<see cref="Unknown1" />, <see cref="Unknown2" />, <see cref="Sprite2" /> and
///         <see cref="Color2" />) are parsed but unused; they are preserved for round-tripping.
///     </para>
/// </remarks>
[ServerOpcode(ServerOpcode.NpcDialog)]
public sealed record NpcDialogPacket : ServerPacket
{
    /// <summary>The source entity is a creature/NPC (the common case).</summary>
    public const byte ObjectTypeCreature = 0x01;

    /// <summary>The source entity is an item.</summary>
    public const byte ObjectTypeItem = 0x02;

    /// <summary>The source entity is a reactor (a map trigger/tile).</summary>
    public const byte ObjectTypeReactor = 0x04;

    /// <summary>The source entity is a castable (spell/skill).</summary>
    public const byte ObjectTypeCastable = 0x05;

    /// <summary>The dialog is asynchronous (server-driven, no concrete origin object).</summary>
    public const byte ObjectTypeAsynchronous = 0xFE;

    /// <summary>The wire DialogType byte. Selects the <see cref="Body" /> shape and the text rule; see <see cref="NpcDialogType" />.</summary>
    public required NpcDialogType DialogType { get; set; }

    /// <summary>
    ///     The source entity kind (<see cref="ObjectTypeCreature" /> etc.). Determines how
    ///     <see cref="Sprite" /> is interpreted (item/creature/castable sprite-offset ranges). Not present
    ///     for <see cref="NpcDialogType.Close" />.
    /// </summary>
    public byte ObjectType { get; set; } = ObjectTypeCreature;

    /// <summary>The source entity's server id. Not present for <see cref="NpcDialogType.Close" />.</summary>
    public uint SourceId { get; set; }

    /// <summary>
    ///     First prefix byte after <see cref="SourceId" />. Parsed but unused; emit 0. Preserved as a
    ///     settable round-trip field.
    /// </summary>
    public byte Unknown1 { get; set; }

    /// <summary>The portrait sprite shown beside the dialog, raw on-wire value (item/creature-offset-encoded).</summary>
    public ushort Sprite { get; set; }

    /// <summary>The color applied to <see cref="Sprite" />.</summary>
    public byte Color { get; set; }

    /// <summary>
    ///     Second prefix byte, between the two sprite/color pairs. Parsed but unused; emit 0. Settable
    ///     round-trip field.
    /// </summary>
    public byte Unknown2 { get; set; }

    /// <summary>
    ///     A second sprite slot on the wire. Parsed but unused; settable round-trip field.
    /// </summary>
    public ushort Sprite2 { get; set; }

    /// <summary>A second color slot, paired with <see cref="Sprite2" /> on the wire. Parsed but unused; settable round-trip field.</summary>
    public byte Color2 { get; set; }

    /// <summary>The pursuit id this dialog belongs to. Echoed back in the 0x3A response.</summary>
    public ushort PursuitId { get; set; }

    /// <summary>The index of this dialog within its pursuit/sequence.</summary>
    public ushort DialogId { get; set; }

    /// <summary>Whether to show the "previous" navigation button.</summary>
    public bool HasPreviousButton { get; set; }

    /// <summary>Whether to show the "next" navigation button.</summary>
    public bool HasNextButton { get; set; }

    /// <summary>
    ///     Trailing prefix byte after the nav flags. Parsed but unused; emit 0. Settable round-trip field.
    /// </summary>
    public byte Unknown3 { get; set; }

    /// <summary>The source entity's display name (<c>string8</c>). Not present for <see cref="NpcDialogType.Close" />.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     The dialog's main text prompt (<c>string16</c>). On the wire only when
    ///     <see cref="CarriesTextPrompt" /> is true for <see cref="DialogType" /> (every modeled type
    ///     except the "Simple" variants and <see cref="NpcDialogType.Close" />).
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>The dialog body. Its shape must be compatible with <see cref="DialogType" />. Never null.</summary>
    public required NpcDialog Body { get; set; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.NpcDialog;

    /// <summary>Whether a <paramref name="dialogType" /> carries the <see cref="Text" /> prompt in its prefix.</summary>
    public static bool CarriesTextPrompt(NpcDialogType dialogType)
        => dialogType is not (NpcDialogType.SimpleOptions or NpcDialogType.SimpleTextInput or NpcDialogType.Close);

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        if (!Body.Accepts(DialogType))
            throw new InvalidOperationException(
                $"NpcDialogPacket: body {Body.GetType().Name} is not valid for DialogType {DialogType}.");

        writer.WriteByte((byte)DialogType);

        // Close is a type-only packet: no prefix follows the type byte.
        if (DialogType == NpcDialogType.Close)
            return;

        writer.WriteByte(ObjectType);
        writer.WriteUInt32(SourceId);
        writer.WriteByte(Unknown1);
        writer.WriteUInt16(Sprite);
        writer.WriteByte(Color);
        writer.WriteByte(Unknown2);
        writer.WriteUInt16(Sprite2);
        writer.WriteByte(Color2);
        writer.WriteUInt16(PursuitId);
        writer.WriteUInt16(DialogId);
        writer.WriteBoolean(HasPreviousButton);
        writer.WriteBoolean(HasNextButton);
        writer.WriteByte(Unknown3);
        writer.WriteString8(Name);

        if (CarriesTextPrompt(DialogType))
            writer.WriteString16(Text);

        Body.Write(writer);
    }

    /// <inheritdoc />
    public static NpcDialogPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var dialogType = (NpcDialogType)reader.ReadByte();

        // Close carries no prefix; tolerate any trailing bytes.
        if (dialogType == NpcDialogType.Close)
            return new NpcDialogPacket { DialogType = dialogType, Body = new CloseDialog() };

        var objectType = reader.ReadByte();
        var sourceId = reader.ReadUInt32();
        var unknown1 = reader.ReadByte();
        var sprite = reader.ReadUInt16();
        var color = reader.ReadByte();
        var unknown2 = reader.ReadByte();
        var sprite2 = reader.ReadUInt16();
        var color2 = reader.ReadByte();
        var pursuitId = reader.ReadUInt16();
        var dialogId = reader.ReadUInt16();
        var hasPreviousButton = reader.ReadBoolean();
        var hasNextButton = reader.ReadBoolean();
        var unknown3 = reader.ReadByte();
        var name = reader.ReadString8();
        var text = CarriesTextPrompt(dialogType) ? reader.ReadString16() : string.Empty;

        var dialog = NpcDialog.Parse(dialogType, ref reader);

        //DOOMVAS encryption appends an inner-pad byte (0x00, or 0x00+opcode for MD5Key) before the rand
        //footer; DecryptServer strips only the footer, so a trailing pad may remain. Tolerate it rather
        //than reject the packet (see DecryptServer inner-pad strip, the proper fix).

        return new NpcDialogPacket
        {
            DialogType = dialogType,
            ObjectType = objectType,
            SourceId = sourceId,
            Unknown1 = unknown1,
            Sprite = sprite,
            Color = color,
            Unknown2 = unknown2,
            Sprite2 = sprite2,
            Color2 = color2,
            PursuitId = pursuitId,
            DialogId = dialogId,
            HasPreviousButton = hasPreviousButton,
            HasNextButton = hasNextButton,
            Unknown3 = unknown3,
            Name = name,
            Text = text,
            Body = dialog,
        };
    }
}

/// <summary>
///     The body of an <see cref="NpcDialogPacket" /> (S->C 0x30): one dialog shape. Which
///     <see cref="NpcDialogType" /> values a shape is valid for is reported by <see cref="Accepts" />.
///     Sealed variants follow this file.
/// </summary>
public abstract record NpcDialog
{
    /// <summary>Whether this body shape is a valid body for <paramref name="dialogType" /> on the wire.</summary>
    internal abstract bool Accepts(NpcDialogType dialogType);

    /// <summary>Writes this body's bytes, following the packet prefix and optional text prompt.</summary>
    internal abstract void Write(IPacketWriter writer);

    /// <summary>Reads the body matching <paramref name="dialogType" /> from the position after the prefix/text.</summary>
    internal static NpcDialog Parse(NpcDialogType dialogType, ref PacketReader reader) => dialogType switch
    {
        NpcDialogType.Normal => new TextDialog(),
        NpcDialogType.Options or NpcDialogType.SimpleOptions or NpcDialogType.OptionsWithFace
            => OptionsDialog.ParseBody(ref reader),
        NpcDialogType.TextInput or NpcDialogType.SimpleTextInput or NpcDialogType.ProtectedTextInput
            => TextInputDialog.ParseBody(ref reader),
        NpcDialogType.Close => new CloseDialog(),
        _ => throw new InvalidDataException($"NpcDialogPacket: unknown dialog type 0x{(byte)dialogType:X2}."),
    };
}

/// <summary>
///     0x30 body - <see cref="NpcDialogType.Normal" />: no body. A plain text dialog; only the prefix
///     and the <see cref="NpcDialogPacket.Text" /> prompt are shown.
/// </summary>
public sealed record TextDialog : NpcDialog
{
    internal override bool Accepts(NpcDialogType dialogType) => dialogType == NpcDialogType.Normal;

    internal override void Write(IPacketWriter writer) { }
}

/// <summary>
///     0x30 body - an options menu for <see cref="NpcDialogType.Options" />,
///     <see cref="NpcDialogType.SimpleOptions" /> and <see cref="NpcDialogType.OptionsWithFace" />:
///     <c>[u8 count]</c> then that many option labels (<c>string8</c> each). The selection is reported by
///     index in the C->S 0x3A response - options carry no per-row id.
/// </summary>
public sealed record OptionsDialog : NpcDialog
{
    /// <summary>The option labels, in order. The chosen index (1-based) is reported in the 0x3A response.</summary>
    public IList<string> Options { get; init; } = [];

    internal override bool Accepts(NpcDialogType dialogType)
        => dialogType is NpcDialogType.Options or NpcDialogType.SimpleOptions or NpcDialogType.OptionsWithFace;

    internal override void Write(IPacketWriter writer)
    {
        if (Options.Count > byte.MaxValue)
            throw new InvalidOperationException(
                $"OptionsDialog: option count {Options.Count} exceeds the wire u8 limit ({byte.MaxValue}).");

        writer.WriteByte((byte)Options.Count);

        foreach (var option in Options)
            writer.WriteString8(option);
    }

    internal static OptionsDialog ParseBody(ref PacketReader reader)
    {
        var count = reader.ReadByte();
        var options = new List<string>(count);

        for (var i = 0; i < count; i++)
            options.Add(reader.ReadString8());

        return new OptionsDialog { Options = options };
    }
}

/// <summary>
///     0x30 body - a text-entry prompt for <see cref="NpcDialogType.TextInput" />,
///     <see cref="NpcDialogType.SimpleTextInput" /> and <see cref="NpcDialogType.ProtectedTextInput" />:
///     <c>[string8 TopCaption][u8 InputLength][string8 BottomCaption]</c>.
/// </summary>
public sealed record TextInputDialog : NpcDialog
{
    /// <summary>The caption shown above the input field.</summary>
    public string TopCaption { get; init; } = string.Empty;

    /// <summary>The maximum number of characters accepted in the input field.</summary>
    public byte InputLength { get; init; }

    /// <summary>The caption shown below the input field.</summary>
    public string BottomCaption { get; init; } = string.Empty;

    internal override bool Accepts(NpcDialogType dialogType)
        => dialogType is NpcDialogType.TextInput or NpcDialogType.SimpleTextInput or NpcDialogType.ProtectedTextInput;

    internal override void Write(IPacketWriter writer)
    {
        writer.WriteString8(TopCaption);
        writer.WriteByte(InputLength);
        writer.WriteString8(BottomCaption);
    }

    internal static TextInputDialog ParseBody(ref PacketReader reader)
    {
        var topCaption = reader.ReadString8();
        var inputLength = reader.ReadByte();
        var bottomCaption = reader.ReadString8();

        return new TextInputDialog { TopCaption = topCaption, InputLength = inputLength, BottomCaption = bottomCaption };
    }
}

/// <summary>
///     0x30 body - <see cref="NpcDialogType.Close" />: no body. A type-only packet that dismisses any
///     open dialog.
/// </summary>
public sealed record CloseDialog : NpcDialog
{
    internal override bool Accepts(NpcDialogType dialogType) => dialogType == NpcDialogType.Close;

    internal override void Write(IPacketWriter writer) { }
}
