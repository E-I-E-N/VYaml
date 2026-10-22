using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace VYaml.SourceGenerator;

static class TrackingNames
{
    public const string Model = "VYaml.Model";
    public const string Emit = "VYaml.Emit";
}

[Generator]
public class VYamlIncrementalSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. transform: symbols -> value-equatable TypeMetaModel (the cache key).
        var models = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                context,
                "VYaml.Annotations.YamlObjectAttribute",
                static (node, cancellation) =>
                {
                    return node is ClassDeclarationSyntax
                        or StructDeclarationSyntax
                        or RecordDeclarationSyntax
                        or InterfaceDeclarationSyntax;
                },
                static (context, cancellation) =>
                {
                    var references = ReferenceSymbols.Create(context.SemanticModel.Compilation);
                    if (references is null)
                    {
                        return null;
                    }
                    return TypeMetaAnalyzer.Analyze(
                        (INamedTypeSymbol)context.TargetSymbol,
                        (TypeDeclarationSyntax)context.TargetNode,
                        context.Attributes.First(),
                        references);
                })
            .Where(static x => x is not null)
            .WithTrackingName(TrackingNames.Model);

        // 2. emit: model -> source string. Cached on the model, so the string is only
        //    rebuilt when a type's relevant shape actually changes.
        var emitted = models.Select(static (model, cancellation) =>
        {
            var source = model!.IsValid ? Emitter.Emit(model) : null;
            return new EmitResult(model.HintName, source, model.Diagnostics);
        }).WithTrackingName(TrackingNames.Emit);

        // 3. output: report diagnostics + AddSource. Skipped when the EmitResult is unchanged.
        context.RegisterSourceOutput(emitted, static (sourceProductionContext, result) =>
        {
            foreach (var diagnostic in result.Diagnostics)
            {
                sourceProductionContext.ReportDiagnostic(diagnostic.ToDiagnostic());
            }

            if (result.Source is { } source)
            {
                sourceProductionContext.AddSource(result.HintName, source);
            }
        });
    }
}

// Value-equatable emit output. Equality lets Roslyn skip RegisterSourceOutput (the AddSource call)
// for types whose generated output is unchanged.
sealed record EmitResult
{
    public string HintName { get; }
    public string? Source { get; }
    public EquatableArray<DiagnosticInfo> Diagnostics { get; }

    public EmitResult(string hintName, string? source, EquatableArray<DiagnosticInfo> diagnostics)
    {
        HintName = hintName;
        Source = source;
        Diagnostics = diagnostics;
    }
}
