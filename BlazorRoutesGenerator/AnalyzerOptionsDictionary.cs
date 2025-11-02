using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BlazorRoutesGenerator;

public class AnalyzerOptionsDictionary : IReadOnlyDictionary<string, string>
{
    private readonly AnalyzerConfigOptions _options;
    
    public AnalyzerOptionsDictionary(AnalyzerConfigOptions options)
    {
        _options = options;
    }
    
    public int Count => _options.Keys.Count();
    
    public IEnumerable<string> Keys => _options.Keys;

    public IEnumerable<string> Values =>
        throw new NotImplementedException("AnalyzerConfigOptions does not support enumerating values.");

    
    public bool ContainsKey(string key) => _options.Keys.Contains(key);

    public bool TryGetValue(string key, out string value)
    {
        return key.StartsWith("build_property")
            ? throw new ArgumentException("Not allowed to access other properties", nameof(key))
            : _options.TryGetValue(key, out value!);
    }

    public string this[string key] => TryGetValue(key, out string value) ? value : string.Empty;
    
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() =>
        throw new NotImplementedException("AnalyzerConfigOptions does not support enumerating value");

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

}