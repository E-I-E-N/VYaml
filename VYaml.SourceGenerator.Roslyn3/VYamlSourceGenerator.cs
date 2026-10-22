using System;
using Microsoft.CodeAnalysis;

namespace VYaml.SourceGenerator;

// Non-incremental (Roslyn 3.x / Unity) generator. Shares the analysis (TypeMetaAnalyzer) and emit
// (Emitter) with the Roslyn4 incremental generator, so both produce identical output.
[Generator(LanguageNames.CSharp)]
public class VYamlSourceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxContextReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        try
        {
            var references = ReferenceSymbols.Create(context.Compilation);
            if (references is null) return;

            if (context.SyntaxContextReceiver! is not SyntaxContextReceiver syntaxCollector) return;

            foreach (var workItem in syntaxCollector.GetWorkItems())
            {
                if (context.CancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var model = workItem.Analyze(in context, references);
                if (model is null) continue;

                foreach (var diagnostic in model.Diagnostics)
                {
                    context.ReportDiagnostic(diagnostic.ToDiagnostic());
                }

                if (model.IsValid)
                {
                    context.AddSource(model.HintName, Emitter.Emit(model));
                }
            }
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.UnexpectedErrorDescriptor,
                Location.None,
                ex.ToString().Replace(Environment.NewLine, " ")));
        }
    }
}
