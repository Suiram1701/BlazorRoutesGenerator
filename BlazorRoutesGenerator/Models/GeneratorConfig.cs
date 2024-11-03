using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace BlazorRoutesGenerator.Models;

public class GeneratorConfig
{
    [JsonProperty("namespace")]
    public string? Namespace { get; set; }

    [JsonProperty("className")]
    public string? ClassName { get; set; }

    [JsonProperty("namesOverwrites")]
    public Dictionary<string, string> OverwriteNames { get; set; } = [];
}
