using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorRoutesGenerator.EqualityComparer;

internal sealed class QueryParameterComparer : IEqualityComparer<KeyValuePair<string, TypeSyntax>>
{
    public static QueryParameterComparer Default => new();

    public bool Equals(KeyValuePair<string, TypeSyntax> x, KeyValuePair<string, TypeSyntax> y)
    {
        return x.Key.Equals(y.Key);
    }

    public int GetHashCode(KeyValuePair<string, TypeSyntax> obj)
    {
        return obj.Key.GetHashCode();
    }
}
