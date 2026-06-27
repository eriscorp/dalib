using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x1F (S->C) - change the ambient weather effect. The body is <c>[u8 WeatherType]</c>; the single
///     byte selects the effect (a trailing zero byte, if present, is not read). A "map change complete"
///     style signal is just this packet with <see cref="WeatherType" /> 0.
/// </summary>
[ServerOpcode(ServerOpcode.ChangeWeather)]
public sealed record ChangeWeatherPacket : ServerPacket
{
    /// <summary>The weather effect to apply.</summary>
    public required byte WeatherType { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.ChangeWeather;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => writer.WriteByte(WeatherType);

    /// <inheritdoc />
    public static ChangeWeatherPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var weatherType = reader.ReadByte();

        return new ChangeWeatherPacket
        {
            WeatherType = weatherType,
        };
    }
}
