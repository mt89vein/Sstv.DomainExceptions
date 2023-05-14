using System.Reflection;

namespace Sstv.DomainExceptions;

/// <summary>
/// Reflection common methods.
/// </summary>
internal static class ReflectionHelper
{
    /// <summary>
    /// Returns implementations of <see cref="DomainException"/> from <paramref name="assemblies"/>.
    /// </summary>
    /// <param name="assemblies">
    /// Assemblies to scan. If not supplied, scans all loaded into app domain assembiles.
    /// </param>
    public static Type[] GetDomainExceptionTypes(params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        if (assemblies.Length == 0)
        {
            assemblies = AppDomain.CurrentDomain.GetAssemblies();
        }

        IEnumerable<Type> domainExceptionsTypes;
        try
        {
            domainExceptionsTypes = assemblies.SelectMany(s => s.GetTypes());
        }
        catch (ReflectionTypeLoadException e)
        {
            domainExceptionsTypes = e.Types.Where(t => t != null).Cast<Type>();
        }

        return domainExceptionsTypes.Where(t =>
            t.IsSubclassOf(typeof(DomainException)) &&
            t is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false }
        ).ToArray();
    }

    /// <summary>
    /// Returns enum type which closes generic parameter of DomainException.
    /// </summary>
    /// <param name="domainExceptionType">Implementation type of DomainException.</param>
    /// <exception cref="ArgumentException">
    /// When <paramref name="domainExceptionType"/> not based on <see cref="DomainException"/>.
    /// </exception>
    public static Type GetDomainErrorCodeEnumType(this Type domainExceptionType)
    {
        if (domainExceptionType.BaseType!.GetGenericTypeDefinition() != typeof(DomainException<>))
        {
            throw new ArgumentException(
                $"{domainExceptionType} should be based on {typeof(DomainException<>)}. " +
                $"Multi-level inheritance is not supported."
            );
        }

        return domainExceptionType.BaseType.GetGenericArguments().Single();
    }

    /// <summary>
    /// Returns <see cref="ErrorDescriptionAttribute"/> from DomainException type, that placed on it's enum type.
    /// </summary>
    /// <param name="domainExceptionType">Implementation type of DomainException.</param>
    public static ErrorDescriptionAttribute? GetErrorDescriptionAttributeFromDomainExceptionType(this Type domainExceptionType)
    {
        var domainErrorCodeEnumType = domainExceptionType.GetDomainErrorCodeEnumType();

        return domainErrorCodeEnumType.GetErrorDescriptionAttributeFromEnum();
    }

    /// <summary>
    /// Returns <see cref="ErrorDescriptionAttribute"/>.
    /// </summary>
    /// <param name="domainErrorCodesEnumType">Enum that describes error codes.</param>
    /// <exception cref="ArgumentException">
    /// When <paramref name="domainErrorCodesEnumType"/> not enum type.
    /// </exception>
    public static ErrorDescriptionAttribute? GetErrorDescriptionAttributeFromEnum(this Type domainErrorCodesEnumType)
    {
        if (domainErrorCodesEnumType.BaseType != typeof(Enum))
        {
            throw new ArgumentException($"{domainErrorCodesEnumType} should be an enum type.");
        }

        return domainErrorCodesEnumType
            .GetCustomAttributes(typeof(ErrorDescriptionAttribute), false)
            .Cast<ErrorDescriptionAttribute>()
            .FirstOrDefault();
    }
}