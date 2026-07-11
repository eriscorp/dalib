namespace DALib.Networking.Wire;

/// <summary>
///     Catalog of server-to-client (S->C) packet opcodes. Values are the on-wire bytes.
/// </summary>
/// <remarks>
///     <para>
///         Direction is part of the identity: the same byte can mean different things in each
///         direction (e.g. <see cref="CryptoKey" /> 0x00 S->C vs <see cref="ClientOpcode.Version" />
///         0x00 C->S; <see cref="Redirect" /> 0x03 S->C vs <see cref="ClientOpcode.Login" /> 0x03 C->S).
///         Keeping S->C and C->S in separate enums makes those collisions explicit.
///     </para>
///     <para>
///         Entries are added as packets are modeled, not speculatively - every member here has a
///         corresponding implemented packet.
///     </para>
/// </remarks>
public enum ServerOpcode : byte
{
    /// <summary>0x00 - encryption key / lobby sub-dispatched control packet.</summary>
    CryptoKey = 0x00,

    /// <summary>0x02 - login result message / dialog.</summary>
    LoginMessage = 0x02,

    /// <summary>0x03 - redirect to another server (lobby -> login -> world).</summary>
    Redirect = 0x03,

    /// <summary>0x04 - snap the player to a map coordinate.</summary>
    Location = 0x04,

    /// <summary>0x05 - a user's appearance snapshot.</summary>
    UserAppearance = 0x05,

    /// <summary>
    ///     0x06 - a runtime map-tile patch: [u8 startX][u8 startY][u8 width][u8 height] then
    ///     width x height tile cells (3 BE u16 each). Modeled for protocol completeness;
    ///     not emitted by typical servers.
    /// </summary>
    MapEdit = 0x06,

    /// <summary>0x07 - add objects to the visible map.</summary>
    DrawObjects = 0x07,

    /// <summary>0x08 - player attribute / stat snapshot.</summary>
    Attributes = 0x08,

    /// <summary>0x0A - a system/UI message: chat-log line, action-bar text, or pop-up window.</summary>
    SystemMessage = 0x0A,

    /// <summary>0x0B - confirms the player's own one-tile walk (answers C->S 0x06 Walk).</summary>
    ConfirmWalk = 0x0B,

    /// <summary>0x0C - another creature/user walked one tile within view.</summary>
    CreatureWalk = 0x0C,

    /// <summary>0x0D - a public spoken message (say/shout/chant) broadcast to nearby clients.</summary>
    PublicMessage = 0x0D,

    /// <summary>0x0E - remove an object from the visible map by serial.</summary>
    RemoveObject = 0x0E,

    /// <summary>0x0F - place an item in an inventory-pane slot.</summary>
    AddItem = 0x0F,

    /// <summary>0x10 - clear an inventory-pane slot.</summary>
    RemoveItem = 0x10,

    /// <summary>0x11 - a creature/user turned in place to face a new direction.</summary>
    CreatureTurn = 0x11,

    /// <summary>0x13 - update a creature/user's floating health bar (and optional hit sound).</summary>
    HealthBar = 0x13,

    /// <summary>0x15 - map header (id, dimensions, weather flags, checksum, name).</summary>
    MapInfo = 0x15,

    /// <summary>0x17 - place a spell in a spell-pane slot.</summary>
    AddSpell = 0x17,

    /// <summary>0x18 - clear a spell-pane slot.</summary>
    RemoveSpell = 0x18,

    /// <summary>0x19 - play a sound effect, or (0xFF marker) switch the background music track.</summary>
    PlaySound = 0x19,

    /// <summary>0x1A - play a body/motion animation on a creature or user.</summary>
    PlayerAnimation = 0x1A,

    /// <summary>0x1B - open an editable paper/sign the player can write on (the writable signpost).</summary>
    EditablePaper = 0x1B,

    /// <summary>0x1F - change the ambient weather effect.</summary>
    ChangeWeather = 0x1F,

    /// <summary>0x20 - set the ambient map light/darkness level.</summary>
    LightLevel = 0x20,

    /// <summary>
    ///     0x21 - self-save acknowledgment; payload-free. Modeled for protocol completeness;
    ///     not emitted by typical servers.
    /// </summary>
    SelfSave = 0x21,

    /// <summary>0x22 - a payload-free "refresh the view" signal that prompts a redraw after a state change.</summary>
    Refresh = 0x22,

    /// <summary>0x29 - play a spell/particle effect, attached to a target or over a map tile.</summary>
    SpellAnimation = 0x29,

    /// <summary>0x2C - place a skill in a skill-pane slot.</summary>
    AddSkill = 0x2C,

    /// <summary>0x2D - clear a skill-pane slot.</summary>
    RemoveSkill = 0x2D,

    /// <summary>0x2E - open the world-map (field-map) screen with clickable warp nodes (answered by C->S 0x3F).</summary>
    WorldMap = 0x2E,

    /// <summary>0x2F - an NPC/merchant menu the server displays (answered by C->S 0x39).</summary>
    NpcMenu = 0x2F,

    /// <summary>
    ///     0x31 - drive the bulletin-board / mailbox UI (list, message index, single post, or
    ///     action result; answered by C->S 0x3B).
    /// </summary>
    Board = 0x31,

    /// <summary>0x32 - a batch of door-tile state updates (open/closed, hinge side).</summary>
    Door = 0x32,

    /// <summary>0x30 - a scripted NPC dialog (text/options/input) the server displays (answered by C->S 0x3A).</summary>
    NpcDialog = 0x30,

    /// <summary>
    ///     0x33 - display another player (aisling) entering view: position, facing, and a
    ///     discriminated appearance (full equipment or a creature-sprite override).
    /// </summary>
    DisplayUser = 0x33,

    /// <summary>
    ///     0x34 - another player's profile pane (sent when the receiving client clicks an aisling).
    ///     Distinct from 0x39 SelfProfile.
    /// </summary>
    Profile = 0x34,

    /// <summary>0x35 - display a read-only paper/scroll (the non-writable signpost slate).</summary>
    ReadonlyPaper = 0x35,

    /// <summary>0x36 - the online-user ("who's online") list, answering C->S 0x18.</summary>
    UserList = 0x36,

    /// <summary>0x3C - one row of raw map tile data (answers C->S 0x05 RequestMap).</summary>
    MapData = 0x3C,

    /// <summary>0x37 - place an item in an equipment-pane slot.</summary>
    AddEquipment = 0x37,

    /// <summary>0x38 - clear an equipment-pane slot.</summary>
    RemoveEquipment = 0x38,

    /// <summary>0x39 - self profile (the player's own profile pane).</summary>
    SelfProfile = 0x39,

    /// <summary>0x3A - add/recolor/remove a status-effect icon on the status bar.</summary>
    StatusBar = 0x3A,

    /// <summary>
    ///     0x3B - the server's byte-heartbeat challenge: two random bytes to be echoed back
    ///     (reversed) via C->S 0x45.
    /// </summary>
    ByteHeartbeat = 0x3B,

    /// <summary>
    ///     0x3D - a two-byte level/point indicator. Modeled for protocol completeness;
    ///     not emitted by typical servers.
    /// </summary>
    LevelPoint = 0x3D,

    /// <summary>
    ///     0x3E - a one-byte window-change signal. Modeled for protocol completeness;
    ///     not emitted by typical servers.
    /// </summary>
    WindowChange = 0x3E,

    /// <summary>0x3F - start a cooldown sweep on a spell- or skill-pane slot.</summary>
    Cooldown = 0x3F,

    /// <summary>0x42 - report a step of a player-to-player trade (answers/drives C->S 0x4A).</summary>
    Exchange = 0x42,

    /// <summary>
    ///     0x44 - payload-free. Modeled for protocol completeness; not emitted by typical servers.
    /// </summary>
    AddUser = 0x44,

    /// <summary>
    ///     0x45 - discriminated body on a leading [u8 flag]: flag != 0 is bare, flag == 0 carries
    ///     [u8][tail]. Modeled for protocol completeness; not emitted by typical servers.
    /// </summary>
    ItemShop = 0x45,

    /// <summary>
    ///     0x47 - a [u16] online-user count. Modeled for protocol completeness;
    ///     not emitted by typical servers.
    /// </summary>
    TotalUsers = 0x47,

    /// <summary>0x48 - abort the spell currently being cast (clears the cast bar); payload-free.</summary>
    CancelCast = 0x48,

    /// <summary>0x49 - request a portrait/profile upload (answered by C->S 0x4F SetProfile); payload-free signal.</summary>
    RequestPortrait = 0x49,

    /// <summary>
    ///     0x4A - body [u8 Type][u8 Payload][u32 BE Magic]. Modeled for protocol completeness;
    ///     not emitted by typical servers.
    /// </summary>
    BadGuy = 0x4A,

    /// <summary>
    ///     0x4B - "bounce": instruct the recipient to send a packet as if it had generated it
    ///     (a length-prefixed inner C->S packet to re-emit).
    /// </summary>
    Bounce = 0x4B,

    /// <summary>
    ///     0x4C - the server's answer to a client exit request (C->S 0x0B): a single byte
    ///     confirming the player may leave the world; the exit handshake then advances and the
    ///     server redirects to login.
    /// </summary>
    ConfirmExit = 0x4C,

    /// <summary>0x4F - open or update a player-run shop (the employee/consignment shop window; answered by C->S 0x54 PlayerShopAction).</summary>
    PlayerShop = 0x4F,

    /// <summary>0x50 - open/page the manufacture (crafting) window (answered by C->S 0x55).</summary>
    Manufacture = 0x50,

    /// <summary>
    ///     0x51 - lock or release player input (block-input toggle). Modeled for protocol
    ///     completeness; not emitted by typical servers.
    /// </summary>
    BlockInput = 0x51,

    /// <summary>0x56 - the server table payload (lobby).</summary>
    ServerTableData = 0x56,

    /// <summary>0x58 - map-stream-complete signal (the map may now be shown).</summary>
    MapLoadComplete = 0x58,

    /// <summary>
    ///     0x5B - body [u16 len][len bytes][u16][u16][u8]. Modeled for protocol completeness;
    ///     not emitted by typical servers.
    /// </summary>
    Advertisement = 0x5B,

    /// <summary>0x60 - the login-screen notification (checksum probe + full compressed payload; answers C->S 0x4B).</summary>
    LoginNotification = 0x60,

    /// <summary>
    ///     0x62 - discriminated body on a leading [u8 type]: type 3 is [string8 url][string8],
    ///     any other type is [string8][string8][string8]. Modeled for protocol completeness;
    ///     not emitted by typical servers.
    /// </summary>
    WebBoard = 0x62,

    /// <summary>
    ///     0x63 - a group notification (invite, recruitment info, or recruitment pull). Grouping
    ///     actions are answered with C->S 0x2E.
    /// </summary>
    Group = 0x63,

    /// <summary>
    ///     0x64 - discriminated body on a leading [u8 type]: types 3/4/8 carry [u8], type 7 carries
    ///     [u32 BE][u32 BE], any other type is bare. Modeled for protocol completeness;
    ///     not emitted by typical servers.
    /// </summary>
    MiniGame = 0x64,

    /// <summary>
    ///     0x66 - a URL the server hands the client: the homepage/account-URL slot (subtype 3)
    ///     or a URL alert popup (subtypes 1/2).
    /// </summary>
    Url = 0x66,

    /// <summary>
    ///     0x67 - the "map change pending" signal sent at the start of a map transition
    ///     (before 0x15 MapInfo / 0x04 Location).
    /// </summary>
    MapChangePending = 0x67,

    /// <summary>
    ///     0x68 - the server's tick-heartbeat challenge: a 32-bit tick echoed back (with the
    ///     receiver's own tick) via C->S 0x75.
    /// </summary>
    TickHeartbeat = 0x68,

    /// <summary>
    ///     0x6B - a one-byte signal that opens a town-map window parameterized by the byte.
    ///     Modeled for protocol completeness; not emitted by typical servers.
    /// </summary>
    Screenshot = 0x6B,

    /// <summary>
    ///     0x6D - a [string8] name push. Modeled for protocol completeness;
    ///     not emitted by typical servers.
    /// </summary>
    FamilyName = 0x6D,

    /// <summary>
    ///     0x6F - push metafile content to the recipient's on-disk cache: either one named file's
    ///     compressed data (DataByName) or the full checksum manifest (AllCheckSums). Answers
    ///     C->S 0x7B RequestMetafile.
    /// </summary>
    Metafile = 0x6F,

    /// <summary>
    ///     0x7E - the lobby's unencrypted connection greeting, sent on accept before the 0x00
    ///     CryptoKey handshake: [u8 0x1B]["CONNECTED SERVER"].
    /// </summary>
    AcceptConnection = 0x7E
}
