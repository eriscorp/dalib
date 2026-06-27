using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x45 (S->C). A leading <c>[u8 Flag]</c> selects the form: a non-zero flag is the bare form
///     (<c>[u8 Flag]</c> only), while <c>Flag == 0</c> is the content form, which adds
///     <c>[u8 ContentByte][bytes...]</c>.
/// </summary>
/// <remarks>
///     Modeled for protocol completeness; not emitted by typical servers. <see cref="Flag" /> is a
///     multi-value discriminator (0 = item-list content form, 2 = panel action, other values bare),
///     not a simple present/absent toggle; only <see cref="Flag" /> == 0 carries
///     <see cref="ContentByte" /> and <see cref="Content" />.
/// </remarks>
[ServerOpcode(ServerOpcode.ItemShop)]
public sealed record ItemShopPacket : ServerPacket
{
    /// <summary>The leading discriminator byte. Non-zero selects the bare form; <c>0</c> selects the content
    ///     form that carries <see cref="ContentByte" /> and <see cref="Content" />.</summary>
    public required byte Flag { get; init; }

    /// <summary>The byte present only on the content form (<see cref="Flag" /> == 0);
    ///     <see langword="null" /> on the bare form. Required to be non-null when <see cref="Flag" /> is 0.</summary>
    public byte? ContentByte { get; init; }

    /// <summary>The variable rest-of-packet tail present only on the content form
    ///     (<see cref="Flag" /> == 0); <see langword="null" /> on the bare form. Required to be non-null
    ///     when <see cref="Flag" /> is 0.</summary>
    public byte[]? Content { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.ItemShop;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(Flag);

        if (Flag != 0)
            return;

        if (ContentByte is null || Content is null)
            throw new InvalidOperationException(
                "ItemShopPacket: ContentByte and Content are required when Flag == 0 (the content form).");

        writer.WriteByte(ContentByte.Value);
        writer.WriteBytes(Content);
    }

    /// <inheritdoc />
    public static ItemShopPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var flag = reader.ReadByte();

        if (flag != 0)
            return new ItemShopPacket { Flag = flag };

        return new ItemShopPacket
        {
            Flag = flag,
            ContentByte = reader.ReadByte(),
            Content = reader.Remaining.ToArray(),
        };
    }
}
