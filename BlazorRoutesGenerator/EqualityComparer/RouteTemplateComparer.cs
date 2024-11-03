using BlazorRoutesGenerator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlazorRoutesGenerator.EqualityComparer;

internal sealed class RouteTemplateComparer : IEqualityComparer<RouteTemplate>
{
    public static RouteTemplateComparer Default => new();

    public bool Equals(RouteTemplate x, RouteTemplate y)
    {
        if (x.Template.Equals(y.Template))
        {
            return true;
        }

        return x.Parameters.Select(p => p.Key).SequenceEqual(y.Parameters.Select(p => p.Key));
    }

    public int GetHashCode(RouteTemplate obj)
    {
        return string.Concat(obj.Parameters.Select(p => p.Key)).GetHashCode();
    }
}
