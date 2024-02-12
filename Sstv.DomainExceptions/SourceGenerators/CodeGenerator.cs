﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Sstv.DomainExceptions.SourceGenerators.Models;
using System.Globalization;
using System.Text;

namespace Sstv.DomainExceptions.SourceGenerators;

/// <summary>
/// Actual code generator.
/// </summary>
internal static class CodeGenerator
{
    /// <summary>
    /// Create enum extensions.
    /// </summary>
    /// <param name="errorCodeEnumInfo">Enum with [ErrorDescription] attribute.</param>
    /// <param name="productionContext">Context of source generator.</param>
    /// <param name="context">Our generator context.</param>
    public static void GenerateEnumExtensions(
        ErrorCodeEnumInfo errorCodeEnumInfo,
        SourceProductionContext productionContext,
        GenerationContext context
    )
    {
        var exceptionClassName = errorCodeEnumInfo.ExceptionConfigAttributeInfo.ClassName
                                 ?? errorCodeEnumInfo.EnumName + "Exception";

        GenerateEnumExtensionsClass(errorCodeEnumInfo, productionContext, context, exceptionClassName);
        GenerateExceptionClass(errorCodeEnumInfo, productionContext, context, exceptionClassName);
    }

    private static void GenerateExceptionClass(
        ErrorCodeEnumInfo errorCodeEnumInfo,
        SourceProductionContext productionContext,
        GenerationContext context,
        string exceptionClassName
    )
    {
        var builder = context.GetBuilder();

        builder.Append("// <auto-generated />");

        if (context.NullableEnabled)
        {
            // Source generated files should contain directive
            builder.AppendLine();
            builder.AppendLine("#nullable enable");
        }

        builder.AppendLine("#pragma warning disable CS0618 // ignore obsolete");
        builder.AppendLine();
        builder.AppendLine("using System;");
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendFormat("using {0};", Constants.NAMESPACE);
        builder.AppendLine();
        builder.AppendLine();

        builder.AppendFormat("namespace {0}\n{{\n", errorCodeEnumInfo.Namespace);
        builder.AppendFormat("    public sealed partial class {0} : DomainException", exceptionClassName);
        builder.AppendLine();
        builder.AppendLine("    {");

        #region Dictionary with all enums

        builder.AppendFormat("        public static readonly IReadOnlyDictionary<{0}, {1}> ErrorDescriptions = new Dictionary<{0}, {1}>", errorCodeEnumInfo.EnumName, Constants.ERROR_DESCRIPTION_CLASS_NAME);

        builder.AppendLine();
        builder.AppendLine("        {");

        foreach (var member in errorCodeEnumInfo.Members)
        {
            var errorCode = GetPrefixedErrorCode(errorCodeEnumInfo.ErrorDescription, member.ErrorDescription, member.IntegralValue);
            var helpLink = GetHelpLink(errorCodeEnumInfo.ErrorDescription, member.ErrorDescription, errorCode);
            var description = member.ErrorDescription.Description ?? "N/A";

            builder.AppendFormat("""            [{0}] = new {1}("{2}", "{3}", "{4}", {5}),""",
                member.EnumMemberNameWithEnumName,
                Constants.ERROR_DESCRIPTION_CLASS_NAME,
                errorCode,
                description,
                helpLink,
                member.IsObsolete ? "true" : "false"
            );
            builder.AppendLine();
        }

        builder.AppendLine("        };");

        #endregion Dictionary with all enums

        builder.AppendLine();

        #region IErrorCodesDescriptionSource

        builder.AppendLine("        public static IErrorCodesDescriptionSource ErrorCodesDescriptionSource { get; } = new ErrorCodesDescriptionInMemorySource(ErrorDescriptions.Values.ToDictionary(x => x.ErrorCode, x => x));");

        #endregion IErrorCodesDescriptionSource

        builder.AppendLine();

        #region Constructor

        builder.AppendFormat("        public {0}({1} {2}, Exception{3} innerException = null)", exceptionClassName, errorCodeEnumInfo.EnumName, errorCodeEnumInfo.GetVariableName(), context.NullableEnabled ? "?" : string.Empty);
        builder.AppendLine();
        builder.AppendFormat("            : base(ErrorDescriptions[{0}], innerException)", errorCodeEnumInfo.GetVariableName());
        builder.AppendLine();
        builder.AppendLine("        {");
        builder.AppendLine("        }");

        #endregion Constructor

        builder.AppendLine("    }");

        builder.AppendLine("}");

        productionContext.AddSource($"{exceptionClassName}.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
    }

    private static void GenerateEnumExtensionsClass(
        ErrorCodeEnumInfo errorCodeEnumInfo,
        SourceProductionContext productionContext,
        GenerationContext context,
        string exceptionClassName
    )
    {
        var builder = context.GetBuilder();

        builder.Append("// <auto-generated>");

        if (context.NullableEnabled)
        {
            // Source generated files should contain directive
            builder.AppendLine();
            builder.Append("#nullable enable\n\n");
        }

        builder.AppendLine("using System;");
        builder.AppendFormat("using {0};", Constants.NAMESPACE);
        builder.AppendLine();
        builder.AppendLine();

        builder.AppendFormat("namespace {0}\n{{\n", errorCodeEnumInfo.Namespace);
        builder.AppendFormat("    public static class {0}Extensions", exceptionClassName);
        builder.AppendLine();
        builder.AppendLine("    {");

        #region extension method to access ErrorDescription by enum value

        builder.AppendFormat("        public static {0} GetDescription(this {1} {2})", Constants.ERROR_DESCRIPTION_CLASS_NAME, errorCodeEnumInfo.EnumName, errorCodeEnumInfo.GetVariableName());
        builder.AppendLine();
        builder.AppendLine("        {");
        builder.AppendFormat("            return {0}.ErrorDescriptions[{1}];", exceptionClassName, errorCodeEnumInfo.GetVariableName());
        builder.AppendLine();
        builder.AppendLine("        }");

        #endregion extension method to access ErrorDescription by enum value

        builder.AppendLine();

        #region extension method to access ErrorCode by enum value

        builder.AppendFormat("        public static string GetErrorCode(this {0} {1})", errorCodeEnumInfo.EnumName, errorCodeEnumInfo.GetVariableName());
        builder.AppendLine();
        builder.AppendLine("        {");
        builder.AppendFormat("            return {0}.ErrorDescriptions[{1}].ErrorCode;", exceptionClassName, errorCodeEnumInfo.GetVariableName());
        builder.AppendLine();
        builder.AppendLine("        }");

        #endregion extension method to access ErrorCode by enum value

        builder.AppendLine();

        #region extension method to create exception

        builder.AppendFormat("        public static {0} ToException(this {1} {2}, Exception{3} innerException = null)", exceptionClassName, errorCodeEnumInfo.EnumName, errorCodeEnumInfo.GetVariableName(), context.NullableEnabled ? "?" : string.Empty);
        builder.AppendLine();
        builder.AppendLine("        {");
        builder.AppendFormat("            return new {0}({1}, innerException);", exceptionClassName, errorCodeEnumInfo.GetVariableName());
        builder.AppendLine();
        builder.AppendLine("        }");

        #endregion extension method to create exception

        builder.AppendLine("    }");
        builder.AppendLine("}");

        productionContext.AddSource($"{errorCodeEnumInfo.EnumName}Extensions.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
    }

    private static string? GetHelpLink(
        ErrorDescriptionAttributeInfo? attributeOnEnum,
        ErrorDescriptionAttributeInfo? attributeOnEnumMember,
        string errorCode
    )
    {
        var helpLink = attributeOnEnumMember?.HelpLink ?? attributeOnEnum?.HelpLink;

        if (!string.IsNullOrWhiteSpace(helpLink) && helpLink!.Contains("{0}"))
        {
            helpLink = string.Format(CultureInfo.InvariantCulture, helpLink, errorCode);
        }

        return helpLink;
    }

    private static string GetPrefixedErrorCode(
        ErrorDescriptionAttributeInfo? attributeOnEnum,
        ErrorDescriptionAttributeInfo? attributeOnEnumMember,
        string errorCodeEnumValue
    )
    {
        if (string.IsNullOrWhiteSpace(attributeOnEnum?.Prefix) &&
            string.IsNullOrWhiteSpace(attributeOnEnumMember?.Prefix))
        {
            throw new InvalidOperationException(
                "No error prefix set. Provide ErrorDescriptionAttribute on enum or its value with Prefix property.");
        }

        var prefix = attributeOnEnumMember?.Prefix ?? attributeOnEnum?.Prefix;
        var errorCodeLength = attributeOnEnumMember?.ErrorCodeLength ?? attributeOnEnum?.ErrorCodeLength;

        var code = errorCodeEnumValue.PadLeft(errorCodeLength.GetValueOrDefault(5), '0');

        return $"{prefix}{code}";
    }
}