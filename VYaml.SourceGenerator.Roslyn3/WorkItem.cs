using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace VYaml.SourceGenerator;

class WorkItem
{
    public TypeDeclarationSyntax Syntax { get; }

    public WorkItem(TypeDeclarationSyntax syntax)
    {
        Syntax = syntax;
    }

    public TypeMetaModel? Analyze(in GeneratorExecutionContext context, ReferenceSymbols references)
    {
        var semanticModel = context.Compilation.GetSemanticModel(Syntax.SyntaxTree);
        var symbol = semanticModel.GetDeclaredSymbol(Syntax, context.CancellationToken);
        if (symbol is INamedTypeSymbol typeSymbol)
        {
            var attributeData = symbol.GetAttributes().FirstOrDefault(x =>
                SymbolEqualityComparer.Default.Equals(x.AttributeClass, references.YamlObjectAttribute));
            if (attributeData is null)
            {
                return null;
            }
            return TypeMetaAnalyzer.Analyze(typeSymbol, Syntax, attributeData, references);
        }
        return null;
    }
}
