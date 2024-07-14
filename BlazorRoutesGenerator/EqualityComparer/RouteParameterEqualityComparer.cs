using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorRoutesGenerator.EqualityComparer;

internal class RouteParameterEqualityComparer : IEqualityComparer<KeyValuePair<string, TypeSyntax>>
{
    public static RouteParameterEqualityComparer Instance => new();

    public bool Equals(KeyValuePair<string, TypeSyntax> x, KeyValuePair<string, TypeSyntax> y)
    {
        return x.Key ==y.Key && x.Value.IsEquivalentTo(y.Value);
    }

    public int GetHashCode(KeyValuePair<string, TypeSyntax> obj)
    {
        return obj.GetHashCode();
    }
}
