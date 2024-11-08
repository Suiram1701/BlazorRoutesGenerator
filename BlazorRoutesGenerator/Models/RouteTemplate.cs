﻿using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace BlazorRoutesGenerator.Models;

internal class RouteTemplate(string template, ImmutableDictionary<string, TypeSyntax> parameters)
{
    public static Regex ParametersRegex => new("{([a-zA-Z][a-zA-Z0-9]*)(?::([a-z]+))?}", RegexOptions.Singleline | RegexOptions.Compiled);

    public string Template { get; set; } = template;

    public ImmutableDictionary<string, TypeSyntax> Parameters { get; set; } = parameters;

    public static RouteTemplate ParseRouteTemplate(string template)
    {
        Dictionary<string, TypeSyntax> parameters = [];
        foreach (Match match in ParametersRegex.Matches(template))
        {
            if (!match.Success)
            {
                continue;
            }

            string name = match.Groups[1].Value;
            string typeName = match.Groups[2].Value;

            Type? type = typeName switch
            {
                "" => typeof(string),
                "bool" => typeof(bool),
                "datetime" => typeof(DateTime),
                "decimal" => typeof(decimal),
                "double" => typeof(double),
                "float" => typeof(float),
                "guid" => typeof(Guid),
                "int" => typeof(int),
                "long" => typeof(long),
                _ => null
            };

            if (type is null)
            {
                continue;
            }

            TypeSyntax typeSyntax = SyntaxFactory.ParseTypeName(type.FullName);
            parameters.Add(name, typeSyntax);
        }

        return new(template, parameters.ToImmutableDictionary());
    }
}
