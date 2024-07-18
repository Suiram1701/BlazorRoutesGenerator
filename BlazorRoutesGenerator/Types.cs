using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorRoutesGenerator;

internal static class Types
{
    private const string _globalKeyword = "global::";

    public const string ExcludeFromCodeCoverageAttributeType = _globalKeyword + "System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage";

    public const string GeneratedCodeAttributeType = _globalKeyword + "System.CodeDom.Compiler.GeneratedCodeAttribute";

    public const string CultureInfoType = _globalKeyword + "System.Globalization.CultureInfo";

    public const string IEnumerableStringType = _globalKeyword + "System.Collections.Generic.IEnumerable<string>";

    public const string IEnumerableKvStringStringType = $"{_globalKeyword}System.Collections.Generic.IEnumerable<{KvStringStringType}>";

    public const string KvStringStringType = _globalKeyword + "System.Collections.Generic.KeyValuePair<string, string?>";

    public const string QueryStringType = _globalKeyword + "Microsoft.AspNetCore.Http.QueryString";

    public const string NavigationManagerType = _globalKeyword + "Microsoft.AspNetCore.Components.NavigationManager";
}
