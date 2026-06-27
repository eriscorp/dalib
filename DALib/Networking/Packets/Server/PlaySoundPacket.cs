using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Server;

/// <summary>
///     0x19 (S->C) - play a sound effect, or switch the background music track. The body is one of two
///     shapes selected by the first byte: <c>[u8 Sound]</c> for a sound effect, or
///     <c>[u8 0xFF][u8 MusicTrack]</c> to change music.
/// </summary>
/// <remarks>
///     The music form is exactly two bytes; <see cref="WriteBody" /> emits two bytes for it. For a plain
///     sound effect, <see cref="Sound" /> is the effect index and <see cref="MusicTrack" /> is null. To
///     change music, set <see cref="Sound" /> to <see cref="MusicMarker" /> (0xFF) and
///     <see cref="MusicTrack" /> to the track number; <see cref="IsMusic" /> reflects this.
/// </remarks>
[ServerOpcode(ServerOpcode.PlaySound)]
public sealed record PlaySoundPacket : ServerPacket
{
    /// <summary>The first-byte sentinel that marks the music form (a track number follows).</summary>
    public const byte MusicMarker = 0xFF;

    /// <summary>The sound-effect index to play, or <see cref="MusicMarker" /> (0xFF) to change music.</summary>
    public required byte Sound { get; init; }

    /// <summary>The music track to switch to; non-null only when <see cref="Sound" /> is <see cref="MusicMarker" />.</summary>
    public byte? MusicTrack { get; init; }

    /// <summary>True when this packet changes the music track rather than playing a sound effect.</summary>
    public bool IsMusic => Sound == MusicMarker;

    /// <inheritdoc />
    public override byte Opcode => (byte)ServerOpcode.PlaySound;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(Sound);

        if (Sound == MusicMarker)
            writer.WriteByte(MusicTrack ?? 0);
    }

    /// <inheritdoc />
    public static PlaySoundPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var sound = reader.ReadByte();
        byte? musicTrack = sound == MusicMarker ? reader.ReadByte() : null;

        return new PlaySoundPacket
        {
            Sound = sound,
            MusicTrack = musicTrack,
        };
    }
}
