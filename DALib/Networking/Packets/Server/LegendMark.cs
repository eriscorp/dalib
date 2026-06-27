using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     One entry in a character's legend, as carried by <see cref="SelfProfilePacket" />.
/// </summary>
public sealed record LegendMark
{
    /// <summary>Sprite icon index.</summary>
    public byte Icon { get; set; }

    /// <summary>Display color (palette index).</summary>
    public byte Color { get; set; }

    /// <summary>Date/time prefix prepended to the text (e.g. <c>"Deoch 5"</c>).</summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>The legend text body.</summary>
    public string Text { get; set; } = string.Empty;

    internal void Write(IPacketWriter writer)
    {
        writer.WriteByte(Icon);
        writer.WriteByte(Color);
        writer.WriteString8(Prefix);
        writer.WriteString8(Text);
    }

    internal static LegendMark Parse(ref PacketReader reader) => new()
    {
        Icon = reader.ReadByte(),
        Color = reader.ReadByte(),
        Prefix = reader.ReadString8(),
        Text = reader.ReadString8(),
    };
}
