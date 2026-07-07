using System;
using System.IO;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x66 (S->C) - delivers a URL. A leading subtype byte selects the body shape: <c>0x03</c>
///     carries a single <c>string8</c> URL (the homepage / account endpoint); <c>0x01</c>/<c>0x02</c>
///     carry a URL alert form built from two <c>string16</c>s (a target URL and a display message).
///     Modeled as a discriminated <see cref="Form" /> in the same style as 0x2F
///     <see cref="NpcMenuPacket" />.
/// </summary>
/// <remarks>
///     The string width differs by subtype: subtype 3 reads its URL with the <c>string8</c> (u8)
///     length reader, subtypes 1/2 read two <c>string16</c> (u16-BE) lengths. The forms are kept as
///     distinct <see cref="UrlForm" /> variants so the wire width can never be mismatched.
/// </remarks>
[ServerOpcode(ServerOpcode.Url)]
public sealed record UrlPacket : ServerPacket
{
    /// <summary>The URL body. Its <see cref="UrlForm.Subtype" /> selects the wire shape. Never null.</summary>
    public required UrlForm Form { get; set; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.Url;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(Form.Subtype);
        Form.Write(writer);
    }

    /// <inheritdoc />
    public static UrlPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var subtype = reader.ReadByte();
        var form = UrlForm.Parse(subtype, ref reader);

        //DOOMVAS encryption appends an inner-pad byte (0x00, or 0x00+opcode for MD5Key) before the rand
        //footer; DecryptServer strips only the footer, so a trailing pad may remain. Tolerate it rather
        //than reject the packet (see DecryptServer inner-pad strip, the proper fix).

        return new UrlPacket { Form = form };
    }
}

/// <summary>
///     The body of a <see cref="UrlPacket" /> (S->C 0x66): one URL form, selected by the leading
///     <see cref="Subtype" /> byte. Sealed variants follow this file.
/// </summary>
public abstract record UrlForm
{
    /// <summary>The leading discriminator byte this form is written with.</summary>
    internal abstract byte Subtype { get; }

    /// <summary>Writes this form's bytes, following the subtype byte.</summary>
    internal abstract void Write(IPacketWriter writer);

    /// <summary>Reads the form matching <paramref name="subtype" /> from the position after the subtype byte.</summary>
    internal static UrlForm Parse(byte subtype, ref PacketReader reader) => subtype switch
    {
        SetUrlForm.SubtypeSetUrl => SetUrlForm.ParseBody(ref reader),
        UrlAlertForm.SubtypeRedirectClose or UrlAlertForm.SubtypeRedirect => UrlAlertForm.ParseBody(subtype, ref reader),
        _ => throw new InvalidDataException($"UrlPacket: unknown subtype 0x{subtype:X2}."),
    };
}

/// <summary>
///     0x66 body - subtype <c>0x03</c>: <c>[string8 Url]</c>. Carries the homepage / account URL (the
///     "set" form).
/// </summary>
public sealed record SetUrlForm : UrlForm
{
    /// <summary>The subtype byte selecting the single-<c>string8</c>-URL "set" form.</summary>
    public const byte SubtypeSetUrl = 0x03;

    /// <summary>The homepage / account URL.</summary>
    public required string Url { get; init; }

    /// <inheritdoc />
    internal override byte Subtype => SubtypeSetUrl;

    /// <inheritdoc />
    internal override void Write(IPacketWriter writer) => writer.WriteString8(Url);

    internal static SetUrlForm ParseBody(ref PacketReader reader) => new() { Url = reader.ReadString8() };
}

/// <summary>
///     0x66 body - subtypes <c>0x01</c>/<c>0x02</c>: <c>[string16 Url][string16 Message]</c>. An alert
///     form carrying a target URL and a display message. The subtype byte carries the only other
///     difference - see <see cref="CloseClient" />. Modeled for protocol completeness; not emitted by
///     typical servers.
/// </summary>
public sealed record UrlAlertForm : UrlForm
{
    /// <summary>Subtype <c>0x01</c>: the redirect-and-close form.</summary>
    public const byte SubtypeRedirectClose = 0x01;

    /// <summary>Subtype <c>0x02</c>: the redirect form.</summary>
    public const byte SubtypeRedirect = 0x02;

    /// <summary>The target URL.</summary>
    public required string Url { get; init; }

    /// <summary>The message shown in the alert pane.</summary>
    public required string Message { get; init; }

    /// <summary>
    ///     Selects the subtype: <c>true</c> is <see cref="SubtypeRedirectClose" /> (0x01),
    ///     <c>false</c> is <see cref="SubtypeRedirect" /> (0x02). The two forms are otherwise
    ///     identical on the wire.
    /// </summary>
    public bool CloseClient { get; init; }

    /// <inheritdoc />
    internal override byte Subtype => CloseClient ? SubtypeRedirectClose : SubtypeRedirect;

    /// <inheritdoc />
    internal override void Write(IPacketWriter writer)
    {
        writer.WriteString16(Url);
        writer.WriteString16(Message);
    }

    internal static UrlAlertForm ParseBody(byte subtype, ref PacketReader reader)
    {
        var url = reader.ReadString16();
        var message = reader.ReadString16();

        return new UrlAlertForm { Url = url, Message = message, CloseClient = subtype == SubtypeRedirectClose };
    }
}
