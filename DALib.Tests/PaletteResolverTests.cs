using System.Text;
using DALib.Data;
using DALib.Definitions;
using DALib.Drawing;
using SkiaSharp;

namespace DALib.Tests;

/// <summary>
///     Coverage for <see cref="PaletteResolver" /> against comhaigne's palette-resolution.md. The resolver
///     reads only entry names (never EPF pixels), so fixtures are archives of dummy <c>.epf</c> entries plus
///     real <c>.pal</c>/<c>.tbl</c> palette sources. Per the spec's testing guidance, assertions are on
///     <c>RuleId</c> / <c>PaletteNumber</c> / <c>Kind</c> / <c>IsLuminanceBlended</c> — the load-bearing
///     first-match order and keying — not on color arrays (except the field000 wart, which is about identity).
/// </summary>
public class PaletteResolverTests : IDisposable
{
    private readonly string ScratchDir;
    private readonly List<DataArchive> Opened = [];

    public PaletteResolverTests() => ScratchDir = Directory.CreateTempSubdirectory("dalib-resolver-").FullName;

    public void Dispose()
    {
        foreach (var archive in Opened)
            archive.Dispose();

        Directory.Delete(ScratchDir, true);
    }

    // ── fixture builders ────────────────────────────────────────────────────────────────────────

    // legacy archive layout: [count+1:i32] then per entry [start:i32][name:13 ascii NUL-padded], a
    // final [end:i32], then concatenated entry data
    private static byte[] BuildArchive(params (string Name, byte[] Data)[] entries)
    {
        const int NAME_LENGTH = 13;

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.Default, true);

        writer.Write(entries.Length + 1);
        var address = 4 + entries.Length * (4 + NAME_LENGTH) + 4;

        foreach (var (name, data) in entries)
        {
            writer.Write(address);
            writer.Write(Encoding.ASCII.GetBytes(name.PadRight(NAME_LENGTH, '\0')));
            address += data.Length;
        }

        writer.Write(address);

        foreach (var (_, data) in entries)
            writer.Write(data);

        return ms.ToArray();
    }

    private static byte[] Pal(SKColor? markerAtIndex1 = null)
    {
        var palette = new Palette();

        if (markerAtIndex1 is { } color)
            palette[1] = color;

        using var ms = new MemoryStream();
        palette.Save(ms);

        return ms.ToArray();
    }

    private static (string, byte[]) Epf(string name) => (name, [0, 0, 0, 0]); // name-only; pixels unused

    private DataArchive Dat(params (string Name, byte[] Data)[] entries)
    {
        var path = Path.Combine(ScratchDir, $"{Guid.NewGuid():N}.dat");
        File.WriteAllBytes(path, BuildArchive(entries));
        var archive = DataArchive.FromFile(path, memoryMapped: false);
        Opened.Add(archive);

        return archive;
    }

    private static ArchiveProvider None => _ => null;

    private static ResolvedPalette? Resolve(string archiveName, DataArchive archive, string entryName, ArchiveProvider? provider = null, int frameIndex = 0)
    {
        var resolver = new PaletteResolver(archiveName, archive, provider ?? None);

        return resolver.Resolve(archive[entryName], frameIndex);
    }

    // gui00..gui17 with a distinct marker per index, so a wrong constant shows up
    private static (string, byte[])[] GuiPalettes()
        => Enumerable.Range(0, 18)
                     .Select(i => ($"gui{i:D2}.pal", Pal(new SKColor((byte)i, 0, 0))))
                     .ToArray();

    // ── setoa: order hazards + constants ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("dlgcre01a.epf", "setoa/dlgcre01", 8)] // must precede dlgcre (rule 12)
    [InlineData("dlgcrexx.epf", "setoa/lback", 4)]
    [InlineData("emot00.epf", "setoa/emot00", 0)]      // must precede emot (rule 9)
    [InlineData("emotxx.epf", "setoa/emot", 3)]
    [InlineData("lsbackm1.epf", "setoa/lsbackm", 0)]   // must precede lsback (rule 16)
    [InlineData("lsbackx.epf", "setoa/lsback", 10)]
    [InlineData("setup12.epf", "setoa/setup12", 0)]    // must precede setup (rule 12)
    [InlineData("setupxx.epf", "setoa/lback", 4)]
    [InlineData("lg_stat.epf", "setoa/lg_", 15)]
    [InlineData("album.epf", "setoa/album", 17)]
    [InlineData("zzztest.epf", "setoa/default", 0)]
    public void Setoa_FirstMatch_Order_And_Constants(string entryName, string ruleId, int paletteNumber)
    {
        (string, byte[])[] entries = [Epf(entryName), .. GuiPalettes()];
        var setoa = Dat(entries);

        var result = Resolve("setoa.dat", setoa, entryName);

        result.Should().NotBeNull();
        result!.RuleId.Should().Be(ruleId);
        result.PaletteNumber.Should().Be(paletteNumber);
        result.Kind.Should().Be(PaletteSourceKind.Constant);
    }

    [Fact]
    public void Setoa_Field_Wart_Forces_Field000_Over_Fielde00()
    {
        var real = new SKColor(10, 20, 30);
        var stray = new SKColor(200, 100, 50);

        // both field000.pal and the stray fielde00.pal parse to id 0; field000 must win slot 0
        (string, byte[])[] entries =
        [
            Epf("field0.epf"),
            ("field000.pal", Pal(real)),
            ("fielde00.pal", Pal(stray)),
            .. GuiPalettes()
        ];
        var setoa = Dat(entries);

        var result = Resolve("setoa.dat", setoa, "field0.epf");

        result.Should().NotBeNull();
        result!.RuleId.Should().Be("setoa/field");
        result.Kind.Should().Be(PaletteSourceKind.Indexed);
        result.Palette[1].Should().Be(real);
    }

    // ── legend ──────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Legend_Branches_Resolve_By_Prefix()
    {
        var legend = Dat(
            Epf("bkstory3.epf"),
            Epf("field2.epf"),
            Epf("skillfoo.epf"),
            Epf("linexx.epf"),
            Epf("f0bar.epf"),
            Epf("staffx.epf"),
            Epf("whatever.epf"),
            ("backpal3.pal", Pal()),
            ("field2.pal", Pal()),
            ("legend01.pal", Pal()),
            ("legend.pal", Pal()),
            ("staff.pal", Pal()));

        Resolve("legend.dat", legend, "bkstory3.epf")!.Should().BeEquivalentTo(new { RuleId = "legend/bkstory", PaletteNumber = 3, Kind = PaletteSourceKind.Indexed });
        Resolve("legend.dat", legend, "field2.epf")!.RuleId.Should().Be("legend/field");
        Resolve("legend.dat", legend, "skillfoo.epf")!.Should().BeEquivalentTo(new { RuleId = "legend/skill", Kind = PaletteSourceKind.Fixed });
        Resolve("legend.dat", legend, "linexx.epf")!.RuleId.Should().Be("legend/line"); // rule 5 before rule 7's list
        Resolve("legend.dat", legend, "f0bar.epf")!.RuleId.Should().Be("legend/f0");
        Resolve("legend.dat", legend, "staffx.epf")!.RuleId.Should().Be("legend/staff");
        Resolve("legend.dat", legend, "whatever.epf")!.RuleId.Should().Be("legend/default");
    }

    [Fact]
    public void Legend_Item_Uses_Global_Icon_Numbering_Per_Frame()
    {
        // file 2, frame 0 -> global icon (2-1)*266 + 0 + 1 = 267
        var legend = Dat(
            Epf("item002.epf"),
            ("itempal.tbl", "267 4\n"u8.ToArray()),
            ("item004.pal", Pal()),
            ("item000.pal", Pal()));

        var result = Resolve("legend.dat", legend, "item002.epf", frameIndex: 0);

        result.Should().NotBeNull();
        result!.RuleId.Should().Be("legend/item");
        result.Kind.Should().Be(PaletteSourceKind.Table);
        result.PaletteNumber.Should().Be(4);
    }

    // ── roh ─────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Roh_Efct_And_Mefc_Are_Table_Others_Unresolved()
    {
        var roh = Dat(
            Epf("efct001.epf"),
            Epf("mefc001.epf"),
            Epf("randomxyz.epf"),
            ("effpal.tbl", "1 2\n"u8.ToArray()),
            ("eff002.pal", Pal()),
            ("mefcpal.tbl", "1 3\n"u8.ToArray()),
            ("mefc003.pal", Pal()));

        Resolve("roh.dat", roh, "efct001.epf")!.Should().BeEquivalentTo(new { RuleId = "roh/efct", PaletteNumber = 2, Kind = PaletteSourceKind.Table });
        Resolve("roh.dat", roh, "mefc001.epf")!.RuleId.Should().Be("roh/mefc");
        Resolve("roh.dat", roh, "randomxyz.epf").Should().BeNull();
    }

    // ── national / misc: sibling legend.pal ─────────────────────────────────────────────────────

    [Fact]
    public void National_Uses_Sibling_Legend_Pal()
    {
        var legend = Dat(("legend.pal", Pal()));
        var national = Dat(Epf("tmap0.epf"));

        var result = Resolve("national.dat", national, "tmap0.epf", provider: name => name.Equals("legend.dat", StringComparison.OrdinalIgnoreCase) ? legend : null);

        result.Should().NotBeNull();
        result!.RuleId.Should().Be("national/all");
        result.Kind.Should().Be(PaletteSourceKind.Fixed);
    }

    [Fact]
    public void National_Without_Sibling_Is_Unresolved()
    {
        var national = Dat(Epf("tmap0.epf"));

        Resolve("national.dat", national, "tmap0.epf").Should().BeNull();
    }

    // ── khan ────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Khan_Table_Letter_With_Gender_Override_And_Remap()
    {
        // palu.tbl: sprite 1 default->0, male(-1)->1, female(-2)->2
        var khanpal = Dat(
            ("palu.tbl", "1 0\n1 1 -1\n1 2 -2\n"u8.ToArray()),
            ("palu000.pal", Pal()),
            ("palu001.pal", Pal()),
            ("palu002.pal", Pal()),
            ("palb.tbl", "1 5\n"u8.ToArray()),
            ("palb005.pal", Pal()));

        ArchiveProvider provider = name => name.Equals("khanpal.dat", StringComparison.OrdinalIgnoreCase) ? khanpal : null;

        var parts = Dat(Epf("mu00101.epf"), Epf("wu00101.epf"), Epf("ma00101.epf"));

        // mu = male, letter u -> palu, sprite 1 (3-digit id, anim 01 dropped), male override -> 1
        Resolve("khanmad.dat", parts, "mu00101.epf", provider)!.Should().BeEquivalentTo(new { RuleId = "khan/letter", PaletteNumber = 1, Kind = PaletteSourceKind.Table });
        // wu = female override -> 2
        Resolve("khanmad.dat", parts, "wu00101.epf", provider)!.PaletteNumber.Should().Be(2);
        // ma = letter a remaps to b -> palb, sprite 1 -> 5
        Resolve("khanmad.dat", parts, "ma00101.epf", provider)!.PaletteNumber.Should().Be(5);
    }

    [Fact]
    public void Khan_Body_Resolves_To_Lowest_Palm()
    {
        var khanpal = Dat(("palm3.pal", Pal()), ("palm7.pal", Pal()));
        ArchiveProvider provider = name => name.Equals("khanpal.dat", StringComparison.OrdinalIgnoreCase) ? khanpal : null;
        var body = Dat(Epf("mm00001.epf"));

        var result = Resolve("khanmad.dat", body, "mm00001.epf", provider);

        result.Should().NotBeNull();
        result!.RuleId.Should().Be("khan/body");
        result.PaletteNumber.Should().Be(3); // lowest available palm
    }

    [Fact]
    public void Khan_Table_Reports_Luminance_Blending_Above_1000()
    {
        // sprite 1 -> palette 1005: luminance-blended, real number 5
        var khanpal = Dat(("palu.tbl", "1 1005\n"u8.ToArray()), ("palu005.pal", Pal()));
        ArchiveProvider provider = name => name.Equals("khanpal.dat", StringComparison.OrdinalIgnoreCase) ? khanpal : null;
        var parts = Dat(Epf("mu00101.epf"));

        var result = Resolve("khanmad.dat", parts, "mu00101.epf", provider);

        result.Should().NotBeNull();
        result!.IsLuminanceBlended.Should().BeTrue();
        result.PaletteNumber.Should().Be(5);
    }

    [Fact]
    public void Khan_Without_Khanpal_Sibling_Is_Unresolved()
    {
        var parts = Dat(Epf("mu00101.epf"));

        Resolve("khanmad.dat", parts, "mu00101.epf").Should().BeNull();
    }

    // ── non-.epf is deferred (returns null) ─────────────────────────────────────────────────────

    [Fact]
    public void NonEpf_Is_Not_Resolved_Here()
    {
        var archive = Dat(("stc00001.hpf", [0, 0, 0, 0]), ("stc.tbl", "1 1\n"u8.ToArray()), ("stc001.pal", Pal()));

        Resolve("ia.dat", archive, "stc00001.hpf").Should().BeNull();
    }
}
