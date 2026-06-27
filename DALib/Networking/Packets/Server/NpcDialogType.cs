namespace DALib.Networking.Packets.Server;

/// <summary>
///     The leading discriminator byte of an S->C 0x30 <see cref="NpcDialogPacket" />. Selects the dialog
///     widget, whether the prefix carries a text prompt, and which body shape follows.
/// </summary>
public enum NpcDialogType : byte
{
    /// <summary>0 - a plain text dialog: prefix + text prompt, no body.</summary>
    Normal = 0,

    /// <summary>2 - an options menu with a text prompt.</summary>
    Options = 2,

    /// <summary>3 - an options menu with no text prompt. Modeled for completeness; not emitted by typical servers.</summary>
    SimpleOptions = 3,

    /// <summary>4 - a text-entry prompt with a text prompt.</summary>
    TextInput = 4,

    /// <summary>5 - a text-entry prompt with no text prompt.</summary>
    SimpleTextInput = 5,

    /// <summary>6 - an options menu rendered with the source portrait/face.</summary>
    OptionsWithFace = 6,

    /// <summary>9 - a protected (account-id) text-entry input.</summary>
    ProtectedTextInput = 9,

    /// <summary>10 - dismiss any open dialog. A type-only packet: no prefix follows this byte.</summary>
    Close = 10,
}
