using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BlazorRoutesGenerator.Models;

public class GeneratorConfig
{
    public string Namespace { get; }
    
    public string ClassName { get; }

    public IReadOnlyDictionary<string, string> OverrideNames { get; }

    private GeneratorConfig(string @namespace, string className, IReadOnlyDictionary<string, string> overrideNames)
    {
        Namespace = @namespace;
        ClassName = className;
        OverrideNames = overrideNames;
    }
    
    public static GeneratorConfig LoadFromGlobalOptions(AnalyzerConfigOptions options, string prefix)
    {
        bool namespaceSet = options.TryGetValue($"{prefix}_Namespace", out string? @namespace);
        if (!namespaceSet || string.IsNullOrWhiteSpace(@namespace))
        {
            if (!options.TryGetValue("build_property.rootnamespace", out @namespace))     // Set by MSBuild -> Can't be false
                throw new InvalidOperationException("Unable to determine root namespace of the application.");
        }

        bool classSet = options.TryGetValue($"{prefix}_ClassName", out string? className);
        if (!classSet || string.IsNullOrWhiteSpace(className))
            className = "Routes";
        
        return new GeneratorConfig(@namespace!, className!, new AnalyzerOptionsDictionary(options));
    }
}
