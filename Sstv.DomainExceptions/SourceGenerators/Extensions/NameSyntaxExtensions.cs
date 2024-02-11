using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sstv.DomainExceptions.SourceGenerators;

internal static class NameSyntaxExtensions
{
    public static bool Contains(this NameSyntax nameSyntax, string value)
    {
        return (nameSyntax switch
        {
            IdentifierNameSyntax i => i.Identifier.Text,
            SimpleNameSyntax s => s.Identifier.Text,
            QualifiedNameSyntax q => q.Right.Identifier.Text,
            _ => nameSyntax.ToFullString()
        }).Contains(value);
    }
}