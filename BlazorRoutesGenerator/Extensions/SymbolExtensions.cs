using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorRoutesGenerator.Extensions;

internal static class SymbolExtensions
{
    /// <summary>
    /// Get the full qualified name of a symbol without the assembly name.
    /// </summary>
    /// <param name="symbol">The symbol.</param>
    /// <returns>The name.</returns>
    public static string GetFullQualifiedName(this ISymbol symbol)
    {
        if (symbol.ContainingNamespace is null)
        {
            return symbol.Name;
        }

        return $"{GetFullQualifiedName(symbol.ContainingNamespace)}.{symbol.Name}".Trim('.');
    }
}
