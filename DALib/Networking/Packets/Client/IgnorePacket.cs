using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x0D (C->S) - manage the player's ignore (whisper-block) list: request the current list, or
///     add/remove a named user. Body: <c>[u8 IgnoreType]</c> followed, for
///     <see cref="IgnoreType.AddUser" /> and <see cref="IgnoreType.RemoveUser" />, by
///     <c>[string8 TargetName]</c>. <see cref="IgnoreType.Request" /> carries no name. Use the
///     static factories.
/// </summary>
[ClientOpcode(ClientOpcode.Ignore)]
public sealed record IgnorePacket : ClientPacket
{
    /// <summary>Whether this requests the list or adds/removes a name.</summary>
    public required IgnoreType IgnoreType { get; init; }

    /// <summary>
    ///     The user to add or remove; set (and written) only when <see cref="IgnoreType" /> is
    ///     <see cref="IgnoreType.AddUser" /> or <see cref="IgnoreType.RemoveUser" />.
    /// </summary>
    public string? TargetName { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.Ignore;

    /// <summary>Builds a request for the current ignore list (no name).</summary>
    public static IgnorePacket Request() => new() { IgnoreType = IgnoreType.Request };

    /// <summary>Builds an "add to ignore list" request for <paramref name="targetName" />.</summary>
    public static IgnorePacket AddUser(string targetName) =>
        new() { IgnoreType = IgnoreType.AddUser, TargetName = targetName };

    /// <summary>Builds a "remove from ignore list" request for <paramref name="targetName" />.</summary>
    public static IgnorePacket RemoveUser(string targetName) =>
        new() { IgnoreType = IgnoreType.RemoveUser, TargetName = targetName };

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte((byte)IgnoreType);

        if (IgnoreType == IgnoreType.Request)
            return;

        if (TargetName is null)
            throw new InvalidOperationException(
                $"{nameof(IgnorePacket)} with {nameof(IgnoreType)}={IgnoreType} requires a non-null " +
                $"{nameof(TargetName)}.");

        writer.WriteString8(TargetName);
    }

    /// <inheritdoc />
    public static IgnorePacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        var ignoreType = (IgnoreType)reader.ReadByte();

        return new IgnorePacket
        {
            IgnoreType = ignoreType,
            TargetName = ignoreType == IgnoreType.Request ? null : reader.ReadString8(),
        };
    }
}
