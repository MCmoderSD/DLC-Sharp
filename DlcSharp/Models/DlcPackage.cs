namespace DlcSharp;

public sealed class DlcPackage
{
    public string? Name { get; init; }

    public string[] Passwords { get; init; } = [];

    public string? Comment { get; init; }

    public string? Category { get; init; }

    public IReadOnlyList<DlcFile> Files { get; init; } = [];
}