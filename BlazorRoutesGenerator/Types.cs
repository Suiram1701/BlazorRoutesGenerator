using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorRoutesGenerator;

internal static class Types
{
    public const string GlobalKeyword = "global::";

    public const string GeneratedCodeAttributeType = GlobalKeyword + "System.CodeDom.Compiler.GeneratedCodeAttribute";

    public const string CultureInfoType = GlobalKeyword + "System.Globalization.CultureInfo";

    public const string IEnumerableStringType = GlobalKeyword + "System.Collections.Generic.IEnumerable<string>";

    public const string IEnumerableKvStringStringType = $"{GlobalKeyword}System.Collections.Generic.IEnumerable<{KvStringStringType}>";

    public const string KvStringStringType = GlobalKeyword + "System.Collections.Generic.KeyValuePair<string, string?>";

    public const string QueryStringType = GlobalKeyword + "Microsoft.AspNetCore.Http.QueryString";
}
