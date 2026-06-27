using DALib.Networking.Crypto;
using DALib.Networking.Packets.Server;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Server;

/// <summary>
///     Coverage for 0x31 Board (S->C) - the bulletin-board / mailbox UI driver. Pins each form's wire
///     layout, verifies type dispatch over the leading byte (including the shared-shape board/mailbox
///     and post pairs), and round-trips through the codec.
/// </summary>
public class BoardResponsePacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    // ---- layout pins --------------------------------------------------------------------------

    [Fact]
    public void BoardList_WriteBody_PinsLayout()
    {
        // [01][string8 name=""][u8 count=2]{[u16 id][string8 name]}  (Mail as first entry, id 0)
        new BoardListPacket
            {
                ResponseType = BoardResponseType.BoardList,
                Name = "",
                Boards = [new BoardListEntry(0, "Mail"), new BoardListEntry(1, "News")]
            }
            .ToBody().Should().Equal(
                0x01, 0x00, 0x02,
                0x00, 0x00, 0x04, 0x4D, 0x61, 0x69, 0x6C,  // (0, "Mail")
                0x00, 0x01, 0x04, 0x4E, 0x65, 0x77, 0x73); // (1, "News")
    }

    [Fact]
    public void BoardIndex_WriteBody_PinsLayout()
    {
        // [02][refresh][u16 boardId][string8 boardName][u8 count]{[bool hl][u16 postId][string8 author][u8 mon][u8 day][string8 subj]}
        new BoardIndexPacket
            {
                ResponseType = BoardResponseType.PublicBoard,
                RefreshFlag = 1,
                BoardId = 5,
                BoardName = "News",
                Messages = [new BoardMessageHeader(true, 10, "Bob", 6, 17, "Hi")]
            }
            .ToBody().Should().Equal(
                0x02, 0x01, 0x00, 0x05, 0x04, 0x4E, 0x65, 0x77, 0x73, 0x01,
                0x01, 0x00, 0x0A, 0x03, 0x42, 0x6F, 0x62, 0x06, 0x11, 0x02, 0x48, 0x69);
    }

    [Fact]
    public void BoardPost_WriteBody_PinsLayout()
    {
        // [03][refresh][bool hl][u16 postId][string8 author][u8 mon][u8 day][string8 subj][string16 body]
        new BoardPostPacket
            {
                ResponseType = BoardResponseType.PublicPost,
                RefreshFlag = 0,
                Highlight = false,
                PostId = 10,
                Author = "Bob",
                Month = 6,
                Day = 17,
                Subject = "Hi",
                Body = "Hello!"
            }
            .ToBody().Should().Equal(
                0x03, 0x00, 0x00, 0x00, 0x0A, 0x03, 0x42, 0x6F, 0x62, 0x06, 0x11, 0x02, 0x48, 0x69,
                0x00, 0x06, 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x21);
    }

    [Fact]
    public void BoardResult_WriteBody_PinsLayout()
    {
        // [06][bool success][string8 message]
        new BoardResultPacket
            { ResponseType = BoardResponseType.EndResult, Success = true, Message = "Posted." }
            .ToBody().Should().Equal(
                0x06, 0x01, 0x07, 0x50, 0x6F, 0x73, 0x74, 0x65, 0x64, 0x2E);
    }

    [Fact]
    public void WriteBody_RejectsTypeOutsideForm()
    {
        // a post type on the result form is invalid
        var act = () => new BoardResultPacket
            { ResponseType = BoardResponseType.PublicPost, Success = true, Message = "x" }.ToBody();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Messages_AreAccumulable_AfterConstruction()
    {
        // the board/mail workflow: build the packet, then add rows as per-character access checks pass
        var packet = new BoardIndexPacket
        {
            ResponseType = BoardResponseType.PublicBoard,
            RefreshFlag = 1,
            BoardId = 5,
            BoardName = "News"
        };

        foreach (var (header, hasAccess) in new[]
                 {
                     (new BoardMessageHeader(false, 1, "Bob", 1, 1, "Public"), true),
                     (new BoardMessageHeader(false, 2, "Mod", 1, 2, "Hidden"), false),
                     (new BoardMessageHeader(true, 3, "Ann", 1, 3, "Pinned"), true),
                 })
            if (hasAccess)
                packet.Messages.Add(header);

        packet.Messages.Should().HaveCount(2);
        packet.ToBody()[0].Should().Be((byte)BoardResponseType.PublicBoard);
    }

    // ---- type dispatch ------------------------------------------------------------------------

    [Fact]
    public void Parse_Type1_IsBoardList()
    {
        var parsed = BoardResponsePacket.Parse(
                [0x01, 0x00, 0x02, 0x00, 0x00, 0x04, 0x4D, 0x61, 0x69, 0x6C, 0x00, 0x01, 0x04, 0x4E, 0x65, 0x77, 0x73])
            .Should().BeOfType<BoardListPacket>().Subject;

        parsed.Name.Should().Be("");
        parsed.Boards.Should().Equal(new BoardListEntry(0, "Mail"), new BoardListEntry(1, "News"));
    }

    [Fact]
    public void Parse_Type4_IsBoardIndex_PrivateBoard()
    {
        // a mailbox index (type 4) shares the board-index shape; ResponseType is preserved
        var parsed = BoardResponsePacket.Parse([0x04, 0x01, 0x00, 0x05, 0x01, 0x4D, 0x00])
            .Should().BeOfType<BoardIndexPacket>().Subject;

        parsed.ResponseType.Should().Be(BoardResponseType.PrivateBoard);
        parsed.BoardId.Should().Be((ushort)5);
        parsed.BoardName.Should().Be("M");
        parsed.Messages.Should().BeEmpty();
    }

    [Fact]
    public void Parse_Type5_IsBoardPost_PrivatePost()
    {
        var parsed = BoardResponsePacket.Parse(
                [0x05, 0x00, 0x01, 0x00, 0x0A, 0x03, 0x42, 0x6F, 0x62, 0x06, 0x11, 0x02, 0x48, 0x69,
                    0x00, 0x06, 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x21])
            .Should().BeOfType<BoardPostPacket>().Subject;

        parsed.ResponseType.Should().Be(BoardResponseType.PrivatePost);
        parsed.PostId.Should().Be((ushort)10);
        parsed.Author.Should().Be("Bob");
        parsed.Subject.Should().Be("Hi");
        parsed.Body.Should().Be("Hello!");
    }

    [Fact]
    public void Parse_UnknownType_Throws()
    {
        var act = () => BoardResponsePacket.Parse([0x09]);

        act.Should().Throw<InvalidDataException>();
    }

    // ---- round-trip through the codec ---------------------------------------------------------

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void RoundTrip_ThroughCodec_PreservesForm(BoardResponsePacket original)
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();

        var wire = codec.EncodeServer(original, crypto);
        var parsed = codec.ParseServerPacket(wire, crypto);

        // record equality compares IReadOnlyList by reference, so assert structural equivalence
        // (element-wise) and the exact variant type separately.
        parsed.Should().BeOfType(original.GetType());
        parsed.Should().BeEquivalentTo(original, opts => opts.RespectingRuntimeTypes());
    }

    public static TheoryData<BoardResponsePacket> RoundTripCases() =>
    [
        new BoardListPacket
        {
            ResponseType = BoardResponseType.BoardList,
            Boards = [new BoardListEntry(0, "Mail"), new BoardListEntry(3, "Town Crier")]
        },
        new BoardIndexPacket
        {
            ResponseType = BoardResponseType.PublicBoard,
            RefreshFlag = 1,
            BoardId = 12,
            BoardName = "Town Crier",
            Messages =
            [
                new BoardMessageHeader(false, 1, "Angelique", 1, 1, "Welcome"),
                new BoardMessageHeader(true, 2, "Comhaigne", 12, 25, "Festival")
            ]
        },
        new BoardIndexPacket
        {
            ResponseType = BoardResponseType.PrivateBoard,
            RefreshFlag = 1,
            BoardId = 0,
            BoardName = "Mail",
            Messages = []
        },
        new BoardPostPacket
        {
            ResponseType = BoardResponseType.PrivatePost,
            RefreshFlag = 0,
            Highlight = true,
            PostId = 42,
            Author = "Aether",
            Month = 6,
            Day = 17,
            Subject = "Re: trade",
            Body = "Meet me in Mileth."
        },
        new BoardResultPacket
            { ResponseType = BoardResponseType.DeleteResult, Success = false, Message = "You cannot delete that." },
    ];
}
