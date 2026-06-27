using System;

namespace DALib.Networking.Wire;

/// <summary>
///     Marks a <see cref="DALib.Networking.Packets.Client.DialogUsePacket" /> variant as the
///     dispatch target for a C->S 0x3A dialog response whose tail begins with the given
///     <see cref="Tag" /> byte (the self-describing <c>DialogArgsType</c>: <c>0x01</c> = menu
///     option, <c>0x02</c> = text input).
/// </summary>
/// <remarks>
///     This keys the 0x3A sub-dispatch table on the response tag, one level below
///     <see cref="ClientOpcodeAttribute" />. The attributed type must derive from
///     <see cref="DALib.Networking.Packets.Client.DialogUsePacket" /> and expose a
///     <c>public static T ParseResponse(ReadOnlySpan&lt;byte&gt;)</c> method that reads the full
///     body (prefix + tag + tail); the distinct method name keeps it from colliding with the
///     base's inherited <c>Parse</c> dispatcher. An external assembly may declare its own
///     <c>[DialogResponseType(0x03)]</c> variant for a new dialog form; when passed to the
///     <see cref="PacketCodec" /> constructor it sends and parses strongly-typed, with no edit
///     to DALib.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class DialogResponseTypeAttribute(byte tag) : Attribute
{
    /// <summary>The self-describing tag byte that selects this response variant on the wire.</summary>
    public byte Tag { get; } = tag;
}
