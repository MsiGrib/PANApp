using PANApp.Datas;
using PANApp.Models;
using PANApp.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PANApp.Services.Implementations.LanguageAnalyzers.CSharp;

public sealed class CSharpBlazorLanguageAnalyzer : ILanguageAnalyzer
{
    private static readonly Regex RazorComponentTagRegex =
        new(@"<(?<name>[A-Z][A-Za-z0-9_.]*)\b", RegexOptions.Compiled);

    private static readonly Regex RazorInheritsRegex =
        new(@"@inherits\s+(?<type>[A-Za-z_][\w\.]*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex RazorInjectRegex =
        new(@"@inject\s+(?<type>[A-Za-z_][\w\.]*)\s+[A-Za-z_]\w*", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex RazorTypeOfRegex =
        new(@"\btypeof\s*\(\s*(?<type>[A-Za-z_][\w\.]*)\s*\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex RazorNewRegex =
        new(@"\bnew\s+(?<type>[A-Za-z_][\w\.]*)\s*(?:\(|\{)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex RazorNamespaceRegex =
        new(@"^\s*@namespace\s+(?<ns>[A-Za-z_][\w\.]*)\s*$", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    public string Language => AvailableLanguagesData.CSharpBlazor;

    public List<FileAnalysisResult> AnalyzeProject(string basePath)
    {
        if (!Directory.Exists(basePath))
            return new List<FileAnalysisResult>();

        var csFiles = Directory.EnumerateFiles(basePath, "*.cs", SearchOption.AllDirectories)
            .Where(IsValidCSharpFile)
            .ToList();

        var csResults = СSharpRoslynProjectGraphAnalyzer.AnalyzeCSharpFiles(basePath, csFiles);

        var razorFiles = Directory.EnumerateFiles(basePath, "*.razor", SearchOption.AllDirectories)
            .Where(IsValidRazorFile)
            .ToList();

        var razorMeta = razorFiles.Select(file =>
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var ns = ExtractNamespace(file);
            var fullName = string.IsNullOrWhiteSpace(ns) ? fileName : $"{ns}.{fileName}";

            return new
            {
                FilePath = file,
                RelativePath = Path.GetRelativePath(basePath, file).Replace('\\', '/'),
                ComponentName = fileName,
                Namespace = ns,
                FullName = fullName
            };
        }).ToList();

        var knownProjectTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in csResults)
        {
            foreach (var declared in item.DeclaredTypes)
            {
                if (!string.IsNullOrWhiteSpace(declared.Name))
                    knownProjectTypes.Add(declared.Name);

                if (!string.IsNullOrWhiteSpace(declared.FullName))
                    knownProjectTypes.Add(declared.FullName);
            }
        }

        foreach (var meta in razorMeta)
        {
            if (!string.IsNullOrWhiteSpace(meta.ComponentName))
                knownProjectTypes.Add(meta.ComponentName);

            if (!string.IsNullOrWhiteSpace(meta.FullName))
                knownProjectTypes.Add(meta.FullName);
        }

        var razorResults = new List<FileAnalysisResult>();

        foreach (var meta in razorMeta)
        {
            var dependencies = CollectRazorDependencies(meta.FilePath, knownProjectTypes).ToList();

            razorResults.Add(new FileAnalysisResult
            {
                FilePath = meta.FilePath,
                RelativePath = meta.RelativePath,
                DeclaredTypes = new List<DeclaredType>
                {
                    new DeclaredType
                    {
                        Name = meta.ComponentName,
                        FullName = meta.FullName
                    }
                },
                Dependencies = dependencies
            });
        }

        return csResults.Concat(razorResults).ToList();
    }

    private static IEnumerable<string> CollectRazorDependencies(string filePath, HashSet<string> knownProjectTypes)
    {
        var dependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string content;
        try
        {
            content = File.ReadAllText(filePath);
        }
        catch
        {
            return dependencies;
        }

        foreach (Match match in RazorComponentTagRegex.Matches(content))
            TryAddKnownType(dependencies, match.Groups["name"].Value, knownProjectTypes);

        foreach (Match match in RazorInheritsRegex.Matches(content))
            TryAddKnownType(dependencies, match.Groups["type"].Value, knownProjectTypes);

        foreach (Match match in RazorInjectRegex.Matches(content))
            TryAddKnownType(dependencies, match.Groups["type"].Value, knownProjectTypes);

        foreach (Match match in RazorTypeOfRegex.Matches(content))
            TryAddKnownType(dependencies, match.Groups["type"].Value, knownProjectTypes);

        foreach (Match match in RazorNewRegex.Matches(content))
            TryAddKnownType(dependencies, match.Groups["type"].Value, knownProjectTypes);

        return dependencies;
    }

    private static void TryAddKnownType(HashSet<string> dependencies, string candidate, HashSet<string> knownProjectTypes)
    {
        if (string.IsNullOrWhiteSpace(candidate)) return;

        candidate = candidate.Trim();

        if (knownProjectTypes.Contains(candidate))
        {
            dependencies.Add(candidate);
            return;
        }

        var shortName = candidate.Split('.').Last();
        if (knownProjectTypes.Contains(shortName))
            dependencies.Add(shortName);
    }

    private static string? ExtractNamespace(string filePath)
    {
        string content;
        try
        {
            content = File.ReadAllText(filePath);
        }
        catch
        {
            return null;
        }

        var match = RazorNamespaceRegex.Match(content);
        if (!match.Success) return null;

        var ns = match.Groups["ns"].Value.Trim();
        return ns.Length > 0 ? ns : null;
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

    private static bool IsValidRazorFile(string file)
    {
        var normalized = file.Replace('\\', '/');

        if (normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase))
            return false;

        if (normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }
}