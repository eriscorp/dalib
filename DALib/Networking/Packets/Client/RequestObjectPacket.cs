using System;
using DALib.Networking.Wire;

namespace DALib.Networking.Packets.Client;

/// <summary>
///     0x0C (C->S) - ask the server to (re)display a visible object, identified by world object id.
///     Sent when an object is detected with no local copy (a draw desync); the server looks the id up
///     and re-runs its area-of-interest entry to redraw the object.
/// </summary>
/// <remarks>
///     The body is a single <c>[u32 BE ObjectId]</c>: a request to display a known object (nothing is
///     placed on the ground). The name here follows the observed behavior.
/// </remarks>
[ClientOpcode(ClientOpcode.RequestObject)]
public sealed record RequestObjectPacket : ClientPacket
{
    /// <summary>World object id to (re)display.</summary>
    public required uint ObjectId { get; init; }

    /// <inheritdoc />
    public override byte Opcode => (byte)ClientOpcode.RequestObject;

    /// <inheritdoc />
    public override void WriteBody(IPacketWriter writer) => writer.WriteUInt32(ObjectId);

    /// <inheritdoc />
    public static RequestObjectPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        return new RequestObjectPacket
        {
            ObjectId = reader.ReadUInt32(),
        };
    }
}
