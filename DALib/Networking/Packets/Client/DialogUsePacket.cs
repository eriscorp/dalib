using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x3A (C->S) - a response within an <em>open</em> dialog. The 9-byte prefix
///     <c>[u8 ObjectType][u32 BE ObjectId][u16 BE PursuitId][u16 BE PursuitIndex]</c> is shared
///     by every form; the tail then selects the form via a self-describing tag byte:
///     <list type="bullet">
///         <item><b>none</b> -> <see cref="DialogNavigationPacket" /> (next / prev / close)</item>
///         <item><c>0x01</c> -> <see cref="DialogOptionResponsePacket" /> (menu selection)</item>
///         <item><c>0x02</c> -> <see cref="DialogTextResponsePacket" /> (typed input)</item>
///     </list>
/// </summary>
/// <remarks>
///     <para>
///         Unlike <see cref="NpcMainMenuPacket" /> (0x39), whose merchant-response tail carries
///         <em>no</em> wire discriminator and is recoverable only from server dialog state, the
///         0x3A tail <strong>is self-describing</strong>: a <c>0x01</c> tag precedes a menu option
///         and a <c>0x02</c> tag precedes a text string. Because the tag is on the wire, 0x3A is
///         parsed statelessly here.
///     </para>
///     <para>
///         The tag is a <strong>free byte</strong> (named constants <see cref="TagMenu" /> /
///         <see cref="TagText" />, not a closed enum) so a consumer can model a net-new dialog
///         form by declaring its own <c>[DialogResponseType]</c> variant - the same extension
///         story <see cref="ClientOpcodeAttribute" /> gives for whole opcodes, one level down.
///         The 6-byte dialog-obfuscation header is added/stripped by the codec's
///         <see cref="DALib.Networking.Crypto.DialogObfuscation" /> layer and is not modeled here.
///     </para>
/// </remarks>
[ClientOpcode(ClientOpcode.DialogUse)]
public abstract record DialogUsePacket : ClientPacket
{
    /// <summary>The clicked object is a creature/NPC.</summary>
    public const byte ObjectTypeCreature = 0x01;

    /// <summary>The clicked object is an item.</summary>
    public const byte ObjectTypeItem = 0x02;

    /// <summary>The clicked object is a reactor tile.</summary>
    public const byte ObjectTypeReactor = 0x04;

    /// <summary>The interaction targets a castable (spell/skill) object.</summary>
    public const byte ObjectTypeCastable = 0x05;

    /// <summary>The interaction is part of an asynchronous dialog session.</summary>
    public const byte ObjectTypeAsynchronous = 0xFE;

    /// <summary>Response-tag byte for a menu/options selection.</summary>
    public const byte TagMenu = 0x01;

    /// <summary>Response-tag byte for a text input.</summary>
    public const byte TagText = 0x02;

    /// <summary>The shared 9-byte prefix length (objType + objId + pursuitId + pursuitIndex).</summary>
    private const int PrefixLength = 1 + 4 + 2 + 2;

    /// <summary>What kind of object the dialog is on. See the <c>ObjectType*</c> constants.</summary>
    public required byte ObjectType { get; init; }

    /// <summary>The dialog object's serial.</summary>
    public required uint ObjectId { get; init; }

    /// <summary>The pursuit (dialog sequence) the response belongs to.</summary>
    public required ushort PursuitId { get; init; }

    /// <summary>The step within the sequence: the response advances (index+1), goes back
    ///     (index-1 = prev), or echoes the current index (close).</summary>
    public required ushort PursuitIndex { get; init; }

    /// <summary>
    ///     The self-describing response-tag byte this variant writes, or <see langword="null" />
    ///     for the no-tail <see cref="DialogNavigationPacket" />. Exposed so callers can inspect
    ///     the form without a downcast and so each variant single-sources its literal.
    /// </summary>
    public abstract byte? ResponseType { get; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.DialogUse;

    /// <summary>
    ///     DALib's built-in response-tag -> variant table, scanned once from the DALib assembly.
    ///     Immutable; used by the standalone <see cref="Parse" /> and as the default the codec
    ///     widens in <see cref="WireInto" />.
    /// </summary>
    private static readonly IReadOnlyDictionary<byte, WireParseFn<DialogUsePacket>> BuiltInVariants =
        PacketDispatchBuilder.Build<DialogResponseTypeAttribute, DialogUsePacket>(
            [typeof(IPacket).Assembly],
            attr => attr.Tag,
            parseMethodName: nameof(DialogOptionResponsePacket.ParseResponse));

    /// <summary>
    ///     Writes the shared 9-byte prefix. Variants call this, then append their tag + tail.
    /// </summary>
    protected void WritePrefix(IPacketWriter writer)
    {
        writer.WriteByte(ObjectType);
        writer.WriteUInt32(ObjectId);
        writer.WriteUInt16(PursuitId);
        writer.WriteUInt16(PursuitIndex);
    }

    /// <summary>
    ///     Reads the shared 9-byte prefix into a tuple, leaving the reader at the tag byte.
    ///     <see langword="protected" /> so consumer-declared variants can reuse it rather than
    ///     hand-reading the prefix.
    /// </summary>
    protected static (byte ObjectType, uint ObjectId, ushort PursuitId, ushort PursuitIndex)
        ReadPrefix(ref PacketReader reader)
        => (reader.ReadByte(), reader.ReadUInt32(), reader.ReadUInt16(), reader.ReadUInt16());

    /// <summary>
    ///     Parses a 0x3A body, dispatching on the self-describing tag over DALib's built-in
    ///     variants. This is the standalone entry (usable without a codec) and what
    ///     <see cref="ClientOpcodeAttribute" /> dispatch binds; <see cref="PacketCodec" /> widens
    ///     it to consumer assemblies via <see cref="WireInto" />.
    /// </summary>
    public static DialogUsePacket Parse(ReadOnlySpan<byte> body) => Dispatch(body, BuiltInVariants);

    /// <summary>
    ///     The shared sub-dispatch: read the prefix, choose the variant by tag (or navigation
    ///     when the body is bare). Backs both <see cref="Parse" /> and <see cref="WireInto" /> so
    ///     the two paths cannot diverge.
    /// </summary>
    internal static DialogUsePacket Dispatch(
        ReadOnlySpan<byte> body,
        IReadOnlyDictionary<byte, WireParseFn<DialogUsePacket>> variants)
    {
        if (body.Length == PrefixLength)
            return DialogNavigationPacket.ParseResponse(body);

        if (body.Length < PrefixLength + 1)
            throw new InvalidDataException(
                $"0x3A DialogUse: body length {body.Length} is between the {PrefixLength}-byte " +
                "prefix and a tagged tail.");

        var tag = body[PrefixLength];

        if (!variants.TryGetValue(tag, out var parse))
            throw new InvalidDataException(
                $"0x3A DialogUse: no registered response type for tag 0x{tag:X2}.");

        return parse(body);
    }

    /// <summary>
    ///     Widens the codec's 0x3A dispatch to every <see cref="DialogResponseTypeAttribute" />
    ///     variant in <paramref name="assemblies" /> (DALib's own plus any consumer's), keeping the
    ///     resulting table on the codec instance - no global state. Called once at codec
    ///     construction.
    /// </summary>
    internal static void WireInto(
        Dictionary<byte, WireParseFn<IClientPacket>> clientParsers,
        IReadOnlyList<Assembly> assemblies)
    {
        var variants = PacketDispatchBuilder.Build<DialogResponseTypeAttribute, DialogUsePacket>(
            assemblies,
            attr => attr.Tag,
            parseMethodName: nameof(DialogOptionResponsePacket.ParseResponse));

        clientParsers[(byte)ClientOpcode.DialogUse] = body => Dispatch(body, variants);
    }
}

/// <summary>
///     0x3A navigation - the bare 9-byte prefix with no tail. Sent to advance (next), go back
///     (PursuitIndex-1 = prev), or close (echo the current PursuitId+PursuitIndex). Carries no
///     response tag.
/// </summary>
public sealed record DialogNavigationPacket : DialogUsePacket
{
    /// <inheritdoc />
    public override byte? ResponseType => null;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => WritePrefix(writer);

    /// <summary>Parses a bare 0x3A body (prefix only). Invoked directly by the sub-dispatch.</summary>
    public static DialogNavigationPacket ParseResponse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var (objectType, objectId, pursuitId, pursuitIndex) = ReadPrefix(ref reader);

        return new DialogNavigationPacket
        {
            ObjectType = objectType,
            ObjectId = objectId,
            PursuitId = pursuitId,
            PursuitIndex = pursuitIndex,
        };
    }
}

/// <summary>
///     0x3A menu response - tail <c>[0x01][u8 Option]</c>. The selected option/slot of an
///     options dialog.
/// </summary>
[DialogResponseType(TagMenu)]
public sealed record DialogOptionResponsePacket : DialogUsePacket
{
    /// <summary>The chosen option index/slot.</summary>
    public required byte Option { get; init; }

    /// <inheritdoc />
    public override byte? ResponseType => TagMenu;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WritePrefix(writer);
        writer.WriteByte(TagMenu);
        writer.WriteByte(Option);
    }

    /// <summary>Parses an option response (prefix + <c>[0x01][option]</c>).</summary>
    public static DialogOptionResponsePacket ParseResponse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var (objectType, objectId, pursuitId, pursuitIndex) = ReadPrefix(ref reader);
        reader.ReadByte(); // tag (already selected by dispatch)

        return new DialogOptionResponsePacket
        {
            ObjectType = objectType,
            ObjectId = objectId,
            PursuitId = pursuitId,
            PursuitIndex = pursuitIndex,
            Option = reader.ReadByte(),
        };
    }
}

/// <summary>
///     0x3A text response - tail <c>[0x02][u8 len][latin-1 string]</c>. The typed input of a
///     text dialog.
/// </summary>
/// <remarks>
///     Only a single <see cref="Text" /> string is modeled. A real text interaction also puts a
///     separate 0x0E chat-echo packet on the wire; that is the UI's concern, not part of this
///     packet.
/// </remarks>
[DialogResponseType(TagText)]
public sealed record DialogTextResponsePacket : DialogUsePacket
{
    /// <summary>The typed input string.</summary>
    public required string Text { get; init; }

    /// <inheritdoc />
    public override byte? ResponseType => TagText;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WritePrefix(writer);
        writer.WriteByte(TagText);
        writer.WriteString8(Text);
    }

    /// <summary>Parses a text response (prefix + <c>[0x02][string8]</c>).</summary>
    public static DialogTextResponsePacket ParseResponse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var (objectType, objectId, pursuitId, pursuitIndex) = ReadPrefix(ref reader);
        reader.ReadByte(); // tag

        return new DialogTextResponsePacket
        {
            ObjectType = objectType,
            ObjectId = objectId,
            PursuitId = pursuitId,
            PursuitIndex = pursuitIndex,
            Text = reader.ReadString8(),
        };
    }
}
