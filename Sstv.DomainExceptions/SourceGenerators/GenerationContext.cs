using System.Text;

namespace Sstv.DomainExceptions.SourceGenerators;

/// <summary>
/// Source generation context.
/// </summary>
internal sealed class GenerationContext
{
    /// <summary>
    /// Source code builder.
    /// </summary>
    private readonly StringBuilder _builder = new(200);

    /// <summary>
    /// Creates new instance of <see cref="GenerationContext"/>.
    /// </summary>
    /// <param name="nullableEnabled">Is nullable enabled.</param>
    public GenerationContext(bool nullableEnabled)
    {
        NullableEnabled = nullableEnabled;
    }

    /// <summary>
    /// Is nullable enabled.
    /// </summary>
    public bool NullableEnabled { get; }

    /// <summary>
    /// Get empty builder.
    /// </summary>
    /// <returns>Source code builder.</returns>
    public StringBuilder GetBuilder()
    {
        _builder.Clear();
        return _builder;
    }
}