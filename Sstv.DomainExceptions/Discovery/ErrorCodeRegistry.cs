using System.Collections.Frozen;

namespace Sstv.DomainExceptions.Discovery;

/// <summary>
/// Aggregates error codes and call graphs from multiple assemblies
/// and provides traversal to collect all error codes for any entry-point method.
/// </summary>
public sealed class ErrorCodeRegistry
{
    private ErrorCodeRegistry(
        FrozenDictionary<string, ErrorCodeSource[]> allErrorCodes,
        FrozenDictionary<string, string[]> callGraph
    )
    {
        AllErrorCodes = allErrorCodes;
        CallGraph = callGraph;
    }

    private static Lazy<ErrorCodeRegistry>? _lazy;

    /// <summary>
    /// Gets the singleton instance. Throws if not initialized.
    /// </summary>
    public static ErrorCodeRegistry Instance => _lazy is null
        ? throw new InvalidOperationException("Error codes registry not initialized")
        : _lazy.Value;

    /// <summary>
    /// All error codes keyed by method key (merged from all assemblies).
    /// </summary>
    public FrozenDictionary<string, ErrorCodeSource[]> AllErrorCodes { get; }

    /// <summary>
    /// Call graph keyed by caller method key (merged from all assemblies).
    /// </summary>
    public FrozenDictionary<string, string[]> CallGraph { get; }

    /// <summary>
    /// Initializes the registry with error code maps and call graphs from each assembly.
    /// Must be called once at application startup before any queries.
    /// </summary>
    public static void Init(
        IReadOnlyDictionary<string, ErrorCodeSource[]>[] errorCodeMaps,
        IReadOnlyDictionary<string, string[]>[] callGraphMaps
    )
    {
        _lazy = new Lazy<ErrorCodeRegistry>(() => new ErrorCodeRegistry(
            MergeErrorCodes(errorCodeMaps),
            MergeCallGraphs(callGraphMaps))
        );
    }

    /// <summary>
    /// Recursively collects all error codes reachable from the given method key.
    /// </summary>
    public static ErrorCodeSource[] GetAllErrorCodes(string methodKey)
    {
        var result = new List<ErrorCodeSource>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var seenCodes = new HashSet<string>(StringComparer.Ordinal);
        CollectErrorCodes(methodKey, result, visited, seenCodes);
        return [.. result];
    }

    private static void CollectErrorCodes(
        string methodKey,
        List<ErrorCodeSource> result,
        HashSet<string> visited,
        HashSet<string> seenCodes)
    {
        if (!visited.Add(methodKey))
        {
            return;
        }

        if (Instance.AllErrorCodes.TryGetValue(methodKey, out var codes))
        {
            foreach (var code in codes)
            {
                if (seenCodes.Add(code.Code))
                {
                    result.Add(code);
                }
            }
        }

        if (Instance.CallGraph.TryGetValue(methodKey, out var callees))
        {
            foreach (var callee in callees)
            {
                CollectErrorCodes(callee, result, visited, seenCodes);
            }
        }
    }

    private static FrozenDictionary<string, ErrorCodeSource[]> MergeErrorCodes(
        IReadOnlyDictionary<string, ErrorCodeSource[]>[] errorCodeMaps
    )
    {
        var merged = new Dictionary<string, List<ErrorCodeSource>>();

        foreach (var errorCodes in errorCodeMaps)
        {
            foreach (var kvp in errorCodes)
            {
                if (merged.TryGetValue(kvp.Key, out var list))
                {
                    list.AddRange(kvp.Value);
                }
                else
                {
                    merged[kvp.Key] = [.. kvp.Value];
                }
            }
        }

        return merged.ToFrozenDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
    }

    private static FrozenDictionary<string, string[]> MergeCallGraphs(
        IReadOnlyDictionary<string, string[]>[] callGraphMaps
    )
    {
        var merged = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var callGraphMap in callGraphMaps)
        {
            foreach (var kvp in callGraphMap)
            {
                if (merged.TryGetValue(kvp.Key, out var set))
                {
                    set.UnionWith(kvp.Value);
                }
                else
                {
                    merged[kvp.Key] = [.. kvp.Value];
                }
            }
        }

        return merged.ToFrozenDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
    }
}