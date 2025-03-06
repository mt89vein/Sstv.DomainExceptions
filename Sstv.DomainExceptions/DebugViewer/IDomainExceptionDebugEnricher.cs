namespace Sstv.DomainExceptions.DebugViewer;

/// <summary>
/// Debug view model enricher.
/// </summary>
public interface IDomainExceptionDebugEnricher
{
    /// <summary>
    /// Erich <paramref name="domainExceptionCodeDebugVm"/>.
    /// </summary>
    /// <param name="domainExceptionCodeDebugVm">Domain exception debug view model.</param>
    public void Enrich(DomainExceptionCodeDebugVm domainExceptionCodeDebugVm);
}