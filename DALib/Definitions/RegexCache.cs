using System.Text.RegularExpressions;

namespace DALib.Definitions;

// ReSharper disable once PartialTypeWithSinglePart
internal static partial class RegexCache
{
    [GeneratedRegex(@"(\D+)(\d+)?(.+)?", RegexOptions.Compiled)]
    internal static partial Regex EntryNameRegex { get; }
}