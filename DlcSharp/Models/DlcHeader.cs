namespace DlcSharp;

/// <summary>
/// Information from the header of a container. Every value is <see langword="null"/> if the container does not state it.
/// </summary>
public sealed class DlcHeader
{
    /// <summary>
    /// The name of the program that created the container.
    /// </summary>
    public string? GeneratorApp { get; init; }

    /// <summary>
    /// The version of that program.
    /// </summary>
    public string? GeneratorVersion { get; init; }

    /// <summary>
    /// The web address of that program.
    /// </summary>
    public string? GeneratorUrl { get; init; }

    /// <summary>
    /// The name from the tribute element of the header.
    /// </summary>
    public string? TributeName { get; init; }

    /// <summary>
    /// The version of the DLC XML format.
    /// </summary>
    public string? DlcXmlVersion { get; init; }
}