namespace DALib.Networking.Wire;

/// <summary>
///     Marker for a packet sent from server to client (S->C).
/// </summary>
/// <remarks>
///     The same opcode can carry an entirely different packet shape in each direction
///     (e.g., 0x05 UserId S->C is unrelated to 0x05 RequestMap C->S). Direction is therefore
///     part of the type, not a runtime flag.
/// </remarks>
public interface IServerPacket : IPacket
{
    static string IPacket.Direction => "server";
}
