using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;

namespace DALib.Networking.Wire;

/// <summary>
///     Encodes typed packets to wire bytes and parses wire bytes back into typed packets,
///     for both C->S and S->C directions.
/// </summary>
/// <remarks>
///     <para>
///         A single codec serves both directions. Two dispatch tables are built at
///         construction by scanning the supplied assemblies for
///         <see cref="ClientOpcodeAttribute" /> and <see cref="ServerOpcodeAttribute" />.
///         Each attributed type must expose
///         <c>public static T Parse(ReadOnlySpan&lt;byte&gt;)</c>.
///     </para>
///     <para>
///         Wire format:
///         <list type="bullet">
///             <item>
///                 <b>None</b>: <c>[0xAA] [u16-BE len] [opcode] [body]</c> with a trailing
///                 <c>0x00</c> on C->S frames only.
///             </item>
///             <item>
///                 <b>Normal / MD5Key</b>:
///                 <c>[0xAA] [u16-BE len] [opcode] [ordinal] [encrypted body] [footer]</c>.
///                 Footer is 7 bytes on C->S (4 MD5 hash + 3 bRand/sRand) and 3 bytes on
///                 S->C (bRand/sRand only).
///             </item>
///         </list>
///     </para>
///     <para>
///         Encryption method is derived from opcode via
///         <see cref="CryptoState.GetClientEncryptMethod" /> /
///         <see cref="CryptoState.GetServerEncryptMethod" /> - packets do not declare it.
///     </para>
///     <para>
///         For C->S opcodes 0x39 / 0x3A, <see cref="DialogObfuscation" /> is applied to the
///         plaintext body before MD5Key encryption (and removed after decryption).
///     </para>
/// </remarks>
public sealed class PacketCodec
{
    private const int ClientEncryptedFooterLength = 7; // 4 MD5 hash + 3 bRand/sRand
    private const int ServerEncryptedFooterLength = 3; // bRand/sRand only
    private const int OrdinalLength = 1;

    private readonly Dictionary<byte, WireParseFn<IClientPacket>> _clientParsers;
    private readonly Dictionary<byte, WireParseFn<IServerPacket>> _serverParsers;

    /// <summary>
    ///     Constructs a codec that discovers packets in the DALib assembly only.
    /// </summary>
    public PacketCodec()
        : this([typeof(IPacket).Assembly]) { }

    /// <summary>
    ///     Constructs a codec that discovers packets in the supplied assemblies.
    /// </summary>
    /// <param name="packetAssemblies">
    ///     Assemblies to scan for <see cref="ClientOpcodeAttribute" /> and
    ///     <see cref="ServerOpcodeAttribute" />-decorated types. The DALib assembly is
    ///     <em>not</em> implicitly included - pass it explicitly if desired.
    /// </param>
    public PacketCodec(IEnumerable<Assembly> packetAssemblies)
    {
        ArgumentNullException.ThrowIfNull(packetAssemblies);

        var assemblies = packetAssemblies.ToList();

        _clientParsers = PacketDispatchBuilder.Build<ClientOpcodeAttribute, IClientPacket>(
            assemblies,
            attr => attr.Opcode);

        _serverParsers = PacketDispatchBuilder.Build<ServerOpcodeAttribute, IServerPacket>(
            assemblies,
            attr => attr.Opcode);

        // 0x3A DialogUse is multi-variant: its response tail (none / menu / text / consumer-
        // defined) is chosen by a self-describing tag byte. Widen its dispatch to the supplied
        // assemblies so consumer-declared [DialogResponseType] variants parse strongly-typed too.
        DialogUsePacket.WireInto(_clientParsers, assemblies);
    }

    /// <summary>The number of distinct C->S opcodes the codec can parse.</summary>
    public int RegisteredClientOpcodeCount => _clientParsers.Count;

    /// <summary>The number of distinct S->C opcodes the codec can parse.</summary>
    public int RegisteredServerOpcodeCount => _serverParsers.Count;

    /// <summary>Returns true if a C->S parser is registered for the given opcode.</summary>
    public bool IsClientOpcodeRegistered(byte opcode) => _clientParsers.ContainsKey(opcode);

    /// <summary>Returns true if an S->C parser is registered for the given opcode.</summary>
    public bool IsServerOpcodeRegistered(byte opcode) => _serverParsers.ContainsKey(opcode);

    /// <summary>
    ///     Encodes a C->S packet into outbound wire bytes.
    /// </summary>
    /// <param name="packet">The packet to encode.</param>
    /// <param name="crypto">Per-session crypto state. Not used for opcodes mapped to
    ///     <see cref="EncryptMethod.None" />; required for encrypted modes.</param>
    /// <param name="sequence">
    ///     Override the auto-allocated ordinal for testing / protocol probing. Ignored for
    ///     <see cref="EncryptMethod.None" /> packets, which have no ordinal on the wire.
    /// </param>
    /// <param name="slop">
    ///     Optional extra bytes appended after the packet body and before encryption (or
    ///     before the trailing 0x00 for None mode).
    /// </param>
    public ReadOnlyMemory<byte> EncodeClient(
        IClientPacket packet,
        CryptoState crypto,
        byte? sequence = null,
        ReadOnlyMemory<byte> slop = default)
    {
        ArgumentNullException.ThrowIfNull(packet);
        ArgumentNullException.ThrowIfNull(crypto);

        var method = CryptoState.GetClientEncryptMethod(packet.Opcode);

        return EncodeFrame(
            packet, crypto, method, sequence, slop,
            isClient: true,
            applyDialogObfuscation: DialogObfuscation.AppliesTo(packet.Opcode));
    }

    /// <summary>
    ///     Encodes an S->C packet into outbound wire bytes.
    /// </summary>
    public ReadOnlyMemory<byte> EncodeServer(
        IServerPacket packet,
        CryptoState crypto,
        byte? sequence = null,
        ReadOnlyMemory<byte> slop = default)
    {
        ArgumentNullException.ThrowIfNull(packet);
        ArgumentNullException.ThrowIfNull(crypto);

        var method = CryptoState.GetServerEncryptMethod(packet.Opcode);

        return EncodeFrame(
            packet, crypto, method, sequence, slop,
            isClient: false,
            applyDialogObfuscation: false);
    }

    /// <summary>
    ///     Parses a single C->S packet from <paramref name="wireBytes" />. The buffer must
    ///     contain exactly one complete frame and no trailing bytes.
    /// </summary>
    public IClientPacket ParseClientPacket(ReadOnlyMemory<byte> wireBytes, CryptoState crypto)
    {
        if (!TryGetClientPacket(wireBytes, crypto, out var packet, out var consumed))
            throw new InvalidDataException(
                $"Parse: buffer is too short to contain a complete packet ({wireBytes.Length} byte(s)).");

        if (consumed != wireBytes.Length)
            throw new InvalidDataException(
                $"Parse: trailing {wireBytes.Length - consumed} byte(s) after packet at position {consumed}.");

        return packet!;
    }

    /// <summary>
    ///     Parses a single S->C packet from <paramref name="wireBytes" />.
    /// </summary>
    public IServerPacket ParseServerPacket(ReadOnlyMemory<byte> wireBytes, CryptoState crypto)
    {
        if (!TryGetServerPacket(wireBytes, crypto, out var packet, out var consumed))
            throw new InvalidDataException(
                $"Parse: buffer is too short to contain a complete packet ({wireBytes.Length} byte(s)).");

        if (consumed != wireBytes.Length)
            throw new InvalidDataException(
                $"Parse: trailing {wireBytes.Length - consumed} byte(s) after packet at position {consumed}.");

        return packet!;
    }

    /// <summary>
    ///     Attempts to pull a single complete C->S packet from <paramref name="buffer" />.
    ///     Returns false (no exception) if the buffer is too short; throws
    ///     <see cref="InvalidDataException" /> on malformed frames or unknown opcodes.
    /// </summary>
    public bool TryGetClientPacket(
        ReadOnlyMemory<byte> buffer,
        CryptoState crypto,
        out IClientPacket? packet,
        out int bytesConsumed)
    {
        ArgumentNullException.ThrowIfNull(crypto);

        return TryGetFrame(buffer, crypto, _clientParsers, isClient: true,
            out packet, out bytesConsumed);
    }

    /// <summary>
    ///     Attempts to pull a single complete S->C packet from <paramref name="buffer" />.
    /// </summary>
    public bool TryGetServerPacket(
        ReadOnlyMemory<byte> buffer,
        CryptoState crypto,
        out IServerPacket? packet,
        out int bytesConsumed)
    {
        ArgumentNullException.ThrowIfNull(crypto);

        return TryGetFrame(buffer, crypto, _serverParsers, isClient: false,
            out packet, out bytesConsumed);
    }

    private static ReadOnlyMemory<byte> EncodeFrame(
        IPacket packet,
        CryptoState crypto,
        EncryptMethod method,
        byte? sequence,
        ReadOnlyMemory<byte> slop,
        bool isClient,
        bool applyDialogObfuscation)
    {
        var bodyWriter = new PacketWriter();
        packet.WriteBody(bodyWriter);

        if (!slop.IsEmpty)
            bodyWriter.WriteBytes(slop.Span);

        if (method == EncryptMethod.None)
            return EncodeNoneFrame(packet.Opcode, bodyWriter.WrittenSpan, isClient);

        // Encrypted (Normal or MD5Key).
        var plaintextBody = applyDialogObfuscation
            ? DialogObfuscation.Apply(bodyWriter.ToArray())
            : bodyWriter.ToArray();

        // CryptoState.Encrypt{Client,Server}Packet expects [opcode][body...] input.
        var plainPayload = new byte[1 + plaintextBody.Length];
        plainPayload[0] = packet.Opcode;
        Buffer.BlockCopy(plaintextBody, 0, plainPayload, 1, plaintextBody.Length);

        var encrypted = sequence.HasValue
            ? (isClient
                ? crypto.EncryptClientPacket(packet.Opcode, plainPayload, sequence.Value)
                : crypto.EncryptServerPacket(packet.Opcode, plainPayload, sequence.Value))
            : (isClient
                ? crypto.EncryptClientPacket(packet.Opcode, plainPayload)
                : crypto.EncryptServerPacket(packet.Opcode, plainPayload));

        if (encrypted.Length > ushort.MaxValue)
            throw new InvalidOperationException(
                $"Encrypted packet length {encrypted.Length} exceeds the u16 wire-frame limit.");

        var frame = new PacketWriter(WireFrame.HeaderLength + encrypted.Length);
        frame.WriteByte(WireFrame.Marker);
        frame.WriteUInt16((ushort)encrypted.Length);
        frame.WriteBytes(encrypted);

        return frame.ToMemory();
    }

    private static ReadOnlyMemory<byte> EncodeNoneFrame(byte opcode, ReadOnlySpan<byte> body, bool isClient)
    {
        var trailerLength = isClient ? WireFrame.TrailingNullLength : 0;
        var wireBodyLength = WireFrame.OpcodeLength + body.Length + trailerLength;

        if (wireBodyLength > ushort.MaxValue)
            throw new InvalidOperationException(
                $"Encoded packet body length {wireBodyLength} exceeds the u16 wire-frame limit.");

        var frame = new PacketWriter(WireFrame.HeaderLength + wireBodyLength);
        frame.WriteByte(WireFrame.Marker);
        frame.WriteUInt16((ushort)wireBodyLength);
        frame.WriteByte(opcode);
        frame.WriteBytes(body);

        if (isClient)
            frame.WriteByte(WireFrame.TrailingNull);

        return frame.ToMemory();
    }

    private static bool TryGetFrame<TPacket>(
        ReadOnlyMemory<byte> buffer,
        CryptoState crypto,
        Dictionary<byte, WireParseFn<TPacket>> parsers,
        bool isClient,
        out TPacket? packet,
        out int bytesConsumed)
        where TPacket : class, IPacket
    {
        packet = null;
        bytesConsumed = 0;

        if (buffer.Length < WireFrame.HeaderLength)
            return false;

        var span = buffer.Span;

        if (span[0] != WireFrame.Marker)
            throw new InvalidDataException(
                $"Frame marker mismatch: expected 0x{WireFrame.Marker:X2}, got 0x{span[0]:X2}.");

        var wireBodyLength = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(1, 2));
        var totalFrameLength = WireFrame.HeaderLength + wireBodyLength;

        if (buffer.Length < totalFrameLength)
            return false;

        if (wireBodyLength < WireFrame.OpcodeLength)
            throw new InvalidDataException(
                $"Wire body length {wireBodyLength} is too small to contain an opcode.");

        var opcode = span[WireFrame.HeaderLength];
        var method = isClient
            ? CryptoState.GetClientEncryptMethod(opcode)
            : CryptoState.GetServerEncryptMethod(opcode);

        if (!parsers.TryGetValue(opcode, out var parser))
            throw new InvalidDataException(
                $"No registered parser for {TPacket.Direction} opcode 0x{opcode:X2}.");

        var bodyBytes = method == EncryptMethod.None
            ? SliceNoneBody(span, wireBodyLength, isClient)
            : DecryptAndUnpadBody(span, wireBodyLength, opcode, method, crypto, isClient);

        packet = parser(bodyBytes);
        bytesConsumed = totalFrameLength;

        return true;
    }

    private static ReadOnlySpan<byte> SliceNoneBody(
        ReadOnlySpan<byte> frameSpan,
        ushort wireBodyLength,
        bool isClient)
    {
        if (isClient)
        {
            if (wireBodyLength < WireFrame.OpcodeLength + WireFrame.TrailingNullLength)
                throw new InvalidDataException(
                    $"Wire body length {wireBodyLength} is too small to contain an opcode and trailing null.");

            var trailingByteIndex = WireFrame.HeaderLength + wireBodyLength - 1;

            if (frameSpan[trailingByteIndex] != WireFrame.TrailingNull)
                throw new InvalidDataException(
                    $"Expected trailing 0x{WireFrame.TrailingNull:X2} at frame index {trailingByteIndex}, " +
                    $"got 0x{frameSpan[trailingByteIndex]:X2}.");

            var bodyStart = WireFrame.HeaderLength + WireFrame.OpcodeLength;
            var bodyLength = wireBodyLength - WireFrame.OpcodeLength - WireFrame.TrailingNullLength;

            return frameSpan.Slice(bodyStart, bodyLength);
        }
        else
        {
            var bodyStart = WireFrame.HeaderLength + WireFrame.OpcodeLength;
            var bodyLength = wireBodyLength - WireFrame.OpcodeLength;

            return frameSpan.Slice(bodyStart, bodyLength);
        }
    }

    private static byte[] DecryptAndUnpadBody(
        ReadOnlySpan<byte> frameSpan,
        ushort wireBodyLength,
        byte opcode,
        EncryptMethod method,
        CryptoState crypto,
        bool isClient)
    {
        var footerLength = isClient ? ClientEncryptedFooterLength : ServerEncryptedFooterLength;
        var minWireBodyLength = WireFrame.OpcodeLength + OrdinalLength + footerLength;

        if (wireBodyLength < minWireBodyLength)
            throw new InvalidDataException(
                $"Encrypted wire body length {wireBodyLength} is too small to contain " +
                $"opcode + ordinal + {footerLength}-byte footer.");

        var ordinal = frameSpan[WireFrame.HeaderLength + WireFrame.OpcodeLength];
        var dataStart = WireFrame.HeaderLength + WireFrame.OpcodeLength + OrdinalLength;
        var dataLength = wireBodyLength - WireFrame.OpcodeLength - OrdinalLength;
        var encryptedWithFooter = frameSpan.Slice(dataStart, dataLength).ToArray();

        var decrypted = isClient
            ? crypto.DecryptClient(opcode, ordinal, encryptedWithFooter)
            : crypto.DecryptServer(opcode, ordinal, encryptedWithFooter);

        // Inner-plaintext padding: C->S Normal has trailing 0x00 (1 byte);
        // C->S MD5Key has trailing 0x00 + opcode copy (2 bytes); S->C has neither.
        var paddingLength = isClient
            ? (method == EncryptMethod.MD5Key ? 2 : 1)
            : 0;
        var unpaddedLength = decrypted.Length - paddingLength;
        var bodyBytes = new byte[unpaddedLength];
        Buffer.BlockCopy(decrypted, 0, bodyBytes, 0, unpaddedLength);

        // For C->S dialog opcodes (0x39 / 0x3A), reverse the dialog obfuscation that was
        // applied before encryption.
        if (isClient && DialogObfuscation.AppliesTo(opcode))
            bodyBytes = DialogObfuscation.Remove(bodyBytes);

        return bodyBytes;
    }
}
