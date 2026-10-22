using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace VYaml.SourceGenerator;

// Equatable, compilation-independent representation of a diagnostic.
// Holding Diagnostic/Location directly in the incremental pipeline would root the
// Compilation and break caching, so we capture only value-typed data here and
// reconstruct the Diagnostic at the RegisterSourceOutput stage.
sealed record DiagnosticInfo
{
    public DiagnosticDescriptor Descriptor { get; }
    public LocationInfo? Location { get; }
    public EquatableArray<string> MessageArgs { get; }

    DiagnosticInfo(DiagnosticDescriptor descriptor, LocationInfo? location, EquatableArray<string> messageArgs)
    {
        Descriptor = descriptor;
        Location = location;
        MessageArgs = messageArgs;
    }

    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location? location, params string[] messageArgs)
    {
        return new DiagnosticInfo(descriptor, LocationInfo.CreateFrom(location), new EquatableArray<string>(messageArgs));
    }

    public Diagnostic ToDiagnostic()
    {
        var args = new object?[MessageArgs.Count];
        for (var i = 0; i < args.Length; i++)
        {
            args[i] = MessageArgs[i];
        }
        return Diagnostic.Create(Descriptor, Location?.ToLocation(), args);
    }
}

// Equatable, compilation-independent representation of a source Location.
sealed record LocationInfo
{
    public string FilePath { get; }
    public TextSpan TextSpan { get; }
    public LinePositionSpan LineSpan { get; }

    LocationInfo(string filePath, TextSpan textSpan, LinePositionSpan lineSpan)
    {
        FilePath = filePath;
        TextSpan = textSpan;
        LineSpan = lineSpan;
    }

    public Location ToLocation() => Location.Create(FilePath, TextSpan, LineSpan);

    public static LocationInfo? CreateFrom(Location? location)
    {
        if (location is null || location.SourceTree is null)
        {
            return null;
        }
        return new LocationInfo(location.SourceTree.FilePath, location.SourceSpan, location.GetLineSpan().Span);
    }
}
