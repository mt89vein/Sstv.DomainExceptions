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
        "Map",
        "MapGet",
        "MapPost",
        "MapPut",
        "MapDelete",
        "MapPatch",
        "MapQuery",
        "MapGroup",
        "MapMethods"
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var settings = context.CompilationProvider
            .Select(static (compilation, _) =>
            {
                var attr = compilation.Assembly.GetAttributes().FirstOrDefault(a =>
                    a.AttributeClass?.ToDisplayString() == "Sstv.DomainExceptions.CollectErrorCodesAttribute");

                if (attr is null)
                {
                    return new GeneratorSettings { IsEnabled = false };
                }

                var s = new GeneratorSettings { IsEnabled = true };

                foreach (var na in attr.NamedArguments)
                {
                    switch (na.Key)
                    {
                        case "MaxPropagationDepth" when na.Value.Value is int depth:
                            s.MaxPropagationDepth = depth;
                            break;
                        case "ClassName" when na.Value.Value is string name:
                            s.ClassName = name;
                            break;
                        default:
                            break;
                    }
                }

                return s;
            });

        var methods = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, _) =>
                {
                    try
                    {
                        var methodInfo = GetMethodInfoFromSyntax(ctx);
                        if (methodInfo is null)
                        {
                            return null;
                        }

                        var compilation = ctx.SemanticModel.Compilation;
                        var cache = new InterfaceCache(compilation);
                        var (codes, calls) = CollectCodeAnalysis(default, methodInfo.SyntaxNode!,
                            methodInfo.SemanticModel, cache);

                        return new MethodAnalysis(
                            methodInfo.TypeName + "." + methodInfo.MethodName,
                            [.. codes],
                            [.. calls]);
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
            settings.Combine(methods.Combine(endpoints)),
            static (spc, data) =>
            {
                var (s, (methods, endpoints)) = data;

                if (!s.IsEnabled)
                {
                    return;
                }

                try
                {
                    GenerateErrorCodesDictionary(spc, s, (methods, endpoints));
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
        var key = endpointName ?? (GetHttpMethod(invocation) + " " + routePattern);

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

    private static string GetHttpMethod(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            return memberAccess.Name.Identifier.Text switch
            {
                "MapGet" => "GET",
                "MapPost" => "POST",
                "MapPut" => "PUT",
                "MapDelete" => "DELETE",
                "MapPatch" => "PATCH",
                "MapQuery" => "QUERY",
                "MapMethods" => "METHODS",
                _ => "*"
            };
        }

        return "*";
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
        GeneratorSettings settings,
        (ImmutableArray<MethodAnalysis?> Methods, ImmutableArray<EndpointInfo?> Endpoints) input)
    {
        var allMethods = input.Methods.OfType<MethodAnalysis>().ToList();
        var allEndpoints = input.Endpoints.OfType<EndpointInfo>().ToList();

        if (allMethods.Count == 0 && allEndpoints.Count == 0)
        {
            return;
        }

        var methodErrorCodes = new Dictionary<string, List<ErrorCodeInfo>>();
        var methodCalls = new Dictionary<string, List<string>>();

        foreach (var ma in allMethods)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (methodErrorCodes.TryGetValue(ma.Key, out var existingCodes))
            {
                foreach (var code in ma.ErrorCodes)
                {
                    if (existingCodes.All(e => e.Code != code.Code))
                    {
                        existingCodes.Add(code);
                    }
                }

                if (methodCalls.TryGetValue(ma.Key, out var existingCalls))
                {
                    foreach (var ck in ma.CalledKeys)
                    {
                        if (!existingCalls.Contains(ck))
                        {
                            existingCalls.Add(ck);
                        }
                    }
                }
                else
                {
                    methodCalls[ma.Key] = [.. ma.CalledKeys];
                }

                continue;
            }

            methodErrorCodes[ma.Key] = [.. ma.ErrorCodes];
            methodCalls[ma.Key] = [.. ma.CalledKeys];
        }

        var compilation = allEndpoints.FirstOrDefault()?.SemanticModel?.Compilation;
        var interfaceCache = compilation is not null
            ? new InterfaceCache(compilation)
            : null;

        foreach (var endpointInfo in allEndpoints)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var key = endpointInfo.Key;
            if (methodErrorCodes.ContainsKey(key))
            {
                continue;
            }

            var (codes, calls) =
                CollectCodeAnalysis(context, endpointInfo.Body, endpointInfo.SemanticModel, interfaceCache);

            methodErrorCodes[key] = codes;
            methodCalls[key] = calls;
        }

        var callersOf = new Dictionary<string, List<string>>();
        foreach (var kvp in methodCalls)
        {
            var caller = kvp.Key;
            foreach (var callee in kvp.Value)
            {
                if (!callersOf.TryGetValue(callee, out var list))
                {
                    list = [];
                    callersOf[callee] = list;
                }

                list.Add(caller);
            }
        }

        var workSet = new List<string>(methodErrorCodes.Count);
        foreach (var kvp in methodErrorCodes)
        {
            if (kvp.Value.Count > 0)
            {
                workSet.Add(kvp.Key);
            }
        }

        var maxDepth = settings.MaxPropagationDepth.GetValueOrDefault(10);
        for (var iteration = 0; iteration < maxDepth && workSet.Count > 0; iteration++)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var nextWorkSet = new List<string>();

            foreach (var key in workSet)
            {
                if (!callersOf.TryGetValue(key, out var callers))
                {
                    continue;
                }

                if (!methodErrorCodes.TryGetValue(key, out var currentCodes))
                {
                    continue;
                }

                foreach (var caller in callers)
                {
                    if (!methodErrorCodes.TryGetValue(caller, out var callerCodes))
                    {
                        continue;
                    }

                    var added = false;
                    foreach (var code in currentCodes)
                    {
                        if (callerCodes.All(e => e.Code != code.Code))
                        {
                            callerCodes.Add(code);
                            added = true;
                        }
                    }

                    if (added)
                    {
                        nextWorkSet.Add(caller);
                    }
                }
            }

            workSet = nextWorkSet;
        }

        if (methodErrorCodes.All(kvp => kvp.Value.Count == 0))
        {
            return;
        }

        var namespaceName = compilation?.AssemblyName ?? "Sstv.DomainExceptions";

        GenerateSource(context, methodErrorCodes, namespaceName, settings.ClassName);
    }
}