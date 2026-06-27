using System;
using System.IO;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x60 (S->C) - the login-screen notification (server message / MOTD). A leading boolean selects the
///     body shape: <c>false</c> carries just a <c>u32</c> checksum of the current notification,
///     <c>true</c> carries the full <c>u16</c>-length-prefixed payload, modeled as a discriminated
///     <see cref="Form" />.
/// </summary>
/// <remarks>
///     This is a cache-validation handshake (compare HTTP <c>ETag</c>/<c>If-None-Match</c>): the checksum
///     form is sent first, and the full payload (<see cref="NotificationDataForm" />) is sent in answer to
///     a C->S 0x4B request only when the checksum does not match the cached notice. The payload is opaque,
///     zlib-compressed bytes; compressing and checksumming the text are the producer's concern and
///     decompression the consumer's.
/// </remarks>
[ServerOpcode(ServerOpcode.LoginNotification)]
public sealed record LoginNotificationPacket : ServerPacket
{
    /// <summary>The notification body - either the checksum probe or the full payload. Never null.</summary>
    public required NotificationForm Form { get; set; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.LoginNotification;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteBoolean(Form.IsFullResponse);
        Form.Write(writer);
    }

    /// <inheritdoc />
    public static LoginNotificationPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var isFullResponse = reader.ReadBoolean();
        NotificationForm form = isFullResponse
            ? NotificationDataForm.ParseBody(ref reader)
            : NotificationChecksumForm.ParseBody(ref reader);

        if (reader.Position != reader.Length)
            throw new InvalidDataException(
                $"LoginNotificationPacket: {reader.Length - reader.Position} trailing byte(s) after the " +
                $"{(isFullResponse ? "full" : "checksum")} body at position {reader.Position}.");

        return new LoginNotificationPacket { Form = form };
    }
}

/// <summary>
///     The body of a <see cref="LoginNotificationPacket" /> (S->C 0x60): either the checksum probe or the
///     full payload, selected by the leading <see cref="IsFullResponse" /> boolean. Sealed variants
///     follow this file.
/// </summary>
public abstract record NotificationForm
{
    /// <summary>The leading boolean: <c>true</c> when this carries the full payload, <c>false</c> for the checksum probe.</summary>
    internal abstract bool IsFullResponse { get; }

    /// <summary>Writes this form's bytes, following the leading boolean.</summary>
    internal abstract void Write(IPacketWriter writer);
}

/// <summary>
///     0x60 body - leading boolean <c>false</c>: <c>[u32 Checksum]</c>. The checksum probe sent on
///     redirect; the full payload (C->S 0x4B) is requested only if this does not match the cached notice.
/// </summary>
public sealed record NotificationChecksumForm : NotificationForm
{
    /// <summary>The CRC32 of the (uncompressed) current notification text. Round-tripped verbatim.</summary>
    public required uint Checksum { get; init; }

    /// <inheritdoc />
    internal override bool IsFullResponse => false;

    /// <inheritdoc />
    internal override void Write(IPacketWriter writer) => writer.WriteUInt32(Checksum);

    internal static NotificationChecksumForm ParseBody(ref PacketReader reader)
        => new() { Checksum = reader.ReadUInt32() };
}

/// <summary>
///     0x60 body - leading boolean <c>true</c>: <c>[u16-BE Length][bytes Data]</c>. The full notification
///     payload, sent in answer to C->S 0x4B. <see cref="Data" /> is the raw on-wire (zlib-compressed)
///     blob; decompression is the consumer's concern.
/// </summary>
/// <remarks>
///     This is the only valid answer to a C->S 0x4B request: answering 0x4B with a
///     <see cref="NotificationChecksumForm" /> instead leaves the login screen stuck re-requesting on every
///     checksum mismatch. The checksum form belongs only on the C->S 0x10 ClientJoin leg.
/// </remarks>
public sealed record NotificationDataForm : NotificationForm
{
    /// <summary>The raw on-wire notification payload (zlib-compressed MOTD / stipulation text).</summary>
    public required byte[] Data { get; init; }

    /// <inheritdoc />
    internal override bool IsFullResponse => true;

    /// <inheritdoc />
    internal override void Write(IPacketWriter writer)
    {
        if (Data.Length > ushort.MaxValue)
            throw new InvalidOperationException(
                $"NotificationDataForm: payload length {Data.Length} exceeds the wire u16 limit ({ushort.MaxValue}).");

        writer.WriteUInt16((ushort)Data.Length);
        writer.WriteBytes(Data);
    }

    internal static NotificationDataForm ParseBody(ref PacketReader reader)
    {
        var length = reader.ReadUInt16();

        return new NotificationDataForm { Data = reader.ReadBytes(length).ToArray() };
    }
}
