using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace Sstv.DomainExceptions.Extensions.DependencyInjection;

/// <summary>
/// Error codes description source from <see cref="IConfiguration"/>.
/// </summary>
[SuppressMessage("Design", "CA1812: Avoid uninstantiated internal classes")]
public sealed class ErrorCodesDescriptionFromConfigurationSource : IErrorCodesDescriptionSource
{
    /// <summary>
    /// Section with error codes.
    /// </summary>
    private readonly IConfigurationSection _configuration;

    /// <summary>
    /// Section name with error codes.
    /// </summary>
    internal static string? SectionName { get; set; }

    /// <summary>
    /// Initialize new instance of <see cref="ErrorCodesDescriptionFromConfigurationSource"/>.
    /// </summary>
    /// <param name="configuration">Configuration.</param>
    /// <exception cref="ArgumentNullException">
    /// When <paramref name="configuration"/> was null.
    /// </exception>
    public ErrorCodesDescriptionFromConfigurationSource(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _configuration = configuration.GetSection(SectionName ?? "DomainExceptionSettings:ErrorCodes");
    }

    /// <summary>
    /// Returns the <see cref="ErrorDescription"/> by it <paramref name="errorCode"/>.
    /// </summary>
    /// <param name="errorCode">Error code.</param>
    public ErrorDescription? GetDescription(string errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            return null;
        }

        var section = _configuration.GetSection(errorCode);

        if (!section.Exists())
        {
            return null;
        }

        var description = section[nameof(ErrorDescription.Description)];
        var helpLink = section[nameof(ErrorDescription.HelpLink)];
        var isObsolete = section.GetValue(nameof(ErrorDescription.IsObsolete), defaultValue: false);

        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var additionalData = section
            .GetSection(nameof(ErrorDescription.AdditionalData))
            .GetChildren()
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .ToDictionary(x => x.Key, x => (object)x.Value!);

        return new ErrorDescription(errorCode, description, helpLink, isObsolete, additionalData);
    }

    /// <summary>
    /// Returns enumeration of all error descriptions.
    /// If source is not able to provide this method, then just return Enumerable.Empty.
    /// </summary>
    public IEnumerable<ErrorDescription> Enumerate()
    {
        var chilren = _configuration.GetChildren();

        foreach (var child in chilren)
        {
            var errorDescription = GetDescription(child.Key);

            if (errorDescription is not null)
            {
                yield return errorDescription;
            }
        }
    }
}