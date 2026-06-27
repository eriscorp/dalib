using System;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x10 (C->S) - connection handshake sent to a redirect target (Login or World server),
///     presenting the seed/key/name/redirectId received in a prior <see cref="RedirectPacket" />.
///     The body byte-for-byte mirrors the inner portion of S->C 0x03.
/// </summary>
[ClientOpcode(ClientOpcode.ClientJoin)]
public sealed record ClientJoinPacket : ClientPacket
{
    /// <summary>Encryption seed handed over by the prior <see cref="RedirectPacket" />.</summary>
    public required byte EncryptionSeed { get; init; }

    /// <summary>Encryption key handed over by the prior <see cref="RedirectPacket" />.</summary>
    public required byte[] EncryptionKey { get; init; }

    /// <summary>Account / character name handed over by the prior <see cref="RedirectPacket" />.</summary>
    public required string Name { get; init; }

    /// <summary>
    ///     Server-side validation token handed over by the prior <see cref="RedirectPacket" />.
    ///     The target server looks this up in its expected-connections manifest.
    /// </summary>
    public required uint RedirectId { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.ClientJoin;

    /// <summary>
    ///     Constructs the corresponding ClientJoin from a received <see cref="RedirectPacket" />,
    ///     forwarding the redirect's credentials verbatim.
    /// </summary>
    public static ClientJoinPacket FromRedirect(RedirectPacket redirect) => new()
    {
        EncryptionSeed = redirect.EncryptionSeed,
        EncryptionKey = redirect.EncryptionKey,
        Name = redirect.Name,
        RedirectId = redirect.RedirectId,
    };

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        if (EncryptionKey.Length > byte.MaxValue)
            throw new InvalidOperationException(
                $"ClientJoin: encryption key length {EncryptionKey.Length} exceeds wire u8 limit.");

        writer.WriteByte(EncryptionSeed);
        writer.WriteByte((byte)EncryptionKey.Length);
        writer.WriteBytes(EncryptionKey);
        writer.WriteString8(Name);
        writer.WriteUInt32(RedirectId);
    }

    /// <inheritdoc />
    public static ClientJoinPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var seed = reader.ReadByte();
        var keyLength = reader.ReadByte();
        var key = reader.ReadBytes(keyLength).ToArray();
        var name = reader.ReadString8();
        var redirectId = reader.ReadUInt32();

        return new ClientJoinPacket
        {
            EncryptionSeed = seed,
            EncryptionKey = key,
            Name = name,
            RedirectId = redirectId,
        };
    }
}
