namespace DALib.Networking.Packets.Client;

/// <summary>
///     The action byte leading the C->S 0x0D <see cref="IgnorePacket" />: whether the request is for
///     the ignore list or adds/removes a name from it. Values start at 1; there is no defined 0.
/// </summary>
public enum IgnoreType : byte
{
    /// <summary>Request the current ignore (whisper-block) list. No target name follows.</summary>
    Request = 1,

    /// <summary>Add a user to the ignore list. A target name follows.</summary>
    AddUser = 2,

    /// <summary>Remove a user from the ignore list. A target name follows.</summary>
    RemoveUser = 3
}
