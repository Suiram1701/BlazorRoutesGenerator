using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlazorRoutesGenerator.Extensions;

internal static class StringBuilderExtensions
{
    public static StringBuilder AddMethod(this StringBuilder builder, string returnType, string methodName, IEnumerable<KeyValuePair<string, TypeSyntax>> parameters, Action<StringBuilder> bodyBuilder, bool navExtension = false)
    {
        builder.Append("\t\tpublic static ").Append(returnType).Append(' ').Append(methodName).Append("(");

        int i = parameters.Count();

        if (navExtension)
        {
            builder.Append("this ").Append(Types.NavigationManagerType).Append(" navigationManager");

            if (i > 0)
            {
                builder.Append(", ");
            }
        }

        foreach (KeyValuePair<string, TypeSyntax> parameter in parameters.OrderBy(paramater => paramater.Value is NullableTypeSyntax))
        {
            builder.AppendFormat("{0} {1}", parameter.Value, parameter.Key);

            if (parameter.Value is NullableTypeSyntax)
            {
                builder.Append(" = null");
            }

            if (i > 1)
            {
                builder.Append(", ");
            }

            i--;
        }

        if (navExtension)
        {
            builder.Append(", bool forceLoad = false, bool replace = false");
        }

        builder
            .AppendLine(")")
            .AppendLine("\t\t{");

        bodyBuilder.Invoke(builder);

        return builder.AppendLine("\t\t}");
    }

    public static StringBuilder CallMethod(this StringBuilder builder, string methodName, IEnumerable<string> parameters)
    {
        builder.Append(methodName).Append("(");

        int i = parameters.Count();
        foreach (string parameter in parameters)
        {
            builder.Append(parameter);

            if (i > 1)
            {
                builder.Append(", ");
            }

            i--;
        }

        return builder.Append(")");
    }
}
