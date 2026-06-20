using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace Sstv.DomainExceptions.Discovery;

[Generator(LanguageNames.CSharp)]
internal partial class ErrorCodeMethodCollector : IIncrementalGenerator
{
    private static readonly HashSet<string> _mapMethodNames = new(StringComparer.Ordinal)
    {
        "Map", "MapGet", "MapPost", "MapPut", "MapDelete", "MapPatch", "MapQuery",
        "MapGroup", "MapMethods"
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var enabled = context.CompilationProvider
            .Select(static (compilation, _) =>
                compilation.Assembly.GetAttributes().Any(
                    a => a.AttributeClass?.ToDisplayString() == "Sstv.DomainExceptions.CollectErrorCodesAttribute"));

        var methods = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, _) =>
                {
                    try
                    {
                        return GetMethodInfoFromSyntax(ctx);
                    }
                    catch
                    {
                        return null;
                    }
                })
            .Where(static m => m != null)
            .Collect();

        var endpoints = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsMinimalApiEndpoint(node),
                transform: static (ctx, _) =>
                {
                    try
                    {
                        return GetEndpointInfo(ctx);
                    }
                    catch
                    {
                        return null;
                    }
                })
            .Where(static e => e != null)
            .Collect();

        context.RegisterSourceOutput(
            enabled.Combine(methods.Combine(endpoints)),
            static (spc, data) =>
            {
                var (isEnabled, (methods, endpoints)) = data;
                if (!isEnabled)
                {
                    return;
                }

                try
                {
                    GenerateErrorCodesDictionary(spc, (methods, endpoints));
                }
                catch (Exception ex)
                {
                    spc.ReportDiagnostic(
                        Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "SSTVDE0001",
                                "ErrorCodeMethodCollector failed",
                                "ErrorCodeMethodCollector failed: {0}",
                                "ErrorCodeMethodCollector",
                                DiagnosticSeverity.Warning,
                                isEnabledByDefault: true),
                            Location.None,
                            ex.Message));
                }
            });
    }

    private static MethodInfo? GetMethodInfoFromSyntax(GeneratorSyntaxContext ctx)
    {
        if (ctx.Node is not MethodDeclarationSyntax methodDecl)
        {
            return null;
        }

        var symbol = ctx.SemanticModel.GetDeclaredSymbol(methodDecl);
        if (symbol is null)
        {
            return null;
        }

        var typeSymbol = symbol.ContainingType;
        if (typeSymbol is null)
        {
            return null;
        }

        var typeName = typeSymbol.ToDisplayString();
        var namespaceName = typeSymbol.ContainingNamespace?.ToDisplayString();
        if (string.IsNullOrEmpty(namespaceName))
        {
            namespaceName = ctx.SemanticModel.Compilation.AssemblyName;
        }

        return new MethodInfo
        {
            TypeName = typeName,
            MethodName = symbol.Name,
            Namespace = namespaceName ?? "Global",
            SyntaxNode = methodDecl,
            SemanticModel = ctx.SemanticModel
        };
    }

    private static bool IsMinimalApiEndpoint(SyntaxNode node)
    {
        if (node is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        return _mapMethodNames.Contains(memberAccess.Name.Identifier.Text);
    }

    private static EndpointInfo? GetEndpointInfo(GeneratorSyntaxContext ctx)
    {
        if (ctx.Node is not InvocationExpressionSyntax invocation)
        {
            return null;
        }

        var routePattern = GetRoutePattern(invocation);
        if (routePattern is null)
        {
            return null;
        }

        var endpointName = GetEndpointName(invocation);
        var key = endpointName ?? routePattern;

        var body = GetEndpointBody(invocation);
        if (body is null)
        {
            return null;
        }

        return new EndpointInfo
        {
            Key = key,
            Namespace = ctx.SemanticModel.Compilation.AssemblyName ?? "MinimalApi",
            Body = body,
            SemanticModel = ctx.SemanticModel
        };
    }

    private static string? GetRoutePattern(InvocationExpressionSyntax invocation)
    {
        if (invocation.ArgumentList is null || invocation.ArgumentList.Arguments.Count < 2)
        {
            return null;
        }

        var firstArg = invocation.ArgumentList.Arguments[0].Expression;
        if (firstArg is LiteralExpressionSyntax literal &&
            literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            return literal.Token.ValueText;
        }

        return null;
    }

    private static string? GetEndpointName(InvocationExpressionSyntax mapInvocation)
    {
        if (mapInvocation.Parent is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Parent is InvocationExpressionSyntax outerInvocation &&
            memberAccess.Name.Identifier.Text == "WithName" &&
            outerInvocation.ArgumentList?.Arguments.Count >= 1 &&
            outerInvocation.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax literal &&
            literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            return literal.Token.ValueText;
        }

        return null;
    }

    private static SyntaxNode? GetEndpointBody(InvocationExpressionSyntax invocation)
    {
        var args = invocation.ArgumentList?.Arguments;
        if (args is null || args.Value.Count < 2)
        {
            return null;
        }

        var handlerArg = args.Value[args.Value.Count - 1].Expression;

        return handlerArg switch
        {
            LambdaExpressionSyntax lambda => lambda.Body ?? (SyntaxNode?)lambda.ExpressionBody,
            AnonymousMethodExpressionSyntax anonymous => anonymous.Block,
            _ => null
        };
    }

    private static void GenerateErrorCodesDictionary(
        SourceProductionContext context,
        (ImmutableArray<MethodInfo?> Methods, ImmutableArray<EndpointInfo?> Endpoints) input)
    {
        var allMethods = input.Methods.OfType<MethodInfo>().ToList();
        var allEndpoints = input.Endpoints.OfType<EndpointInfo>().ToList();

        if (allMethods.Count == 0 && allEndpoints.Count == 0)
        {
            return;
        }

        var methodErrorCodes = new Dictionary<string, List<ErrorCodeInfo>>();
        var methodCalls = new Dictionary<string, List<string>>();

        Compilation? compilation = null;
        if (allMethods.Count > 0)
        {
            compilation = allMethods[0].SemanticModel?.Compilation;
        }

        compilation ??= allEndpoints.FirstOrDefault()?.SemanticModel?.Compilation;

        var interfaceImplCache = compilation is not null
            ? BuildInterfaceImplementationCache(compilation)
            : null;

        foreach (var methodInfo in allMethods)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var key = methodInfo.TypeName + "." + methodInfo.MethodName;

            if (methodErrorCodes.TryGetValue(key, out var existingCodes))
            {
                var overloadCodes = CollectErrorCodes(context, methodInfo);
                foreach (var code in overloadCodes)
                {
                    if (existingCodes.All(e => e.Code != code.Code))
                    {
                        existingCodes.Add(code);
                    }
                }

                var overloadCalls = CollectCalledMethods(methodInfo, interfaceImplCache);
                if (methodCalls.TryGetValue(key, out var existingCalls))
                {
                    foreach (var ck in overloadCalls)
                    {
                        if (!existingCalls.Contains(ck))
                        {
                            existingCalls.Add(ck);
                        }
                    }
                }
                else
                {
                    methodCalls[key] = overloadCalls;
                }

                continue;
            }

            var errorCodes = CollectErrorCodes(context, methodInfo);
            var calledKeys = CollectCalledMethods(methodInfo, interfaceImplCache);

            methodErrorCodes[key] = errorCodes;
            methodCalls[key] = calledKeys;
        }

        foreach (var endpointInfo in allEndpoints)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var key = endpointInfo.Key;
            if (methodErrorCodes.ContainsKey(key))
            {
                continue;
            }

            var errorCodes = AnalyzeErrorCodesFromNode(context, endpointInfo.Body, endpointInfo.SemanticModel);
            var calledKeys = AnalyzeCalledMethodsFromNode(endpointInfo.Body, endpointInfo.SemanticModel, interfaceImplCache);

            methodErrorCodes[key] = errorCodes;
            methodCalls[key] = calledKeys;
        }

        for (var iteration = 0; iteration < 10; iteration++)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var anyAdded = false;
            foreach (var kvp in methodCalls)
            {
                var key = kvp.Key;
                var calledKeys = kvp.Value;

                if (!methodErrorCodes.TryGetValue(key, out var currentCodes))
                {
                    continue;
                }

                foreach (var calledKey in calledKeys)
                {
                    if (methodErrorCodes.TryGetValue(calledKey, out var calledCodes) && calledCodes.Count > 0)
                    {
                        foreach (var code in calledCodes)
                        {
                            if (currentCodes.All(e => e.Code != code.Code))
                            {
                                currentCodes.Add(code);
                                anyAdded = true;
                            }
                        }
                    }
                }
            }

            if (!anyAdded)
            {
                break;
            }
        }

        if (methodErrorCodes.All(kvp => kvp.Value.Count == 0))
        {
            return;
        }

        var namespaceName = compilation?.AssemblyName ?? "Sstv.DomainExceptions";

        GenerateSource(context, methodErrorCodes, namespaceName);
    }
}
