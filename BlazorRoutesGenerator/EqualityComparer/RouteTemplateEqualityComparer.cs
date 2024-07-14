using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlazorRoutesGenerator.EqualityComparer;

internal class RouteTemplateEqualityComparer : IEqualityComparer<RouteTemplate>
{
    public static RouteTemplateEqualityComparer Instance => new();

    public bool Equals(RouteTemplate x, RouteTemplate y)
    {
        if (x.Template == y.Template)
        {
            return true;
        }

        return x.Parameters.SequenceEqual(y.Parameters, RouteParameterEqualityComparer.Instance);
    }

    public int GetHashCode(RouteTemplate obj)
    {
        return obj.GetHashCode();
    }
}
