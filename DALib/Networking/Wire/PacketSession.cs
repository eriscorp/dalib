using System;
using DALib.Networking.Crypto;

namespace DALib.Networking.Wire;

/// <summary>
///     Pairs a shared <see cref="PacketCodec" /> with a single connection's
///     <see cref="CryptoState" /> so consumers can encode and parse packets without
///     threading crypto state through every call.
/// </summary>
/// <remarks>
///     <para>
///         The codec is stateless and meant to be shared across every connection; only
///         <see cref="CryptoState" /> is per-connection. The intended usage is one codec
///         for the process and one <see cref="PacketSession" /> per connection, each
///         wrapping that shared codec and the connection's own crypto state.
///     </para>
///     <para>
///         Every method forwards directly to the codec, supplying <see cref="Crypto" />.
///         See <see cref="PacketCodec" /> for wire-format, framing, and encryption details
///         - including how the encryption method is derived from each opcode.
///     </para>
/// </remarks>
public sealed record PacketSession
{
    /// <summary>The shared, stateless codec that owns framing, dispatch, and crypto routing.</summary>
    public PacketCodec Codec { get; }

    /// <summary>This connection's crypto state - ordinals, key table, and encryption seed.</summary>
    public CryptoState Crypto { get; }

    /// <summary>
    ///     Pairs a shared <paramref name="codec" /> with one connection's
    ///     <paramref name="crypto" /> state.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="codec" /> or <paramref name="crypto" /> is null.
    /// </exception>
    public PacketSession(PacketCodec codec, CryptoState crypto)
    {
        ArgumentNullException.ThrowIfNull(codec);
        ArgumentNullException.ThrowIfNull(crypto);

        Codec = codec;
        Crypto = crypto;
    }

    /// <summary>
    ///     Encodes a C->S packet into outbound wire bytes, advancing the session's client
    ///     ordinal for encrypted opcodes unless <paramref name="sequence" /> is supplied.
    /// </summary>
    /// <param name="packet">The packet to encode.</param>
    /// <param name="sequence">
    ///     Override the auto-allocated ordinal for testing / protocol probing. Ignored for
    ///     opcodes mapped to <see cref="EncryptMethod.None" />, which carry no ordinal.
    /// </param>
    /// <param name="slop">
    ///     Optional extra bytes appended after the packet body and before encryption (or
    ///     before the trailing 0x00 for None mode).
    /// </param>
    public ReadOnlyMemory<byte> EncodeClient(
        IClientPacket packet,
        byte? sequence = null,
        ReadOnlyMemory<byte> slop = default)
        => Codec.EncodeClient(packet, Crypto, sequence, slop);

    /// <summary>
    ///     Encodes an S->C packet into outbound wire bytes, advancing the session's server
    ///     ordinal for encrypted opcodes unless <paramref name="sequence" /> is supplied.
    /// </summary>
    public ReadOnlyMemory<byte> EncodeServer(
        IServerPacket packet,
        byte? sequence = null,
        ReadOnlyMemory<byte> slop = default)
        => Codec.EncodeServer(packet, Crypto, sequence, slop);

    /// <summary>
    ///     Parses exactly one C->S packet from <paramref name="wireBytes" />, which must
    ///     contain a single complete frame and no trailing bytes.
    /// </summary>
    public IClientPacket ParseClientPacket(ReadOnlyMemory<byte> wireBytes)
        => Codec.ParseClientPacket(wireBytes, Crypto);

    /// <summary>
    ///     Parses exactly one S->C packet from <paramref name="wireBytes" />, which must
    ///     contain a single complete frame and no trailing bytes.
    /// </summary>
    public IServerPacket ParseServerPacket(ReadOnlyMemory<byte> wireBytes)
        => Codec.ParseServerPacket(wireBytes, Crypto);

    /// <summary>
    ///     Attempts to pull a single complete C->S packet from <paramref name="buffer" />.
    ///     Returns false (no exception) if the buffer is too short for a full frame.
    /// </summary>
    public bool TryGetClientPacket(
        ReadOnlyMemory<byte> buffer,
        out IClientPacket? packet,
        out int bytesConsumed)
        => Codec.TryGetClientPacket(buffer, Crypto, out packet, out bytesConsumed);

    /// <summary>
    ///     Attempts to pull a single complete S->C packet from <paramref name="buffer" />.
    ///     Returns false (no exception) if the buffer is too short for a full frame.
    /// </summary>
    public bool TryGetServerPacket(
        ReadOnlyMemory<byte> buffer,
        out IServerPacket? packet,
        out int bytesConsumed)
        => Codec.TryGetServerPacket(buffer, Crypto, out packet, out bytesConsumed);
}
