﻿using BlazorRoutesGenerator;
using BlazorRoutesGenerator.EqualityComparer;
using BlazorRoutesGenerator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using static BlazorRoutesGenerator.Types;

namespace GeneratedBlazorRoutes
{
    [Generator]
    public class RoutesGenerator : IIncrementalGenerator
    {
        private const string _routeAttributeQualifier = "Microsoft.AspNetCore.Components.RouteAttribute";
        private const string _routeAttributeClassName = "RouteAttribute";
        private const string _queryAttributeClassName = "SupplyParameterFromQueryAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValueProvider<ImmutableArray<(ClassDeclarationSyntax, ImmutableArray<string>)>> csFileProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: _routeAttributeQualifier,
                predicate: (node, _) => node is ClassDeclarationSyntax,
                transform: (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Where(m => m.Item1 != null && m.Item2.Any())
                .Collect();

            IncrementalValueProvider<ImmutableArray<(ClassDeclarationSyntax, ImmutableArray<string>)>> razorFileProvider = context.AdditionalTextsProvider
                .Where(file => file.Path.EndsWith(".razor"))
                .Select(ParseRazorPageForGeneration)
                .Where(m => m.Item1 != null && m.Item2.Any())
                .Collect();

            IncrementalValueProvider<(ImmutableArray<(ClassDeclarationSyntax, ImmutableArray<string>)>, ImmutableArray<(ClassDeclarationSyntax, ImmutableArray<string>)>)> combinedProvider = csFileProvider.Combine(razorFileProvider);

            IncrementalValueProvider<(Compilation, (ImmutableArray<(ClassDeclarationSyntax, ImmutableArray<string>)>, ImmutableArray<(ClassDeclarationSyntax, ImmutableArray<string>)>))> resultProvider = context.CompilationProvider.Combine(combinedProvider);
            context.RegisterSourceOutput(resultProvider, (spc, source) =>
            {
                ImmutableArray<(ClassDeclarationSyntax, ImmutableArray<string>)> values = [.. source.Item2.Item1, .. source.Item2.Item2];
                Execute(source.Item1, spc, values);
            });
        }

        private static (ClassDeclarationSyntax, ImmutableArray<string>) GetSemanticTargetForGeneration(GeneratorAttributeSyntaxContext context)
        {
            if (context.TargetNode is not ClassDeclarationSyntax classDeclaration)
            {
                return (null, [])!;
            }
            
            ImmutableArray<string> pageRoutes = context.Attributes
                .Where(attribute => attribute.AttributeClass?.Name == _routeAttributeClassName)
                .Select(attributeData =>
                {
                    TypedConstant routeParameter = attributeData.ConstructorArguments[0];
                    if (routeParameter.Kind == TypedConstantKind.Primitive)
                    {
                        return routeParameter.Value?.ToString() ?? string.Empty;
                    }

                    return string.Empty;
                })
                .Where(routeTemplate => routeTemplate != null)
                .Distinct()
                .ToImmutableArray();

            return (classDeclaration, pageRoutes);
        }

        private static (ClassDeclarationSyntax, ImmutableArray<string>) ParseRazorPageForGeneration(AdditionalText file, CancellationToken ct)
        {
            SourceText? content = file.GetText(ct);
            if (content == null)
            {
                return (null, [])!;
            }

            ImmutableArray<string> routes = content.Lines
                .Select(line => Regex.Match(line.ToString(), "^\\s*@page\\s\"(.+?)\"", RegexOptions.Singleline).Groups[1].Value)
                .Where(route => !string.IsNullOrEmpty(route))
                .Distinct()
                .ToImmutableArray();

            string pageIdentifier = GetRazorPageIdentifier(file, content);
            return (SyntaxFactory.ClassDeclaration(pageIdentifier), routes);
        }

        private static void Execute(Compilation compilation, SourceProductionContext context, ImmutableArray<(ClassDeclarationSyntax, ImmutableArray<string>)> pages)
        {
            IEnumerable<PageModel> pageModels = GetPageModels(compilation, pages);

            string classNamespace = compilation.Assembly.Name;
            Type generatorType = typeof(RoutesGenerator);

            StringBuilder sourceBuilder = new();
            sourceBuilder
                .AppendLine("// <auto-generated/>")
                .AppendLine("#nullable enable")
                .AppendLine()
                .Append($"namespace ").AppendLine(classNamespace)
                .AppendLine("{")
                .Append("\t[").Append(GeneratedCodeAttributeType).Append("(\"").Append(generatorType.FullName).Append("\", \"").Append(generatorType.Assembly.GetName().Version).AppendLine("\")]")
                .Append("\t[").Append(ExcludeFromCodeCoverageAttributeType).AppendLine("]")
                .AppendLine("\tpublic static class Routes")
                .AppendLine("\t{");

            IEnumerable<string> allRoutes = pageModels.SelectMany(page => page.RouteTemplates.OrderBy(route => route.Parameters.Count).Select(route => route.Template));
            BuildAllRoutes(sourceBuilder, allRoutes);

            sourceBuilder.AppendLine();

            BuildAllMethods(sourceBuilder, pageModels);

            sourceBuilder
                .AppendLine("\t}")
                .AppendLine("}");

            context.AddSource("Routes.g.cs", sourceBuilder.ToString());
        }

        private static string GetRazorPageIdentifier(AdditionalText file, SourceText text)
        {
            foreach (TextLine line in text.Lines)
            {
                Match match = Regex.Match(line.ToString(), "^\\s*@namespace\\s(?:([a-zA-Z]\\w*)\\.?)+", RegexOptions.Singleline);
                if (match.Success)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file.Path);
                    return string.Join(".", [.. match.Groups[1].Captures, fileName]);
                }
            }

            string filePath = file.Path.Substring(0, file.Path.LastIndexOf(".razor"));
            string pathWithoutDrive = filePath.Substring(filePath.IndexOf(Path.VolumeSeparatorChar) + 2);
            return pathWithoutDrive.Replace(Path.DirectorySeparatorChar, '.');
        }

        private static ImmutableArray<PageModel> GetPageModels(Compilation compilation, ImmutableArray<(ClassDeclarationSyntax, ImmutableArray<string>)> pages)
        {
            Dictionary<string, (List<RouteTemplate>, List<KeyValuePair<string, TypeSyntax>>)> pageModels = [];
            foreach ((ClassDeclarationSyntax classDeclaration, ImmutableArray<string> routes) in pages)
            {
                IEnumerable<RouteTemplate> routeTemplates = routes.Select(RouteTemplate.ParseRouteTemplate);

                IEnumerable<KeyValuePair<string, TypeSyntax>> queryParams = GetQueryParameters(compilation, classDeclaration);

                INamedTypeSymbol? symbol = compilation.SyntaxTrees.Contains(classDeclaration.SyntaxTree)
                    ? compilation.GetSemanticModel(classDeclaration.SyntaxTree).GetDeclaredSymbol(classDeclaration)
                    : null;
                string name = symbol is not null
                    ? symbol.GetFullQualifiedName()
                    : classDeclaration.Identifier.Text;

                IEnumerable<string> namespaces = name.Split('.');

                int nameIndex = namespaces.ToList().IndexOf(compilation.Assembly.Name);
                string typeQualifier = string.Join(".", namespaces.Skip(nameIndex));

                if (pageModels.ContainsKey(typeQualifier))
                {
                    pageModels[typeQualifier].Item1.AddRange(routeTemplates);
                    pageModels[typeQualifier].Item2.AddRange(queryParams);

                    continue;
                }

                pageModels.Add(typeQualifier, (routeTemplates.ToList(), queryParams.ToList()));
            }

            return pageModels
                .Select(kv => new PageModel(
                    name: kv.Key,
                    routeTemplates: [..kv.Value.Item1.Distinct(RouteTemplateComparer.Default)],
                    queryParameters: kv.Value.Item2.Distinct(QueryParameterComparer.Default).ToImmutableDictionary()))
                .ToImmutableArray();
        }

        private static IEnumerable<KeyValuePair<string, TypeSyntax>> GetQueryParameters(Compilation compilation, ClassDeclarationSyntax classDeclaration)
        {
            foreach (PropertyDeclarationSyntax property in classDeclaration.Members.OfType<PropertyDeclarationSyntax>())
            {
                IPropertySymbol? symbol = compilation.GetSemanticModel(property.SyntaxTree).GetDeclaredSymbol(property);
                if (symbol is null)
                {
                    continue;
                }

                AttributeData? queryAttribute = symbol.GetAttributes().FirstOrDefault(attribute => attribute.AttributeClass?.Name == _queryAttributeClassName);
                if (queryAttribute is not null)
                {
                    string? queryParamName = null;

                    if (queryAttribute.NamedArguments.Count() >= 1)
                    {
                        TypedConstant queryParameter = queryAttribute.NamedArguments.First(kv => kv.Key == "Name").Value;
                        queryParamName = queryParameter.Value as string;

                        if (queryParameter.Kind != TypedConstantKind.Primitive || string.IsNullOrEmpty(queryParamName))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        queryParamName = property.Identifier.Text;
                    }

                    yield return new KeyValuePair<string, TypeSyntax>(queryParamName!, property.Type);
                }
            }
        }

        private static void BuildAllRoutes(StringBuilder builder, IEnumerable<string> routes)
        {
            builder
                .AddMemberDocumentation("An enumerable that contains every by the generator recognized route template.", null, null, [])
                .Append("\t\tpublic static ").Append(IEnumerableStringType).AppendLine(" All => new[]")
                .AppendLine("\t\t{");

            int i = routes.Count();
            foreach (string route in routes)
            {
                builder.Append("\t\t\t\"").Append(route).Append("\"");

                if (i > 1)
                {
                    builder.Append(",");
                }
                builder.AppendLine();

                i--;
            }

            builder.AppendLine("\t\t};");
        }

        private static void BuildAllMethods(StringBuilder builder, IEnumerable<PageModel> pageModels)
        {
            IEnumerable<string> methodNames = Helpers.FindMinimalSegments(pageModels.Select(p => p.Name)).ToArray();

            int i = pageModels.Count();
            foreach (PageModel pageModel in pageModels)
            {
                string methodName = methodNames.FirstOrDefault(pageModel.Name.EndsWith);
                if (methodName == default)
                {
                    methodName = pageModel.Name;
                }
                methodName = methodName.Replace(".", string.Empty);

                foreach (RouteTemplate template in pageModel.RouteTemplates.OrderBy(route => route.Parameters.Count))
                {
                    IEnumerable<KeyValuePair<string, TypeSyntax>> parameters = template.Parameters.Concat(pageModel.QueryParameters);

                    IEnumerable<KeyValuePair<string, string>> parametersDoc = parameters.Select(parameter =>
                    {
                        string name = parameter.Key;

                        string paramType = template.Parameters.ContainsKey(name)
                            ? "route"
                            : "query";
                        string doc = $"The value of the {paramType} parameter <c>{name}</c>";

                        return new KeyValuePair<string, string>(name, doc);
                    });

                    builder
                        .AddMemberDocumentation(
                            summary: $"Gets the relative uri of the page <c>{pageModel.Name}</c> with the route template of <c>{template.Template}</c>.",
                            remarks: string.Empty,
                            returns: "The generated relative uri.",
                            parameters: parametersDoc)
                        .AddMethod("string", methodName, parameters, methodBuilder => BuildPageMethod(methodBuilder, pageModel, template)).AppendLine();

                    IEnumerable<KeyValuePair<string, string>> navigationParametersDoc =
                    [
                        new KeyValuePair<string, string>("forceLoad", "If true, bypasses client-side routing and reloads the page from the server."),
                        new KeyValuePair<string, string>("replace", "If true, replaces the current entry in the history stack.")
                    ];

                    builder
                        .AddMemberDocumentation(
                            summary: $"Navigates to the uri generated by <see cref=\"{methodName}\"/>.",
                            remarks: string.Empty,
                            returns: string.Empty,
                            parameters: parametersDoc.Concat(navigationParametersDoc))
                        .AddMethod("void", $"NavigateTo{methodName}", parameters, methodBuilder =>
                    {
                        methodBuilder
                            .Append("\t\t\tstring uri = ").CallMethod(methodName, parameters.Select(kv => kv.Key)).AppendLine(";")
                            .Append("\t\t\t").CallMethod("navigationManager.NavigateTo", ["uri", "forceLoad", "replace"]).AppendLine(";");
                    }, navExtension: true);

                    if (i > 1)
                    {
                        builder.AppendLine();
                    }
                }

                i--;
            }
        }

        private static void BuildPageMethod(StringBuilder methodBuilder, PageModel pageModel, RouteTemplate template)
        {
            string path = template.Template;
            if (template.Parameters.Any())
            {
                foreach (Match match in RouteTemplate.ParametersRegex.Matches(template.Template))
                {
                    string name = match.Groups[1].Value;

                    string? typeName = null;
                    if (match.Groups.Count >= 2)
                    {
                        typeName = match.Groups[2].Value;
                    }

                    path = path.Replace(match.Value, $"{{{name}{GetParameterFormatter(typeName)}}}");
                }
            }

            if (pageModel.QueryParameters.Any())
            {
                methodBuilder
                    .Append("\t\t\tstring routePath = ").Append(template.Parameters.Any() ? "$" : string.Empty).Append("\"").Append(path).AppendLine("\";")
                    .AppendLine()
                    .Append("\t\t\t").Append(IEnumerableKvStringStringType).AppendLine(" queryParameters = new[]")
                    .Append("\t\t\t{");

                int i = pageModel.QueryParameters.Count();
                foreach (KeyValuePair<string, TypeSyntax> queryParam in pageModel.QueryParameters)
                {
                    methodBuilder
                        .AppendLine()
                        .Append("\t\t\t\tnew ").Append(KvStringStringType).Append("(\"").Append(queryParam.Key).Append("\", ").Append(queryParam.Key);

                    if (queryParam.Value is NullableTypeSyntax)
                    {
                        methodBuilder.Append("?");
                    }
                    methodBuilder.Append(".ToString())");

                    if (i > 1)
                    {
                        methodBuilder.Append(",");
                    }

                    i--;
                }
                methodBuilder
                    .AppendLine()
                    .AppendLine("\t\t\t};")
                    .Append("\t\t\t").Append(QueryStringType).Append(" queryString = ").Append(QueryStringType).AppendLine(".Create(queryParameters.Where(p => p.Value != null));")
                    .AppendLine("\t\t\tif (queryString.HasValue)")
                    .AppendLine("\t\t\t{")
                    .AppendLine("\t\t\t\troutePath += queryString.Value;")
                    .AppendLine("\t\t\t}")
                    .AppendLine("\t\t\treturn routePath;");

            }
            else
            {
                methodBuilder.Append("\t\t\treturn ").Append(template.Parameters.Any() ? "$" : string.Empty).Append("\"").Append(path).AppendLine("\";");
            }
        }

        private static string GetParameterFormatter(string? typeName) 
        {
            string formatterExpression = typeName switch
            {
                "datetime" => "\"s\"",
                "decimal" or
                "double" or
                "float" => $"{CultureInfoType}.InvariantCulture",
                _ => string.Empty
            };
            return $".ToString({formatterExpression})";
        }
    }
}
