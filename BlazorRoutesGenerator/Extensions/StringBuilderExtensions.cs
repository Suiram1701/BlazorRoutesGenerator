using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlazorRoutesGenerator.Extensions;

internal static class StringBuilderExtensions
{
    public static StringBuilder AddMethod(this StringBuilder builder, string returnType, string methodName, IEnumerable<KeyValuePair<string, TypeSyntax>> parameters, Action<StringBuilder> bodyBuilder)
    {
        builder.Append("\t\tpublic static ").Append(returnType).Append(' ').Append(methodName).Append("(").AddParameters(parameters).AppendLine(")")
            .AppendLine("\t\t{");

        bodyBuilder.Invoke(builder);
        return builder.AppendLine("\t\t}");
    }

    public static StringBuilder AddParameters(this StringBuilder builder, IEnumerable<KeyValuePair<string, TypeSyntax>> parameters)
    {
        int i = parameters.Count();
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

        return builder;
    }
}
