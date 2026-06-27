namespace DALib.Networking.Wire;

/// <summary>
///     Marker for a packet sent from client to server (C->S).
/// </summary>
/// <remarks>
///     The same opcode can carry an entirely different packet shape in each direction
///     (e.g., 0x05 RequestMap C->S is unrelated to 0x05 UserId S->C). Direction is therefore
///     part of the type, not a runtime flag.
/// </remarks>
public interface IClientPacket : IPacket
{
    static string IPacket.Direction => "client";
}
