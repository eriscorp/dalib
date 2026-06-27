namespace DALib.Networking.Packets.Server;

/// <summary>
///     How a spell is invoked from the spell pane - the third byte of S->C 0x17
///     <see cref="AddSpellPacket" />. Drives the cast UI: target selection, a free-text prompt,
///     a fixed-width numeric prompt, or no input at all.
/// </summary>
public enum SpellUseType : byte
{
    /// <summary>The spell cannot be invoked from the pane (passive).</summary>
    None = 0,

    /// <summary>Casting prompts for free-form text input.</summary>
    Prompt = 1,

    /// <summary>Casting prompts for a target selection.</summary>
    Target = 2,

    /// <summary>Casting prompts for up to four digits.</summary>
    FourDigitPrompt = 3,

    /// <summary>Casting prompts for up to three digits.</summary>
    ThreeDigitPrompt = 4,

    /// <summary>Casting requires no input (fires immediately).</summary>
    NoTarget = 5,

    /// <summary>Casting prompts for up to two digits.</summary>
    TwoDigitPrompt = 6,

    /// <summary>Casting prompts for a single digit.</summary>
    OneDigitPrompt = 7
}
