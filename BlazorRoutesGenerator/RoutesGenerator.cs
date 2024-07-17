using BlazorRoutesGenerator;
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

        private static (ClassDeclarationSyntax, ImmutableArray<string>) ParseRazorPageForGeneration(AdditionalText text, CancellationToken ct)
        {
            SourceText? content = text.GetText(ct);
            if (content == null)
            {
                return (null, [])!;
            }

            ImmutableArray<string> routes = content.Lines
                .Select(line => Regex.Match(line.ToString(), "@page\\s\"(.+?)\"").Groups[1].Value)
                .Where(route => !string.IsNullOrEmpty(route))
                .Distinct()
                .ToImmutableArray();

            string filePath = text.Path.Substring(0, text.Path.LastIndexOf(".razor"));
            string pathWithOutDrive = filePath.Substring(filePath.IndexOf(Path.VolumeSeparatorChar) + 2);

            return (SyntaxFactory.ClassDeclaration(pathWithOutDrive.Replace(Path.DirectorySeparatorChar, '.')), routes);
        }

        private static void Execute(Compilation compilation, SourceProductionContext context, ImmutableArray<(ClassDeclarationSyntax, ImmutableArray<string>)> pages)
        {
            IEnumerable<PageModel> pageModels = GetPageModels(compilation, pages);

            string classNamespace = compilation.Assembly.Name;
            AssemblyName generatorAssemblyName = typeof(RoutesGenerator).Assembly.GetName();

            StringBuilder sourceBuilder = new();
            sourceBuilder
                .AppendLine("// <auto-generated />")
                .AppendLine("#nullable enable")
                .AppendLine()
                .Append($"namespace ").AppendLine(classNamespace)
                .AppendLine("{")
                .Append("\t[").Append(GeneratedCodeAttributeType).Append("(\"").Append(generatorAssemblyName.Name).Append("\", \"").Append(generatorAssemblyName.Version).AppendLine("\")]")
                .AppendLine("\tpublic static class Routes")
                .AppendLine("\t{");

            IEnumerable<string> allRoutes = pageModels.OrderBy(page => page.Name).SelectMany(page => page.RouteTemplates.OrderBy(route => route.Parameters.Count).Select(route => route.Template));
            BuildAllRoutes(sourceBuilder, allRoutes);

            sourceBuilder.AppendLine();

            foreach (PageModel pageModel in pageModels.OrderBy(page => page.Name))
            {
                string methodName = pageModel.Name.Split('.').Last();

                int i = pageModels.Count();
                foreach (RouteTemplate template in pageModel.RouteTemplates.OrderBy(route => route.Parameters.Count))
                {
                    IEnumerable<KeyValuePair<string, TypeSyntax>> parameters = template.Parameters.Concat(pageModel.QueryParameters);

                    sourceBuilder.AddMethod("string", methodName, parameters, methodBuilder =>
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

                        methodBuilder
                            .Append("\t\t\tstring routePath = ").Append(template.Parameters.Any() ? "$" : string.Empty).Append("\"").Append(path).AppendLine("\";")
                            .AppendLine();

                        if (pageModel.QueryParameters.Any())
                        {
                            methodBuilder
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
                                .AppendLine();
                        }

                        methodBuilder.AppendLine("\t\t\treturn routePath;");
                    });

                    sourceBuilder.AddMethod("void", $"NavigateTo{methodName}", parameters, methodBuilder =>
                    {
                        sourceBuilder
                            .Append("\t\t\tstring uri = ").CallMethod(methodName, parameters.Select(kv => kv.Key)).AppendLine(";")
                            .Append("\t\t\t").CallMethod("navigationManager.NavigateTo", ["uri", "forceLoad", "replace"]).AppendLine(";");
                    }, navExtension: true);

                    if (i > 1)
                    {
                        sourceBuilder.AppendLine();
                    }

                    i--;
                }
            }

            sourceBuilder
                .AppendLine("\t}")
                .AppendLine("}");

            context.AddSource("Routes.g.cs", sourceBuilder.ToString());
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
                    routeTemplates: [..kv.Value.Item1],
                    queryParameters: kv.Value.Item2.ToImmutableDictionary()))
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
                    TypedConstant queryParameter = queryAttribute.NamedArguments.First(kv => kv.Key == "Name").Value;
                    if (queryParameter.Kind != TypedConstantKind.Primitive || queryParameter.Value is not string queryParamName)
                    {
                        continue;
                    }

                    yield return new KeyValuePair<string, TypeSyntax>(queryParamName, property.Type);
                }
            }
        }

        private static void BuildAllRoutes(StringBuilder builder, IEnumerable<string> routes)
        {
            builder
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
