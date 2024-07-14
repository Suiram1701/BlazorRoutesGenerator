using Microsoft.AspNetCore.Components;

namespace SampleApp.Components.Pages;

[Route("/weather/{number:int}")]
public sealed partial class Weather
{
    [SupplyParameterFromQuery(Name = "date")]
    public DateTime? Date { get; set; }
}
