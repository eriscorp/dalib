using DALib.Networking.Crypto;
using DALib.Networking.Packets.Client;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Packets.Client;

/// <summary>
///     Coverage for 0x3B BoardRequest (C->S) - the multi-variant board/mailbox request. Pins each
///     form's wire layout, verifies action-byte dispatch, the trailing-byte tolerance on Delete,
///     and the round-trip through the codec.
/// </summary>
public class BoardRequestPacketTests
{
    private static CryptoState MakeCrypto() => new()
    {
        EncryptionSeed = 5,
        EncryptionKey = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09],
    };

    // ---- layout pins --------------------------------------------------------------------------

    [Fact]
    public void BoardList_WriteBody_IsBareActionByte()
    {
        new BoardListPacket().ToBody().Should().Equal(0x01);
    }

    [Fact]
    public void ViewBoard_WriteBody_PinsLayout()
    {
        // [02][u16 boardId][s16 startPostId][s8 offset] - the "open board" sentinels 0x7FFF / -16
        new ViewBoardPacket { BoardId = 0x1234, StartPostId = 0x7FFF, Offset = -16 }
            .ToBody().Should().Equal(0x02, 0x12, 0x34, 0x7F, 0xFF, 0xF0);
    }

    [Fact]
    public void ViewPost_WriteBody_PinsLayout()
    {
        // [03][u16 boardId][s16 postId][s8 offset]
        new ViewPostPacket { BoardId = 5, PostId = 10, Offset = -1 }
            .ToBody().Should().Equal(0x03, 0x00, 0x05, 0x00, 0x0A, 0xFF);
    }

    [Fact]
    public void NewPost_WriteBody_PinsLayout()
    {
        // [04][u16 boardId][string8 subject][string16 body]
        new NewPostPacket { BoardId = 1, Subject = "hi", Body = "yo" }
            .ToBody().Should().Equal(
                0x04, 0x00, 0x01, 0x02, 0x68, 0x69, 0x00, 0x02, 0x79, 0x6F);
    }

    [Fact]
    public void Delete_WriteBody_PinsLayout()
    {
        // [05][u16 boardId][u16 postId] - canonical 6-byte body (the optional stray trailing byte
        // is not reproduced; the model writes the canonical form)
        new DeletePostPacket { BoardId = 2, PostId = 7 }
            .ToBody().Should().Equal(0x05, 0x00, 0x02, 0x00, 0x07);
    }

    [Fact]
    public void SendMail_WriteBody_PinsLayout()
    {
        // [06][u16 boardId][string8 recipient][string8 subject][string16 body]
        new SendMailPacket { BoardId = 3, Recipient = "Kedian", Subject = "hi", Body = "yo" }
            .ToBody().Should().Equal(
                0x06, 0x00, 0x03,
                0x06, 0x4B, 0x65, 0x64, 0x69, 0x61, 0x6E, // "Kedian"
                0x02, 0x68, 0x69,                         // "hi"
                0x00, 0x02, 0x79, 0x6F);                  // "yo"
    }

    [Fact]
    public void Highlight_WriteBody_PinsLayout()
    {
        // [07][u16 boardId][s16 postId]
        new HighlightPostPacket { BoardId = 4, PostId = 9 }
            .ToBody().Should().Equal(0x07, 0x00, 0x04, 0x00, 0x09);
    }

    // ---- action-byte dispatch -----------------------------------------------------------------

    [Fact]
    public void Parse_Action1_IsBoardList()
    {
        BoardRequestPacket.Parse([0x01]).Should().BeOfType<BoardListPacket>();
    }

    [Fact]
    public void Parse_Action2_IsViewBoard_WithOffset()
    {
        var parsed = BoardRequestPacket.Parse([0x02, 0x12, 0x34, 0x7F, 0xFF, 0xF0])
            .Should().BeOfType<ViewBoardPacket>().Subject;

        parsed.BoardId.Should().Be((ushort)0x1234);
        parsed.StartPostId.Should().Be((short)0x7FFF);
        parsed.Offset.Should().Be((sbyte)-16);
    }

    [Fact]
    public void Parse_Action3_IsViewPost()
    {
        var parsed = BoardRequestPacket.Parse([0x03, 0x00, 0x05, 0x00, 0x0A, 0xFF])
            .Should().BeOfType<ViewPostPacket>().Subject;

        parsed.BoardId.Should().Be((ushort)5);
        parsed.PostId.Should().Be((short)10);
        parsed.Offset.Should().Be((sbyte)-1);
    }

    [Fact]
    public void Parse_Action4_IsNewPost()
    {
        var parsed = BoardRequestPacket.Parse([0x04, 0x00, 0x01, 0x02, 0x68, 0x69, 0x00, 0x02, 0x79, 0x6F])
            .Should().BeOfType<NewPostPacket>().Subject;

        parsed.BoardId.Should().Be((ushort)1);
        parsed.Subject.Should().Be("hi");
        parsed.Body.Should().Be("yo");
    }

    [Fact]
    public void Parse_Action6_IsSendMail()
    {
        var parsed = BoardRequestPacket.Parse(
                [0x06, 0x00, 0x03, 0x06, 0x4B, 0x65, 0x64, 0x69, 0x61, 0x6E, 0x02, 0x68, 0x69, 0x00, 0x02, 0x79, 0x6F])
            .Should().BeOfType<SendMailPacket>().Subject;

        parsed.Recipient.Should().Be("Kedian");
        parsed.Subject.Should().Be("hi");
        parsed.Body.Should().Be("yo");
    }

    [Fact]
    public void Parse_Delete_ToleratesStrayTrailingByte()
    {
        // Some emitters append a constant trailing 0 (len-7 body); it is tolerated and ignored.
        var parsed = BoardRequestPacket.Parse([0x05, 0x00, 0x02, 0x00, 0x07, 0x00])
            .Should().BeOfType<DeletePostPacket>().Subject;

        parsed.BoardId.Should().Be((ushort)2);
        parsed.PostId.Should().Be((short)7);
    }

    [Fact]
    public void Parse_UnknownAction_Throws()
    {
        var act = () => BoardRequestPacket.Parse([0x7F]);

        act.Should().Throw<InvalidDataException>();
    }

    // ---- round-trip through the codec ---------------------------------------------------------

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void RoundTrip_ThroughCodec_PreservesForm(BoardRequestPacket original)
    {
        var codec = new PacketCodec();
        var crypto = MakeCrypto();

        var wire = codec.EncodeClient(original, crypto);
        var parsed = codec.ParseClientPacket(wire, crypto);

        parsed.Should().Be(original); // records: value equality across all fields + exact type
    }

    public static TheoryData<BoardRequestPacket> RoundTripCases() =>
    [
        new BoardListPacket(),
        new ViewBoardPacket { BoardId = 0x1234, StartPostId = 0x7FFF, Offset = -16 },
        new ViewPostPacket { BoardId = 5, PostId = 10, Offset = -1 },
        new NewPostPacket { BoardId = 1, Subject = "Welcome", Body = "Hello, world." },
        new DeletePostPacket { BoardId = 2, PostId = 7 },
        new SendMailPacket { BoardId = 3, Recipient = "Kedian", Subject = "hi", Body = "yo" },
        new HighlightPostPacket { BoardId = 4, PostId = 9 },
    ];
}
