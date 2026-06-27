using System;

namespace DALib.Networking.Wire;

/// <summary>
///     Marks a type as the dispatch target for an inbound C->S wire packet of the given opcode.
/// </summary>
/// <remarks>
///     Place on a concrete <see cref="ClientPacket" />-derived record for single-variant opcodes,
///     or on a per-opcode abstract base when the opcode dispatches to multiple variant records.
///     The attributed type must expose a <c>public static</c> method
///     <c>Parse(ReadOnlySpan&lt;byte&gt;)</c> returning a value assignable to
///     <see cref="IClientPacket" />. <see cref="PacketCodec" /> discovers these attributed types
///     in the assemblies passed to its constructor and builds an opcode-to-parser dispatch table.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ClientOpcodeAttribute(ClientOpcode opcode) : Attribute
{
    /// <summary>The opcode this type dispatches, as a named catalog value.</summary>
    public ClientOpcode OpcodeValue { get; } = opcode;

    /// <summary>The single on-wire opcode byte this type dispatches.</summary>
    public byte Opcode { get; } = (byte)opcode;
}
