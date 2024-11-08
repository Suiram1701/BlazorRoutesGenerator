# BlazorRoutesGenerator ![NuGet Version](https://img.shields.io/nuget/vpre/Suiram1.BlazorRoutesGenerator) ![NuGet Downloads](https://img.shields.io/nuget/dt/Suiram1.BlazorRoutesGenerator)

This library provides a code generator that detects routable components in .cs or in .razor files and create har-coded routes based on them.
The code generator will be recognize [@page-directive](https://learn.microsoft.com/aspnet/core/mvc/views/razor#page) in .razor files and the [[Route("/")]](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.components.routeattribute)-attribute in .cs files.\
For each route defined by one of the mentioned ways the generator will generate two method. The first one will generate an relative uri to the page and the second one is an extensions of [NavigationManager](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.components.navigationmanager) that will navigate to this page. The extension has the same method name as the uri creator method but with the prefix NavigateTo.

## Route parameters

The generated methods supports route parameters of every by ASP.NET Core [supported route parameter](https://learn.microsoft.com/aspnet/core/blazor/fundamentals/routing#route-constraints). A recognized route parameters gets in both methods of his route a parameter were you can specify this parameter.

## Query parameters

The generated methods supports query parameter as well. Query parameters of a page are recognized when a property has the [[SupplyParameterFromQuery]](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.components.supplyparameterfromqueryattribute)-attribute.\
The name of the query parameter can provided using the [Name](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.components.supplyparameterfromqueryattribute.name#microsoft-aspnetcore-components-supplyparameterfromqueryattribute-name)-property of the attribute. If this property isn't specified the name of the property will be used.\
The type of the query parameter is determined by the type of the property. Complex query parameters won't get serialized and only the [ToString](https://learn.microsoft.com/dotnet/api/system.object.tostring) get called.

## Generated methods

Every method generated is inside of a static class that is by default inside of the applications root namespace and is called **Routes**. The name of the methods is determined by the class/file name. If two classes/files has the same names as little as possible from the namespace is used to clearly identify the page.\
For example the page *../Pages/Index.razor* has the method name **Index**. But when there are two pages *../Pages/SectionA/Index.razor* and *../Pages/SectionB/Index.razor* the method names of these pages are **SectionAIndex** and **SectionBIndex**. In .razor files is for that purpose also the [@namespace-directive](https://learn.microsoft.com/aspnet/core/mvc/views/razor#namespace) supported.

## Configuration

Some behaviors of the generator can be configured using a .json file. Using a configuration file you can change the namespace and the class name of the class containing the routes.
You can also change the name of the methods that are responsable for a routable component.

The file have to be named *BlazorRoutesGenerator.config.json* and have to be included in the .csproj file of the project. You have add the following part to the .csproj file:
```
<ItemGroup>
  <AdditionalFiles Include="BlazorRoutesGenerator.config.json" />
</ItemGroup>
```
If multiple *BlazorRoutesGenerator.config.json* are specified every file will be ignored.

The *BlazorRoutesGenerator.config.json* have to has the following structure:
```
{
  "namespace": "MyApplication",
  "className": "Routes",
  "namesOverwrites": {
    "SampleApp.Components.Pages.Weather": "RenamedPage",
    "SampleApp.Components.Pages.Something.Page": "AnotherRenamedPage"
  }
}
```
If you don't want to change a settings you can remove the json property and the default value will be used. The default namespace is you applications root namespace and the default class name is **Routes**.
An invalid configuration or doubled names for example if rename a page to **PageA** but another page is by default named **PageA** the configuration will be ignored.

## Limitations

Query parameters are __only in .cs files__ supported. Inside of .razor files they won't be detected. In case that you want to use .razor files for HTML + CS and query parameters you can define the page in partial classes ([more about that here](https://www.pragimtech.com/blog/blazor/Split-razor-component/)) and define the query parameters there.
