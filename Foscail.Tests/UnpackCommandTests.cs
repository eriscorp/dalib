using System.Text;
using Foscail.Commands;

namespace Foscail.Tests;

/// <summary>
///     End-to-end coverage of <c>foscail unpack</c> via <see cref="UnpackCommand.Run" />: the happy
///     path over a synthetic archive, batch continuation when one archive is hostile or unreadable,
///     explicit reporting of extended-format archives, and case-insensitive .dat discovery.
/// </summary>
public class UnpackCommandTests : IDisposable
{
    private readonly string OutputDir;
    private readonly string ScratchDir;

    public UnpackCommandTests()
    {
        ScratchDir = Directory.CreateTempSubdirectory("foscail-tests-").FullName;
        OutputDir = Path.Combine(ScratchDir, "out");
    }

    public void Dispose() => Directory.Delete(ScratchDir, true);

    private static byte[] BuildArchive(params (string Name, byte[] Data)[] entries)
        => SyntheticData.BuildArchive(entries);

    private string WriteArchive(string fileName, byte[] bytes)
    {
        var path = Path.Combine(ScratchDir, fileName);
        File.WriteAllBytes(path, bytes);

        return path;
    }

    [Fact]
    public void Run_Unpacks_A_Single_Archive()
    {
        var payload = "hello"u8.ToArray();
        var path = WriteArchive("simple.dat", BuildArchive(("a.txt", payload)));

        var exitCode = UnpackCommand.Run(path, OutputDir, convert: false);

        exitCode.Should().Be(0);
        File.ReadAllBytes(Path.Combine(OutputDir, "simple", "a.txt")).Should().BeEquivalentTo(payload);
    }

    [Fact]
    public void Run_Continues_The_Batch_When_One_Archive_Is_Hostile()
    {
        var archiveDir = Path.Combine(ScratchDir, "archives");
        Directory.CreateDirectory(archiveDir);

        // hostile: a traversal entry name; sorts before "good.dat" so failure must not end the batch
        File.WriteAllBytes(Path.Combine(archiveDir, "bad.dat"), BuildArchive(("../evil", [1, 2, 3])));
        File.WriteAllBytes(Path.Combine(archiveDir, "good.dat"), BuildArchive(("a.txt", [4, 5])));

        var exitCode = UnpackCommand.Run(archiveDir, OutputDir, convert: false);

        exitCode.Should().Be(2);
        File.Exists(Path.Combine(OutputDir, "good", "a.txt")).Should().BeTrue();
        File.Exists(Path.Combine(OutputDir, "evil")).Should().BeFalse();
    }

    [Fact]
    public void Run_Reports_ExtendedFormat_Archives_And_Continues()
    {
        var archiveDir = Path.Combine(ScratchDir, "archives");
        Directory.CreateDirectory(archiveDir);

        File.WriteAllBytes(Path.Combine(archiveDir, "extended.dat"), [0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00]);
        File.WriteAllBytes(Path.Combine(archiveDir, "good.dat"), BuildArchive(("a.txt", [4, 5])));

        var exitCode = UnpackCommand.Run(archiveDir, OutputDir, convert: false);

        // extended archive counts as a failure (never a silent zero-entry success); the batch continues
        exitCode.Should().Be(2);
        File.Exists(Path.Combine(OutputDir, "good", "a.txt")).Should().BeTrue();
        Directory.Exists(Path.Combine(OutputDir, "extended")).Should().BeFalse();
    }

    [Fact]
    public void Run_Finds_Archives_With_Uppercase_Extension()
    {
        var archiveDir = Path.Combine(ScratchDir, "archives");
        Directory.CreateDirectory(archiveDir);

        File.WriteAllBytes(Path.Combine(archiveDir, "UPPER.DAT"), BuildArchive(("a.txt", [7])));

        var exitCode = UnpackCommand.Run(archiveDir, OutputDir, convert: false);

        exitCode.Should().Be(0);
        File.Exists(Path.Combine(OutputDir, "UPPER", "a.txt")).Should().BeTrue();
    }

    [Fact]
    public void Run_Unpacks_An_Uppercase_Archive_Passed_Directly()
    {
        var path = WriteArchive("DIRECT.DAT", BuildArchive(("a.txt", [8])));

        var exitCode = UnpackCommand.Run(path, OutputDir, convert: false);

        exitCode.Should().Be(0);
        File.Exists(Path.Combine(OutputDir, "DIRECT", "a.txt")).Should().BeTrue();
    }
}
