namespace DALib.Networking.Wire;

/// <summary>
///     Catalog of client-to-server (C->S) packet opcodes. Values are the on-wire bytes.
/// </summary>
/// <remarks>
///     <para>
///         Direction is part of the identity: the same byte can mean different things in each
///         direction (e.g. <see cref="Version" /> 0x00 C->S vs <see cref="ServerOpcode.CryptoKey" />
///         0x00 S->C; <see cref="RequestMap" /> 0x05 C->S vs <see cref="ServerOpcode.UserAppearance" />
///         0x05 S->C). Keeping C->S and S->C in separate enums makes those collisions explicit.
///     </para>
///     <para>
///         Entries are added as packets are modeled, not speculatively - every member here has a
///         corresponding implemented packet.
///     </para>
/// </remarks>
public enum ClientOpcode : byte
{
    /// <summary>0x00 - client version / fingerprint handshake.</summary>
    Version = 0x00,

    /// <summary>0x02 - begin character creation: reserve a name/password (and email).</summary>
    CreateCharRequest = 0x02,

    /// <summary>0x03 - login credentials submission.</summary>
    Login = 0x03,

    /// <summary>0x04 - finalize character creation: hair style, gender, hair colour for the reserved name.</summary>
    CreateCharFinalize = 0x04,

    /// <summary>0x05 - request the current map's tile rows (CRC-gated).</summary>
    RequestMap = 0x05,

    /// <summary>0x06 - walk one tile in a cardinal direction.</summary>
    Walk = 0x06,

    /// <summary>0x07 - pick up an item or gold from a tile.</summary>
    PickupItem = 0x07,

    /// <summary>0x08 - drop a count of items from a slot onto a tile.</summary>
    DropItem = 0x08,

    /// <summary>0x0B - exit-dialog request / exit confirmation.</summary>
    ClientExit = 0x0B,

    /// <summary>0x0C - ask the server to (re)display a visible object the client is missing.</summary>
    RequestObject = 0x0C,

    /// <summary>0x0D - manage the ignore (whisper-block) list: request / add / remove.</summary>
    Ignore = 0x0D,

    /// <summary>0x0E - public chat (say) or shout.</summary>
    Talk = 0x0E,

    /// <summary>0x0F - cast a spell from a spellbook slot (args vary by spell type).</summary>
    UseSpell = 0x0F,

    /// <summary>0x10 - connection handshake presented to a redirect target.</summary>
    ClientJoin = 0x10,

    /// <summary>0x11 - turn to face a cardinal direction, without moving.</summary>
    Turn = 0x11,

    /// <summary>0x13 - basic "assail" attack in the current facing direction (no body).</summary>
    Attack = 0x13,

    /// <summary>0x18 - request the online-player (world) list; no body.</summary>
    RequestWorldList = 0x18,

    /// <summary>0x19 - directed message: player whisper, or guild/group chat via "!"/"!!" targets.</summary>
    Whisper = 0x19,

    /// <summary>0x1B - toggle a client setting (0 requests the full list).</summary>
    Settings = 0x1B,

    /// <summary>0x1C - use/invoke an item from an inventory slot.</summary>
    UseItem = 0x1C,

    /// <summary>0x1D - play an emote (body animation); body is a single 0-35 index (server adds 9).</summary>
    Emote = 0x1D,

    /// <summary>0x23 - save an item slot's notepad text.</summary>
    SetNotepad = 0x23,

    /// <summary>0x24 - drop an amount of gold onto a tile.</summary>
    DropGold = 0x24,

    /// <summary>0x26 - change the account password (handled by the login server).</summary>
    ChangePassword = 0x26,

    /// <summary>0x29 - drop/give items from a slot onto a creature or player.</summary>
    DropItemOnCreature = 0x29,

    /// <summary>0x2A - drop/give gold onto a creature or player.</summary>
    DropGoldOnCreature = 0x2A,

    /// <summary>0x2D - request the player's own profile pane; no body.</summary>
    RequestProfile = 0x2D,

    /// <summary>0x2E - group request; a Stage byte selects invite/accept/groupbox/remove/recruit-join.</summary>
    GroupRequest = 0x2E,

    /// <summary>0x2F - toggle the "accepting group invitations" flag; no body.</summary>
    GroupToggle = 0x2F,

    /// <summary>0x30 - swap two slots within a panel (inventory/spellbook/skillbook).</summary>
    SwapSlot = 0x30,

    /// <summary>0x31 - confirm a server prompt; carries three state bytes then a length-prefixed payload. 
    Confirm = 0x31,

    /// <summary>0x38 - request a refresh of the surrounding area; no body.</summary>
    Refresh = 0x38,

    /// <summary>0x39 - click an object to start a dialog pursuit (NPC menu, item, reactor, castable).</summary>
    NpcMainMenu = 0x39,

    /// <summary>0x3A - respond within an open dialog (navigation / menu option / text input). Multi-variant; the tail is selected by a self-describing tag byte.</summary>
    DialogUse = 0x3A,

    /// <summary>0x3B - board / mailbox operation; a leading action byte selects the form (list / view / post / delete / mail / highlight). Multi-variant.</summary>
    BoardRequest = 0x3B,

    /// <summary>0x3E - use a skill from a skill-book slot.</summary>
    UseSkill = 0x3E,

    /// <summary>0x3F - click a node on the world map; echoes the clicked point's four opaque handles.</summary>
    MapPointClick = 0x3F,

    /// <summary>0x42 - report a client exception/crash: uploads the (zlib-compressed) crash log or error string. Client diagnostics infrastructure, not a game action.</summary>
    Exception = 0x42,

    /// <summary>0x43 - click an entity (by serial) or a tile (by x,y).</summary>
    Click = 0x43,

    /// <summary>0x44 - unequip an item by clicking it on the equipment screen.</summary>
    Unequip = 0x44,

    /// <summary>0x45 - reply to the server's byte-heartbeat challenge (two bytes, echoed reversed).</summary>
    ByteHeartbeat = 0x45,

    /// <summary>0x46 - request the details of the current group; no body.</summary>
    GroupView = 0x46,

    /// <summary>0x47 - spend a level-up point to raise a primary stat (single-bit selector).</summary>
    StatPoint = 0x47,

    /// <summary>0x4A - drive a player-to-player trade; a leading stage byte selects the step (start / add item / add stackable / set gold / cancel / accept). Multi-variant.</summary>
    Exchange = 0x4A,

    /// <summary>0x4B - request the full login notice after a checksum mismatch; no body. Answered by S->C 0x60.</summary>
    RequestNotification = 0x4B,

    /// <summary>0x4D - opens a spell cast; carries the spell's chant-line count.</summary>
    BeginCasting = 0x4D,

    /// <summary>0x4E - one line of a spell's chant, sent while casting.</summary>
    CastLine = 0x4E,

    /// <summary>0x4F - upload the player's own profile (portrait + legend text).</summary>
    SetProfile = 0x4F,

    /// <summary>0x54 - drive an open player-run shop (the employee/consignment window); echoes a 0x01 gate + the shop id then an action byte (withdraw / add / update / remove / close / opened). The C->S pair for S->C 0x4F PlayerShop. Multi-variant.</summary>
    PlayerShopAction = 0x54,

    /// <summary>0x55 - drive a manufacture dialog; echoes the window's type+slot then a subtype byte (request page / make). Multi-variant.</summary>
    Manufacture = 0x55,

    /// <summary>0x57 - lobby request for the server table.</summary>
    ServerTable = 0x57,

    /// <summary>0x68 - request the homepage/account URL for the main-menu link; no body. Answered by S->C 0x66 (subtype 3).</summary>
    RequestHomepage = 0x68,

    /// <summary>0x6A - drive a mini-game; a leading action byte selects the sub-action (5 open / 6 submit / 7 sync / 8 result). Multi-variant.</summary>
    MiniGame = 0x6A,

    /// <summary>0x6C - drive the cash-shop / item-mall window; a leading subtype byte selects open / purchase / close. Multi-variant.</summary>
    CashShop = 0x6C,

    /// <summary>0x71 - keep-alive "still here" signal; no body. Sent on a timer when the client's activity flag is set.</summary>
    SendAlive = 0x71,

    /// <summary>0x73 - drive the in-client browser window; a leading sub-action byte selects the form (0 opened / 3 navigate). Multi-variant.</summary>
    BrowserAction = 0x73,

    /// <summary>0x75 - reply to the server's tick-heartbeat challenge (server + client ticks).</summary>
    TickHeartbeat = 0x75,

    /// <summary>0x79 - set the player's social/group status (0-7).</summary>
    Status = 0x79,

    /// <summary>0x7A - request the player's spouse/family name; no body. Answered by S->C 0x6D FamilyName.</summary>
    RequestLoverName = 0x7A,

    /// <summary>0x7B - request metafile data: all checksums, or one metafile by name.</summary>
    RequestMetafile = 0x7B
}
