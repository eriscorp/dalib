using System;
using System.IO;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x73 (C->S) - drive the in-client browser window (the embedded web/homepage pane). The body opens
///     with a <see cref="BrowserActionType" /> sub-action byte that selects the form and any tail. The
///     concrete forms are the sealed records deriving from this base
///     (<see cref="BrowserOpenedPacket" />, <see cref="BrowserNavigatePacket" />).
/// </summary>
/// <remarks>
///     Binary-verified send (the sub-action senders sit under the retail <c>BrowserDialogPane</c>); not
///     emitted by Hybrasyl or Chaos. Uses Normal encryption. Modeled for wire completeness: the structure of
///     each observed form is pinned, but the field semantics are inferred.
/// </remarks>
[ClientOpcode(ClientOpcode.BrowserAction)]
public abstract record BrowserActionPacket : ClientPacket
{
    /// <summary>The sub-action byte that selects this variant's form.</summary>
    public abstract BrowserActionType ActionType { get; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.BrowserAction;

    /// <summary>Writes the leading <c>[u8 sub-action]</c>. Variants call this, then append their tail.</summary>
    protected void WritePrefix(IPacketWriter writer) => writer.WriteByte((byte)ActionType);

    /// <summary>
    ///     Parses a 0x73 body, dispatching on the leading sub-action byte to the matching variant. This is
    ///     the standalone entry and what <see cref="ClientOpcodeAttribute" /> dispatch binds.
    /// </summary>
    public static BrowserActionPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);
        var type = (BrowserActionType)reader.ReadByte();

        return type switch
        {
            BrowserActionType.Opened => new BrowserOpenedPacket(),
            BrowserActionType.Navigate => new BrowserNavigatePacket
                { Index = reader.ReadByte(), Arg = reader.ReadByte() },
            _ => throw new InvalidDataException(
                $"0x73 BrowserAction: unknown sub-action 0x{(byte)type:X2}.")
        };
    }
}

/// <summary>
///     0x73 sub-action 0 - the in-client browser window opened. Prefix only.
/// </summary>
public sealed record BrowserOpenedPacket : BrowserActionPacket
{
    /// <inheritdoc />
    public override BrowserActionType ActionType => BrowserActionType.Opened;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => WritePrefix(writer);
}

/// <summary>
///     0x73 sub-action 3 - a browser navigation/page action. Tail <c>[u8 Index][u8 Arg]</c>.
/// </summary>
public sealed record BrowserNavigatePacket : BrowserActionPacket
{
    /// <summary>A page/entry index (role inferred; the client sources it from the pane's current entry).</summary>
    public required byte Index { get; init; }

    /// <summary>A second navigation byte (role inferred).</summary>
    public required byte Arg { get; init; }

    /// <inheritdoc />
    public override BrowserActionType ActionType => BrowserActionType.Navigate;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer)
    {
        WritePrefix(writer);
        writer.WriteByte(Index);
        writer.WriteByte(Arg);
    }
}
