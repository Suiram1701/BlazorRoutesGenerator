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
        if (!options.TryGetValue($"{prefix}_Namespace", out string? @namespace))
        {
            if (!options.TryGetValue("build_property.rootnamespace", out @namespace))     // Set by MSBuild -> Can't be false
                throw new InvalidOperationException("Unable to determine root namespace of the application.");
        }
        
        if (!options.TryGetValue($"{prefix}_ClassName", out string? className))
            className = "Routes";

        // Dictionary<string, string> overrideNames = [];
        // if (options.TryGetValue($"{prefix}_NameOverrides", out string? overrideStr))
        // {
        //     foreach (string line in overrideStr.Trim().Split('\n', '\r'))
        //     {
        //         string[] parts = line.Split('=');
        //         if (parts.Length != 2)
        //             throw new InvalidOperationException("Assignment from full qualified name to method name was expected. See https://github.com/Suiram1701/BlazorRoutesGenerator?tab=readme-ov-file#configuration");
        //         
        //         overrideNames.Add(parts[0].Trim(), parts[1].Trim());
        //     }
        // }
        
        return new GeneratorConfig(@namespace, className, new AnalyzerOptionsDictionary(options));
    }
}
