using PANApp.Models;
using PANApp.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace PANApp.Services.Implementations.LanguageAnalyzers;

public sealed class CSharpAvaloniaWpfLanguageAnalyzer : ILanguageAnalyzer
{
    private static readonly Regex XamlTypeReferenceRegex =
        new(@"\{x:(?:Type|Static)\s+(?<prefix>[\w\-]+):(?<type>[\w_][\w\d_]*)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Language => "C# Avalonia/WPF";

    public List<FileAnalysisResult> AnalyzeProject(string basePath)
    {
        if (!Directory.Exists(basePath))
            return new List<FileAnalysisResult>();

        var csFiles = Directory.EnumerateFiles(basePath, "*.cs", SearchOption.AllDirectories)
            .Where(IsValidCSharpFile)
            .ToList();

        var csResults = СSharpRoslynProjectGraphAnalyzer.AnalyzeCSharpFiles(basePath, csFiles);

        var knownProjectTypes = new HashSet<string>(
            csResults.SelectMany(x => x.DeclaredTypes)
                .Select(x => x.FullName).Where(x => !string.IsNullOrWhiteSpace(x)),
            StringComparer.OrdinalIgnoreCase
        );

        var xamlFiles = Directory.EnumerateFiles(basePath, "*.*", SearchOption.AllDirectories)
            .Where(IsValidXamlFile)
            .ToList();

        var xamlResults = new List<FileAnalysisResult>();

        foreach (var file in xamlFiles)
        {
            var dependencies = CollectXamlDependencies(file, knownProjectTypes).ToList();

            xamlResults.Add(new FileAnalysisResult
            {
                FilePath = file,
                RelativePath = Path.GetRelativePath(basePath, file).Replace('\\', '/'),
                DeclaredTypes = new List<DeclaredType>(),
                Dependencies = dependencies
            });
        }

        return csResults.Concat(xamlResults).ToList();
    }

    private static IEnumerable<string> CollectXamlDependencies(string filePath, HashSet<string> knownProjectTypes)
    {
        var dependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        XDocument document;
        try
        {
            document = XDocument.Load(filePath, LoadOptions.None);
        }
        catch
        {
            return dependencies;
        }

        var root = document.Root;
        if (root is null) return dependencies;

        XNamespace xamlNs = "http://schemas.microsoft.com/winfx/2006/xaml";

        var xClass = root.Attribute(xamlNs + "Class")?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(xClass) && knownProjectTypes.Contains(xClass))
            dependencies.Add(xClass);

        foreach (var element in root.DescendantsAndSelf())
        {
            var typeFromElement = ResolveClrTypeFromElement(element, knownProjectTypes);
            if (typeFromElement is not null)
                dependencies.Add(typeFromElement);

            foreach (var attribute in element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration) continue;

                foreach (var typeFromMarkup in ResolveMarkupTypeReferences(element, attribute.Value, knownProjectTypes))
                    dependencies.Add(typeFromMarkup);
            }
        }

        return dependencies;
    }

    private static string? ResolveClrTypeFromElement(XElement element, HashSet<string> knownProjectTypes)
    {
        if (!TryGetClrNamespace(element.Name.NamespaceName, out var clrNamespace)) return null;

        var candidate = $"{clrNamespace}.{element.Name.LocalName}";
        return knownProjectTypes.Contains(candidate) ? candidate : null;
    }

    private static IEnumerable<string> ResolveMarkupTypeReferences(XElement context, string value, HashSet<string> knownProjectTypes)
    {
        foreach (Match match in XamlTypeReferenceRegex.Matches(value))
        {
            var prefix = match.Groups["prefix"].Value;
            var typeName = match.Groups["type"].Value;

            var xmlNamespace = context.GetNamespaceOfPrefix(prefix)?.NamespaceName;
            if (string.IsNullOrWhiteSpace(xmlNamespace)) continue;

            if (!TryGetClrNamespace(xmlNamespace, out var clrNamespace)) continue;

            var candidate = $"{clrNamespace}.{typeName}";
            if (knownProjectTypes.Contains(candidate)) yield return candidate;
        }
    }

    private static bool TryGetClrNamespace(string xmlNamespace, out string clrNamespace)
    {
        const string prefix = "clr-namespace:";

        if (xmlNamespace.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            var rest = xmlNamespace[prefix.Length..];
            var separatorIndex = rest.IndexOf(';');

            clrNamespace = separatorIndex >= 0
                ? rest[..separatorIndex]
                : rest;

            clrNamespace = clrNamespace.Trim();

            return clrNamespace.Length > 0;
        }

        clrNamespace = string.Empty;

        return false;
    }

    private static bool IsValidCSharpFile(string file)
    {
        var normalized = file.Replace('\\', '/');

        if (normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase))
            return false;

        if (normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase))
            return false;

        if (normalized.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase))
            return false;

        if (normalized.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase))
            return false;

        if (normalized.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase))
            return false;

        if (normalized.EndsWith(".xaml.g.cs", StringComparison.OrdinalIgnoreCase))
            return false;

        if (normalized.EndsWith(".axaml.g.cs", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private static bool IsValidXamlFile(string file)
    {
        var normalized = file.Replace('\\', '/');

        if (normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase))
            return false;

        if (normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase))
            return false;

        return normalized.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase);
    }
}