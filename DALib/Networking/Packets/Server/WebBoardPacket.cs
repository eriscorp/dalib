using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x62 (S->C) - a web-board packet. A leading <c>[u8 Type]</c> selects the form: <c>Type == 3</c>
///     is the URL form (<c>[string8 Url][string8 Trailing]</c>); any other type is the general form
///     (<c>[string8 First][string8 Second][string8 Trailing]</c>). The <see cref="Trailing" /> string
///     is always read last.
/// </summary>
/// <remarks>
///     Modeled for protocol completeness; not emitted by typical servers. The two forms reuse
///     <see cref="Trailing" /> differently: <see cref="Url" /> is present iff <see cref="Type" /> is 3;
///     <see cref="First" /> / <see cref="Second" /> are present otherwise.
/// </remarks>
[ServerOpcode(ServerOpcode.WebBoard)]
public sealed record WebBoardPacket : ServerPacket
{
    /// <summary>The discriminator byte selecting the URL form (<see cref="UrlForm" />) versus the general
    ///     form.</summary>
    public const byte UrlForm = 0x03;

    /// <summary>The leading discriminator byte. <c>3</c> selects the URL form; any other value selects the
    ///     general form.</summary>
    public required byte Type { get; init; }

    /// <summary>The URL string, present iff <see cref="Type" /> is 3; <see langword="null" /> otherwise.
    ///     Required to be non-null on the URL form.</summary>
    public string? Url { get; init; }

    /// <summary>The first string of the general form, present iff <see cref="Type" /> is not 3;
    ///     <see langword="null" /> on the URL form. Required to be non-null on the general form.</summary>
    public string? First { get; init; }

    /// <summary>The second string of the general form, present iff <see cref="Type" /> is not 3;
    ///     <see langword="null" /> on the URL form. Required to be non-null on the general form.</summary>
    public string? Second { get; init; }

    /// <summary>The trailing string, always read last on both forms.</summary>
    public required string Trailing { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.WebBoard;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(Type);

        if (Type == UrlForm)
        {
            if (Url is null)
                throw new InvalidOperationException("WebBoardPacket: Url is required when Type == 3 (the URL form).");

            writer.WriteString8(Url);
        }
        else
        {
            if (First is null || Second is null)
                throw new InvalidOperationException(
                    "WebBoardPacket: First and Second are required when Type != 3 (the general form).");

            writer.WriteString8(First);
            writer.WriteString8(Second);
        }

        writer.WriteString8(Trailing);
    }

    /// <inheritdoc />
    public static WebBoardPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var type = reader.ReadByte();

        if (type == UrlForm)
        {
            var url = reader.ReadString8();

            return new WebBoardPacket
            {
                Type = type,
                Url = url,
                Trailing = reader.ReadString8(),
            };
        }

        var first = reader.ReadString8();
        var second = reader.ReadString8();

        return new WebBoardPacket
        {
            Type = type,
            First = first,
            Second = second,
            Trailing = reader.ReadString8(),
        };
    }
}
