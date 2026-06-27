using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x39 (C->S) - interact with a dialog-capable object (NPC, item, reactor, castable) and
///     select a pursuit, or answer a merchant-menu prompt. Every form shares the prefix
///     <c>[u8 ObjectType][u32 BE ObjectId][u16 BE PursuitId]</c>; the trailing payload then varies
///     by which menu the server last displayed.
/// </summary>
/// <remarks>
///     <para>
///         The tail is not self-describing. Unlike 0x3A (<see cref="DialogUsePacket" />), whose
///         response tail carries a wire tag, the 0x39 tail shape (text / two-text / option / handle)
///         is recoverable only from the menu type the server last displayed plus the pursuit id -
///         dialog state, not bytes on the wire. DALib therefore models the forms as send-side typed
///         variants: each overrides <see cref="ClientPacket.WriteBody" /> and offers an opt-in static
///         <c>ParseResponse</c> the caller invokes when it already knows the form. They are not
///         auto-dispatched - the codec binds 0x39 to <see cref="Parse" />, which always yields the
///         bare <see cref="NpcMainMenuSelectPacket" /> (the only form recoverable without state). An
///         inbound 0x39 carrying a merchant tail therefore deserializes to the bare select form with
///         the tail silently dropped; a caller wanting the typed tail calls the matching variant's
///         <c>ParseResponse</c> on the body directly.
///     </para>
///     <para>
///         The 6-byte dialog-obfuscation header (<c>[rand][rand][len][len][crc][crc]</c>) is
///         added/stripped by the codec's <see cref="DALib.Networking.Crypto.DialogObfuscation" />
///         layer and is not part of the body modeled here.
///     </para>
/// </remarks>
[ClientOpcode(ClientOpcode.NpcMainMenu)]
public abstract record NpcMainMenuPacket : ClientPacket
{
    /// <summary>The interacting object is a creature/NPC.</summary>
    public const byte ObjectTypeCreature = 0x01;

    /// <summary>The interacting object is an item.</summary>
    public const byte ObjectTypeItem = 0x02;

    /// <summary>The interacting object is a reactor tile.</summary>
    public const byte ObjectTypeReactor = 0x04;

    /// <summary>The interaction targets a castable (spell/skill) object.</summary>
    public const byte ObjectTypeCastable = 0x05;

    /// <summary>The interaction is part of an asynchronous dialog session.</summary>
    public const byte ObjectTypeAsynchronous = 0xFE;

    /// <summary>
    ///     The fixed <c>0x01</c> lead byte of a structured tail (Form D, and the wrapped option Form
    ///     E). A constant marker, not a wire discriminator; there is no tag-based dispatch on 0x39.
    /// </summary>
    private protected const byte StructuredTailLead = 0x01;

    /// <summary>The shared prefix length: objType + objId + pursuitId.</summary>
    private const int PrefixLength = 1 + 4 + 2;

    /// <summary>What kind of object the interaction is on. See the <c>ObjectType*</c> constants.</summary>
    public required byte ObjectType { get; init; }

    /// <summary>The interacting object's serial.</summary>
    public required uint ObjectId { get; init; }

    /// <summary>The pursuit (dialog sequence / merchant menu item) the message belongs to.</summary>
    public required ushort PursuitId { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.NpcMainMenu;

    /// <summary>Writes the shared prefix. Variants call this, then append their tail (if any).</summary>
    protected void WritePrefix(IPacketWriter writer)
    {
        writer.WriteByte(ObjectType);
        writer.WriteUInt32(ObjectId);
        writer.WriteUInt16(PursuitId);
    }

    /// <summary>
    ///     Reads the shared prefix, leaving the reader positioned at the (possibly empty) tail.
    ///     <see langword="protected" /> so variant <c>ParseResponse</c> methods reuse it.
    /// </summary>
    protected static (byte ObjectType, uint ObjectId, ushort PursuitId) ReadPrefix(ref PacketReader reader)
        => (reader.ReadByte(), reader.ReadUInt32(), reader.ReadUInt16());

    /// <summary>
    ///     The opcode-dispatch entry the codec binds for 0x39. Because the merchant tail is not
    ///     self-describing, this always yields the bare <see cref="NpcMainMenuSelectPacket" />
    ///     (prefix only); any trailing merchant payload on an inbound frame is ignored. Callers that
    ///     know the form parse it with the matching variant's <c>ParseResponse</c>.
    /// </summary>
    public static NpcMainMenuPacket Parse(ReadOnlySpan<byte> body) => NpcMainMenuSelectPacket.ParseResponse(body);
}

/// <summary>
///     0x39 Form A - the bare prefix with no tail. Sent to open a pursuit / start a dialog sequence
///     on the clicked object, and to navigate a no-argument options menu. This is the form the codec
///     auto-dispatches an inbound 0x39 to.
/// </summary>
public sealed record NpcMainMenuSelectPacket : NpcMainMenuPacket
{
    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => WritePrefix(writer);

    /// <summary>Parses the bare form (prefix only); ignores any trailing merchant payload.</summary>
    public static NpcMainMenuSelectPacket ParseResponse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var (objectType, objectId, pursuitId) = ReadPrefix(ref reader);

        return new NpcMainMenuSelectPacket
        {
            ObjectType = objectType,
            ObjectId = objectId,
            PursuitId = pursuitId,
        };
    }
}

/// <summary>
///     0x39 Form B - tail <c>[u8 len][latin-1 string]</c>. A single text input: a quantity typed as
///     text, a skill/spell name, a parcel recipient, a buy/sell confirmation.
/// </summary>
public sealed record NpcTextResponsePacket : NpcMainMenuPacket
{
    /// <summary>The typed input string.</summary>
    public required string Text { get; init; }

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WritePrefix(writer);
        writer.WriteString8(Text);
    }

    /// <summary>Parses a single-string response (prefix + <c>[u8 len][string]</c>).</summary>
    public static NpcTextResponsePacket ParseResponse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var (objectType, objectId, pursuitId) = ReadPrefix(ref reader);

        return new NpcTextResponsePacket
        {
            ObjectType = objectType,
            ObjectId = objectId,
            PursuitId = pursuitId,
            Text = reader.ReadString8(),
        };
    }
}

/// <summary>
///     0x39 Form C - tail <c>[u8 len][name][u8 len][quantity]</c>: two strings, name first then
///     quantity. The buy-with-quantity response.
/// </summary>
public sealed record NpcTextPairResponsePacket : NpcMainMenuPacket
{
    /// <summary>The item/object name (first string on the wire).</summary>
    public required string Name { get; init; }

    /// <summary>The typed quantity (second string on the wire; a numeric string).</summary>
    public required string Quantity { get; init; }

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WritePrefix(writer);
        writer.WriteString8(Name);
        writer.WriteString8(Quantity);
    }

    /// <summary>Parses the name+quantity pair (prefix + two <c>string8</c>, name first).</summary>
    public static NpcTextPairResponsePacket ParseResponse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var (objectType, objectId, pursuitId) = ReadPrefix(ref reader);

        return new NpcTextPairResponsePacket
        {
            ObjectType = objectType,
            ObjectId = objectId,
            PursuitId = pursuitId,
            Name = reader.ReadString8(),
            Quantity = reader.ReadString8(),
        };
    }
}

/// <summary>
///     0x39 Form E - tail <c>[u8 Option]</c>. An options-menu selection: a slot/index chosen from a
///     list (sell/deposit-by-slot, forget skill/spell).
/// </summary>
public sealed record NpcOptionResponsePacket : NpcMainMenuPacket
{
    /// <summary>The chosen option/slot index.</summary>
    public required byte Option { get; init; }

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WritePrefix(writer);
        writer.WriteByte(Option);
    }

    /// <summary>Parses an option response (prefix + <c>[u8 Option]</c>).</summary>
    public static NpcOptionResponsePacket ParseResponse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var (objectType, objectId, pursuitId) = ReadPrefix(ref reader);

        return new NpcOptionResponsePacket
        {
            ObjectType = objectType,
            ObjectId = objectId,
            PursuitId = pursuitId,
            Option = reader.ReadByte(),
        };
    }
}

/// <summary>
///     0x39 Form E (wrapped) - tail <c>[0x01][u8 Option][0x01]</c>: the option byte bracketed by
///     fixed <c>0x01</c> markers, used for an options-with-argument menu. Modeled for protocol
///     completeness; not emitted by typical servers.
/// </summary>
public sealed record NpcOptionArgumentResponsePacket : NpcMainMenuPacket
{
    /// <summary>The chosen option/slot index (between the <c>0x01</c> markers).</summary>
    public required byte Option { get; init; }

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WritePrefix(writer);
        writer.WriteByte(StructuredTailLead);
        writer.WriteByte(Option);
        writer.WriteByte(StructuredTailLead);
    }

    /// <summary>Parses the wrapped option response (prefix + <c>[0x01][u8 Option][0x01]</c>).</summary>
    public static NpcOptionArgumentResponsePacket ParseResponse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var (objectType, objectId, pursuitId) = ReadPrefix(ref reader);
        reader.ReadByte(); // leading 0x01 marker
        var option = reader.ReadByte();
        reader.ReadByte(); // trailing 0x01 marker

        return new NpcOptionArgumentResponsePacket
        {
            ObjectType = objectType,
            ObjectId = objectId,
            PursuitId = pursuitId,
            Option = option,
        };
    }
}

/// <summary>
///     0x39 Form D - tail <c>[0x01][u32 Handle][u8 Param]</c>. The select-response of an ID-keyed
///     server-item menu: instead of echoing the row's name string (Form B), the response carries the
///     server-assigned row <see cref="Handle" />. The leading <c>0x01</c> is a fixed structured-tail
///     marker, not a discriminator. Modeled for protocol completeness; not emitted by typical servers.
/// </summary>
public sealed record NpcHandleResponsePacket : NpcMainMenuPacket
{
    /// <summary>The server-assigned row handle being selected (echoed back verbatim).</summary>
    public required uint Handle { get; init; }

    /// <summary>The trailing parameter byte (typically 1).</summary>
    public required byte Param { get; init; }

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WritePrefix(writer);
        writer.WriteByte(StructuredTailLead);
        writer.WriteUInt32(Handle);
        writer.WriteByte(Param);
    }

    /// <summary>Parses the handle response (prefix + <c>[0x01][u32 Handle][u8 Param]</c>).</summary>
    public static NpcHandleResponsePacket ParseResponse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var (objectType, objectId, pursuitId) = ReadPrefix(ref reader);
        reader.ReadByte(); // leading 0x01 marker

        return new NpcHandleResponsePacket
        {
            ObjectType = objectType,
            ObjectId = objectId,
            PursuitId = pursuitId,
            Handle = reader.ReadUInt32(),
            Param = reader.ReadByte(),
        };
    }
}
