using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sstv.DomainExceptions.Discovery;

internal partial class ErrorCodeMethodCollector
{
    /// <summary>
    /// Per-interface implementation cache.
    /// Builds entries only for interfaces that are actually encountered.
    /// </summary>
    private sealed class InterfaceCache
    {
        private readonly Dictionary<string, List<string>> _cache = new();
        private readonly HashSet<string> _interfacesScanned = new();
        private readonly Compilation _compilation;
        private Dictionary<string, INamedTypeSymbol>? _typeMap;
        private List<INamedTypeSymbol>? _classTypes;

        public InterfaceCache(Compilation compilation)
        {
            _compilation = compilation;
        }

        public bool TryGetValue(string key, out List<string>? value)
        {
            if (_cache.TryGetValue(key, out value))
            {
                return true;
            }

            var dotIndex = key.LastIndexOf('.');
            if (dotIndex < 0)
            {
                value = null;
                return false;
            }

            var interfaceName = key.Substring(0, dotIndex);

            if (!_interfacesScanned.Add(interfaceName))
            {
                value = null;
                return false;
            }

            BuildForInterface(interfaceName);

            return _cache.TryGetValue(key, out value);
        }

        private Dictionary<string, INamedTypeSymbol> GetTypeMap()
        {
            if (_typeMap is null)
            {
                _typeMap = new Dictionary<string, INamedTypeSymbol>();
                foreach (var type in GetAllSourceTypes(_compilation.SourceModule.GlobalNamespace))
                {
                    _typeMap[type.ToDisplayString()] = type;
                }
            }
            return _typeMap;
        }

        private List<INamedTypeSymbol> GetClassTypes()
        {
            return _classTypes ??= [.. GetTypeMap().Values
                .Where(t => t is { TypeKind: TypeKind.Class, IsAbstract: false, IsStatic: false })];
        }

        private void BuildForInterface(string interfaceName)
        {
            var typeMap = GetTypeMap();
            if (!typeMap.TryGetValue(interfaceName, out var iface))
            {
                return;
            }

            foreach (var type in GetClassTypes())
            {
                if (!type.AllInterfaces.Any(i => i.ToDisplayString() == interfaceName))
                {
                    continue;
                }

                foreach (var member in iface.GetMembers().OfType<IMethodSymbol>())
                {
                    var impl = type.FindImplementationForInterfaceMember(member);
                    if (impl is IMethodSymbol { MethodKind: MethodKind.Ordinary } implMethod)
                    {
                        var ifaceKey = iface.ToDisplayString() + "." + member.Name;
                        var implKey = implMethod.ContainingType.ToDisplayString() + "." + implMethod.Name;

                        if (!_cache.TryGetValue(ifaceKey, out var list))
                        {
                            list = [implKey];
                            _cache[ifaceKey] = list;
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

    private static List<string> CollectCalledMethods(
        MethodInfo methodInfo,
        InterfaceCache? interfaceCache = null)
    {
        if (methodInfo.SyntaxNode is null || methodInfo.SemanticModel is null)
        {
            return [];
        }

        return AnalyzeCalledMethodsFromNode(methodInfo.SyntaxNode, methodInfo.SemanticModel, interfaceCache);
    }

    private static List<string> AnalyzeCalledMethodsFromNode(
        SyntaxNode body, SemanticModel? semanticModel,
        InterfaceCache? interfaceCache = null)
    {
        var calledKeys = new List<string>();
        if (semanticModel is null)
        {
            return calledKeys;
        }

        var seen = new HashSet<string>();
        try
        {
            foreach (var invocation in body.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                CollectCallFromInvocation(invocation, calledKeys, seen, semanticModel, interfaceCache);
            }
        }
        catch
        {
            // partial results are acceptable
        }

        return calledKeys;
    }

    private static void CollectCallFromInvocation(
        InvocationExpressionSyntax invocation,
        List<string> calledKeys,
        HashSet<string> seen,
        SemanticModel semanticModel,
        InterfaceCache? interfaceCache)
    {
        if (semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol methodSymbol)
        {
            var receiverType = methodSymbol.ContainingType?.ToDisplayString();
            if (string.IsNullOrEmpty(receiverType))
            {
                return;
            }

            if (methodSymbol.ContainingType?.TypeKind == TypeKind.Interface &&
                interfaceCache is not null &&
                interfaceCache.TryGetValue(receiverType + "." + methodSymbol.Name, out var implKeys) &&
                implKeys is not null)
            {
                foreach (var implKey in implKeys)
                {
                    if (seen.Add(implKey))
                    {
                        calledKeys.Add(implKey);
                    }
                }
            }
            else
            {
                var key = receiverType + "." + methodSymbol.Name;
                if (seen.Add(key))
                {
                    calledKeys.Add(key);
                }
            }
        }
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


}
