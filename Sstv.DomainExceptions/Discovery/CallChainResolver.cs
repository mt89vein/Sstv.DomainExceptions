using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sstv.DomainExceptions.Discovery;

internal partial class ErrorCodeMethodCollector
{
    private static List<string> CollectCalledMethods(
        MethodInfo methodInfo,
        Dictionary<string, List<string>>? interfaceImplCache = null)
    {
        if (methodInfo.SyntaxNode is null || methodInfo.SemanticModel is null)
        {
            return [];
        }

        return AnalyzeCalledMethodsFromNode(methodInfo.SyntaxNode, methodInfo.SemanticModel, interfaceImplCache);
    }

    private static List<string> AnalyzeCalledMethodsFromNode(
        SyntaxNode body, SemanticModel? semanticModel,
        Dictionary<string, List<string>>? interfaceImplCache = null)
    {
        var calledKeys = new List<string>();
        if (semanticModel is null)
        {
            return calledKeys;
        }

        try
        {
            foreach (var invocation in body.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol methodSymbol)
                {
                    var receiverType = methodSymbol.ContainingType?.ToDisplayString();
                    if (string.IsNullOrEmpty(receiverType))
                    {
                        continue;
                    }

                    if (methodSymbol.ContainingType?.TypeKind == TypeKind.Interface &&
                        interfaceImplCache is not null &&
                        interfaceImplCache.TryGetValue(receiverType + "." + methodSymbol.Name, out var implKeys))
                    {
                        foreach (var implKey in implKeys)
                        {
                            if (!calledKeys.Contains(implKey))
                            {
                                calledKeys.Add(implKey);
                            }
                        }
                    }
                    else
                    {
                        var key = receiverType + "." + methodSymbol.Name;
                        if (!calledKeys.Contains(key))
                        {
                            calledKeys.Add(key);
                        }
                    }
                }
            }
        }
        catch
        {
            // partial results are acceptable
        }

        return calledKeys;
    }

    private static IEnumerable<INamedTypeSymbol> GetAllSourceTypes(INamespaceSymbol ns)
    {
        foreach (var member in ns.GetMembers())
        {
            if (member is INamespaceSymbol childNs)
            {
                foreach (var type in GetAllSourceTypes(childNs))
                {
                    yield return type;
                }
            }
            else if (member is INamedTypeSymbol namedType)
            {
                yield return namedType;
                foreach (var nested in namedType.GetTypeMembers())
                {
                    yield return nested;
                }
            }
        }
    }

    private static Dictionary<string, List<string>> BuildInterfaceImplementationCache(Compilation compilation)
    {
        var cache = new Dictionary<string, List<string>>();

        foreach (var type in GetAllSourceTypes(compilation.SourceModule.GlobalNamespace))
        {
            if (type is { TypeKind: TypeKind.Class, IsAbstract: false, IsStatic: false })
            {
                foreach (var iface in type.AllInterfaces)
                {
                    foreach (var member in iface.GetMembers().OfType<IMethodSymbol>())
                    {
                        var impl = type.FindImplementationForInterfaceMember(member);
                        if (impl is IMethodSymbol { MethodKind: MethodKind.Ordinary } implMethod)
                        {
                            var ifaceKey = iface.ToDisplayString() + "." + member.Name;
                            var implKey = implMethod.ContainingType.ToDisplayString() + "." + implMethod.Name;
                            if (!cache.TryGetValue(ifaceKey, out var list))
                            {
                                list = [implKey];
                                cache[ifaceKey] = list;
                            }
                            else if (!list.Contains(implKey))
                            {
                                list.Add(implKey);
                            }
                        }
                    }
                }
            }
        }

        return cache;
    }
}
