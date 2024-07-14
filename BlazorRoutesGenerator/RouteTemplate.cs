using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace BlazorRoutesGenerator;

internal class RouteTemplate(string template, ImmutableDictionary<string, TypeSyntax> parameters)
{
    public string Template { get; set; } = template;

    public ImmutableDictionary<string, TypeSyntax> Parameters { get; set; } = parameters;

    public static RouteTemplate ParseRouteTemplate(string template)
    {
        Dictionary<string, TypeSyntax> parameters = [];
        foreach (Match match in Regex.Matches(template, "{([a-zA-Z][a-zA-Z0-9]*)(?::([a-z]+))?}"))
        {
            if (!match.Success)
            {
                continue;
            }

            string name = match.Groups[1].Value;
            string? typeName = match.Groups[2].Value;

            Type type = typeName switch
            {
                null => typeof(string),
                "bool" => typeof(bool),
                "datetime" => typeof(DateTime),
                "decimal" => typeof(decimal),
                "double" => typeof(double),
                "float" => typeof(float),
                "guid" => typeof(Guid),
                "int" => typeof(int),
                "long" => typeof(long),
                _ => throw new NotImplementedException()
            };
            TypeSyntax typeSyntax = SyntaxFactory.ParseTypeName(type.FullName);

            parameters.Add(name, typeSyntax);
        }

        return new(template, parameters.ToImmutableDictionary());
    }
}
