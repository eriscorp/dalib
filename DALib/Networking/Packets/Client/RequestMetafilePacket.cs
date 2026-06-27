using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x7B (C->S) - request metafile data: either the checksums of <em>all</em> metafiles, or the full
///     data for a single metafile by name. The body is <c>[u8 All]</c> followed, only when
///     <see cref="All" /> is <c>false</c>, by <c>[string8 Name]</c>.
/// </summary>
/// <remarks>
///     The leading byte is a request-type discriminator. Non-zero (<see cref="All" /> = <c>true</c>)
///     requests the all-checksums listing and no name follows; zero (<see cref="All" /> = <c>false</c>)
///     is followed by a <c>[string8 Name]</c> naming the single metafile to fetch. Use the static
///     factories.
/// </remarks>
[ClientOpcode(ClientOpcode.RequestMetafile)]
public sealed record RequestMetafilePacket : ClientPacket
{
    /// <summary>
    ///     <c>true</c> to request the checksums of all metafiles (no <see cref="Name" />);
    ///     <c>false</c> to request a single metafile named by <see cref="Name" />.
    /// </summary>
    public required bool All { get; init; }

    /// <summary>
    ///     The metafile name to fetch; set (and written) only when <see cref="All" /> is
    ///     <c>false</c>.
    /// </summary>
    public string? Name { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.RequestMetafile;

    /// <summary>Builds an all-checksums request (no name).</summary>
    public static RequestMetafilePacket AllCheckSums() => new() { All = true };

    /// <summary>Builds a request for a single metafile by <paramref name="name" />.</summary>
    public static RequestMetafilePacket ForName(string name) => new() { All = false, Name = name };

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte((byte)(All ? 1 : 0));

        if (All)
            return;

        if (Name is null)
            throw new InvalidOperationException(
                $"{nameof(RequestMetafilePacket)} with {nameof(All)}=false requires a non-null {nameof(Name)}.");

        writer.WriteString8(Name);
    }

    /// <inheritdoc />
    public static RequestMetafilePacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var all = reader.ReadBoolean();

        return new RequestMetafilePacket
        {
            All = all,
            Name = all ? null : reader.ReadString8(),
        };
    }
}
