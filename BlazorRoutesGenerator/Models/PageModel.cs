using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace BlazorRoutesGenerator.Models;

internal class PageModel(string name, ImmutableArray<RouteTemplate> routeTemplates, IEnumerable<KeyValuePair<string, TypeSyntax>> queryParameters)
{
    public string Name { get; set; } = name;

    public ImmutableArray<RouteTemplate> RouteTemplates { get; set; } = routeTemplates;

    public IEnumerable<KeyValuePair<string, TypeSyntax>> QueryParameters { get; set; } = queryParameters;
}