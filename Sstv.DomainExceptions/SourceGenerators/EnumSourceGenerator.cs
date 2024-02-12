using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sstv.DomainExceptions.SourceGenerators.Models;
using System.Collections.Immutable;

namespace Sstv.DomainExceptions.SourceGenerators;

[Generator(LanguageNames.CSharp)]
internal sealed class EnumSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // subscribe to changes in enums
        var directEnums = context
            .SyntaxProvider
            .CreateSyntaxProvider(
                predicate: EnumDeclarationSyntaxPredicate,
                transform: EnumDeclarationSyntaxTransform)
            .Where(x => x is not null);

        var provider = context.CompilationProvider.Combine(directEnums.Collect());
        context.RegisterSourceOutput(provider, (context, tuple) => GenerateEnumClasses(tuple.Left, tuple.Right!, context));
    }

    private static bool EnumDeclarationSyntaxPredicate(SyntaxNode node, CancellationToken _)
    {
        return node is EnumDeclarationSyntax
        {
            AttributeLists: { Count: > 0 } attributes
        } && attributes.Any(attr => attr.Name.Contains(Constants.ATTRIBUTE_CLASS_NAME));
    }

    private static EnumDeclarationSyntax? EnumDeclarationSyntaxTransform(
        GeneratorSyntaxContext context,
        CancellationToken token
    )
    {
        var syntax = (EnumDeclarationSyntax)context.Node;
        var attributeLists = syntax.AttributeLists;

        // Flatten all attribute lists
        for (var i = 0; i < attributeLists.Count; i++)
        {
            var attributes = attributeLists[i].Attributes;
            for (var j = 0; j < attributes.Count; j++)
            {
                var attributeSyntax = attributes[j];

                // Constructor is the method
                if (context.SemanticModel.GetSymbolInfo(attributeSyntax, token).Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                // Check attribute name with plain text,
                // because now we do not have access to ISymbol of Attribute
                var containingTypeSymbol = attributeSymbol.ContainingType?
                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    .Replace("global::", "");
                if (containingTypeSymbol is not Constants.ERROR_DESCRIPTION_ATTRIBUTE_FULL_NAME)
                {
                    continue;
                }

                return syntax;
            }
        }

        return null;
    }

    private static void GenerateEnumClasses(
        Compilation compilation,
        ImmutableArray<EnumDeclarationSyntax> enums,
        SourceProductionContext context
    )
    {
        // nothing to generate
        if (enums.IsDefaultOrEmpty)
        {
            return;
        }

        var enumInfos = GetAllEnumsToGenerate(compilation, enums, context.CancellationToken);

        if (enumInfos is null or { Count: 0 })
        {
            return;
        }

        var generationContext = new GenerationContext(
            nullableEnabled: compilation.Options.NullableContextOptions is not NullableContextOptions.Disable
        );

        foreach (var enumInfo in enumInfos)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            CodeGenerator.GenerateEnumExtensions(enumInfo, context, generationContext);
        }
    }

    /// <summary>
    /// Returns ErrorCodeEnumInfo for all enums with [ErrorDescription] attribute, with its members etc.
    /// </summary>
    /// <param name="compilation">Compilation context, where <paramref name="enums"/> were found</param>
    /// <param name="enums">All <c>enum</c> declarations. They will be filtered to be annotated with [EnumClass]</param>
    /// <param name="ct">Cancellation token from user compilation</param>
    /// <returns>List of <see cref="ErrorCodeEnumInfo"/> with length > 0 if successfully parsed, or null otherwise</returns>
    private static List<ErrorCodeEnumInfo>? GetAllEnumsToGenerate(
        Compilation compilation,
        ImmutableArray<EnumDeclarationSyntax> enums,
        CancellationToken ct
    )
    {
        var errorDescriptionAttributeSymbol =
            compilation.GetTypeByMetadataName(Constants.ERROR_DESCRIPTION_ATTRIBUTE_FULL_NAME);

        if (errorDescriptionAttributeSymbol is null)
        {
            return null;
        }

        var exceptionConfigAttributeSymbol =
            compilation.GetTypeByMetadataName(Constants.EXCEPTION_CONFIG_ATTRIBUTE_FULL_NAME);

        if (exceptionConfigAttributeSymbol is null)
        {
            return null;
        }

        var errorCodeEnums = new List<ErrorCodeEnumInfo>(enums.Length);

        foreach (var syntax in enums)
        {
            ct.ThrowIfCancellationRequested();

            var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);

            if (semanticModel.GetDeclaredSymbol(syntax, ct) is not { EnumUnderlyingType: not null } enumSymbol)
            {
                continue;
            }

            var enumInfo = ErrorCodeDescriptionFromEnumParser.ParseEnum(
                enumSymbol,
                errorDescriptionAttributeSymbol,
                exceptionConfigAttributeSymbol
            );

            ct.ThrowIfCancellationRequested();

            errorCodeEnums.Add(enumInfo);
        }

        return errorCodeEnums.Count > 0
            ? errorCodeEnums
            : null;
    }
}