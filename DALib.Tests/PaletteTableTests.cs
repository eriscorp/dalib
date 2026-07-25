using System.Text;
using DALib.Drawing;

namespace DALib.Tests;

/// <summary>
///     Regression coverage for <see cref="PaletteTable" /> text parsing. Per the 7.41 palette-range parser
///     (darkages-741 <c>file_palette_table_parse_ranges</c>), blank lines and <c>//</c> comments are skipped.
///     The old parser split each line on a single space and only accepted 2- or 3-token lines, so a trailing
///     <c>//</c> comment (extra tokens) or a run of spaces (empty tokens) silently dropped the entry.
/// </summary>
public class PaletteTableTests
{
    private static string WriteTempTable(string contents)
    {
        var path = Path.Combine(Path.GetTempPath(), $"dalib-pal-{Guid.NewGuid():N}.tbl");
        File.WriteAllText(path, contents, Encoding.UTF8);

        return path;
    }

    [Fact]
    public void Parse_StripsCommentsAndToleratesWhitespaceRuns()
    {
        const string CONTENTS = """
            // leading full-line comment
            1 5 3   // range 1-5 -> palette 3, with a trailing comment
            10    7
            """;

        var path = WriteTempTable(CONTENTS);

        try
        {
            var table = PaletteTable.FromFile(path);

            // range entry survived the trailing // comment
            table.GetPaletteNumber(1).Should().Be(3);
            table.GetPaletteNumber(5).Should().Be(3);

            // single override survived the run of spaces
            table.GetPaletteNumber(10).Should().Be(7);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
