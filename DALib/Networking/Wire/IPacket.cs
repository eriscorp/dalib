using System;
using DALib.Networking.Crypto;

namespace DALib.Networking.Wire;

/// <summary>
///     The common contract for every DOOMVAS v1 wire packet, regardless of direction.
/// </summary>
/// <remarks>
///     <para>
///         Concrete packets are records (see <see cref="ClientPacket" /> and
///         <see cref="ServerPacket" />); this interface exists so that direction-agnostic
///         tooling (hex dumpers, debug formatters, generic dispatch surfaces) can hold a
///         reference to any packet.
///     </para>
///     <para>
///         Packets are pure data. They do not own crypto, framing, or sequence state - those
///         concerns live in the codec.
///     </para>
/// </remarks>
public interface IPacket
{
    /// <summary>
    ///     A short human-readable label identifying the direction this packet travels
    ///     (e.g., <c>"client"</c> for C->S or <c>"server"</c> for S->C). Provided by
    ///     <see cref="IClientPacket" /> and <see cref="IServerPacket" /> via default
    ///     implementation; used by codec error messages.
    /// </summary>
    static abstract string Direction { get; }

    /// <summary>
    ///     The single-byte opcode that identifies this packet on the wire.
    /// </summary>
    byte Opcode { get; }

    /// <summary>
    ///     The encryption method the codec will apply, derived from <see cref="Opcode" />
    ///     by direction. Informational - the codec computes this itself during encode/decode;
    ///     it's exposed here for consumers that need to reason about a packet's encryption
    ///     out of band (debug tooling, edge-case handling).
    /// </summary>
    EncryptMethod EncryptMethod { get; }

    /// <summary>
    ///     Writes the plaintext body of the packet - everything after the opcode and before
    ///     framing/encryption - into <paramref name="writer" />.
    /// </summary>
    /// <param name="writer">The writer to emit body bytes into.</param>
    /// <remarks>
    ///     The body is whatever <see cref="WriteBody" /> emits, including any
    ///     protocol-meaningful trailers (e.g. the 0x03 Login integrity trailer). It does
    ///     <em>not</em> include opcode, sequence, framing markers, MD5 hash, or the
    ///     stream-encryption a/b/a tail - those are added by the codec.
    /// </remarks>
    void WriteBody(IPacketWriter writer);

    /// <summary>
    ///     Returns the plaintext body bytes as a freshly-allocated array. Equivalent to
    ///     running <see cref="WriteBody" /> against a fresh writer and copying the result.
    /// </summary>
    /// <remarks>
    ///     Convenience for tests, hex-dump tooling, and hand-comparison against captures.
    ///     No opcode, no framing, no encryption.
    /// </remarks>
    byte[] ToBody();

    /// <summary>
    ///     Returns the plaintext body bytes as a <see cref="ReadOnlyMemory{T}" /> over a
    ///     freshly-allocated buffer. Semantically identical to <see cref="ToBody" />.
    /// </summary>
    ReadOnlyMemory<byte> ToBodyMemory();
}
