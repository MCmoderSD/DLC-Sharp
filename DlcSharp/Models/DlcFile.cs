namespace DlcSharp;

/// <summary>
/// A file of a package.
/// </summary>
public sealed class DlcFile
{
    /// <summary>
    /// The download link, or <see langword="null"/> if the container has none for this file.
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// The file name as stated in the container, or <see langword="null"/> if it does not state one.
    /// </summary>
    public string? FileName { get; init; }

    /// <summary>
    /// The size in bytes, or <see langword="null"/> if the container does not state it.
    /// </summary>
    public long? Size { get; init; }
}