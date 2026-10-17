using System.Runtime.CompilerServices;
using Xunit;

namespace PlantCare.Api.Tests;

/// <summary>
/// Roslynator's RCS0056 enforces the 120-character cap during the build, but it skips
/// lines that start with a string literal and lines that are pure comments. This
/// source scan covers those blind spots so the cap holds for every line.
/// </summary>
public class LineLengthTests
{
    private const int MaxLength = 120;

    [Fact]
    public void Source_files_stay_within_the_line_length_cap()
    {
        var backend = Path.GetFullPath(Path.Combine(TestDirectory(), ".."));
        var offenders = new List<string>();

        foreach (var project in new[] { "PlantCare.Api", "PlantCare.Api.Tests" })
        {
            foreach (var file in Directory.EnumerateFiles(
                Path.Combine(backend, project), "*.cs", SearchOption.AllDirectories))
            {
                if (IsExcluded(file))
                {
                    continue;
                }

                var lines = File.ReadAllLines(file);
                for (var index = 0; index < lines.Length; index++)
                {
                    if (lines[index].Length > MaxLength)
                    {
                        var relative = Path.GetRelativePath(backend, file);
                        offenders.Add($"{relative}:{index + 1} ({lines[index].Length} chars)");
                    }
                }
            }
        }

        Assert.Empty(offenders);
    }

    private static bool IsExcluded(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains(
            $"{Path.DirectorySeparatorChar}Data{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}",
            StringComparison.Ordinal);

    private static string TestDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;
}
