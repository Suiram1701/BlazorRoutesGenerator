using Microsoft.AspNetCore.Components;

namespace SampleApp.Components.Pages;

[Route("/weather/{value:datetime}")]
public sealed partial class Weather
{
    [Parameter]
    public DateTime Value { get; set; }

    [SupplyParameterFromQuery(Name = "date")]
    public DateTime? Date { get; set; }

    [SupplyParameterFromQuery(Name = "date")]
    public DateTime? Date2 { get; set; }
}
