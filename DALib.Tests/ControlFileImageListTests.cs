using System.Text;
using DALib.Drawing;

namespace DALib.Tests;

/// <summary>
///     Regression coverage for the ControlFile <c>&lt;IMAGE&gt;</c> parse. Per the 7.41 UI layout grammar
///     (darkages-741 <c>ui_layout_parse_control_block</c>), each <c>&lt;IMAGE&gt;</c> entry is one explicit
///     (archive-entry, frame) visual state, and the block is an ordered list — not a start/end range. The old
///     parser filled the inclusive range between the first and last frame index, so a sparse list like 0, 1, 3
///     became 0, 1, 2, 3: it fabricated frame 2 and shifted every later button state.
/// </summary>
public class ControlFileImageListTests
{
    private static string WriteTempControlFile(string contents)
    {
        var path = Path.Combine(Path.GetTempPath(), $"dalib-ctrl-{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, contents, Encoding.ASCII);

        return path;
    }

    [Fact]
    public void SparseImageList_IsKeptVerbatim_NotRangeFilled()
    {
        // frames 0, 1, 3 — deliberately sparse (frame 2 omitted)
        const string CONTENTS = """
            <CONTROL>
            <NAME> "TESTBTN"
            <IMAGE>
            "btn.spf" 0
            "btn.spf" 1
            "btn.spf" 3
            <ENDCONTROL>
            """;

        var path = WriteTempControlFile(CONTENTS);

        try
        {
            var controlFile = ControlFile.FromFile(path);
            var control = controlFile["TESTBTN"];

            control.Images.Should().NotBeNull();
            control.Images!.Select(i => i.FrameIndex)
                   .Should()
                   .Equal(0, 1, 3); // NOT 0, 1, 2, 3
        }
        finally
        {
            File.Delete(path);
        }
    }
}
