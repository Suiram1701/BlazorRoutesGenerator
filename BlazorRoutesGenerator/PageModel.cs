using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace BlazorRoutesGenerator;

internal class PageModel(string name, ImmutableArray<RouteTemplate> routeTemplates, ImmutableDictionary<string, TypeSyntax> queryParameters)
{
    public string Name { get; set; } = name;

    public ImmutableArray<RouteTemplate> RouteTemplates { get; set; } = routeTemplates;

    public ImmutableDictionary<string, TypeSyntax> QueryParameters { get; set; } = queryParameters;
}