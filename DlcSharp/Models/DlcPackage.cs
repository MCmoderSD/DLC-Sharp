namespace DlcSharp;

/// <summary>
/// A group of files that belong together, for example the parts of one archive.
/// </summary>
public sealed class DlcPackage
{
    /// <summary>
    /// The name of the package, or <see langword="null"/> if the container has none.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// The passwords of the archive. Empty if there are none, never <see langword="null"/>.
    /// A list such as <c>{"a","b"}</c> is split into its items.
    /// </summary>
    public string[] Passwords { get; init; } = [];

    /// <summary>
    /// A comment, or <see langword="null"/> if the container has none.
    /// </summary>
    public string? Comment { get; init; }

    /// <summary>
    /// The category, or <see langword="null"/> if the container has none.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// The files of the package. A file without a download link is kept.
    /// </summary>
    public IReadOnlyList<DlcFile> Files { get; init; } = [];
}