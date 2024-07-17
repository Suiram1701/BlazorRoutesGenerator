using Microsoft.AspNetCore.Components;

namespace SampleApp.Components.Pages;

[Route("/weather/{value}")]
public sealed partial class Weather
{
    [Parameter]
    public string Value { get; set; }
}
