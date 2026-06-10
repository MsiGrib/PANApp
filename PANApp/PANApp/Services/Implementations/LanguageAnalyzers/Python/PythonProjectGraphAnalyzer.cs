using PANApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PANApp.Services.Implementations.LanguageAnalyzers.Python;

public static class PythonProjectGraphAnalyzer
{
    public static List<FileAnalysisResult> AnalyzePythonFiles(string basePath, IEnumerable<string> sourceFiles)
    {
        var files = sourceFiles.ToList();
        var moduleMap = BuildModuleMap(basePath, files);

        var results = new List<FileAnalysisResult>();

        foreach (var file in files)
        {
            var moduleName = GetModuleName(basePath, file);

            var dependencies = AnalyzeImports(
                File.ReadAllText(file),
                moduleMap
            );

            results.Add(new FileAnalysisResult
            {
                FilePath = file,
                RelativePath = Path.GetRelativePath(basePath, file).Replace('\\', '/'),

                DeclaredTypes = new List<DeclaredType>
                {
                    new()
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        FullName = moduleName
                    }
                },

                Dependencies = dependencies.ToList()
            });
        }

        return results;
    }

    private static Dictionary<string, string> BuildModuleMap(string basePath, IEnumerable<string> files)
        => files.ToDictionary(
            file => GetModuleName(basePath, file),
            file => file,
            StringComparer.OrdinalIgnoreCase);

    private static string GetModuleName(string basePath, string file)
    {
        var relative = Path.GetRelativePath(basePath, file)
            .Replace('\\', '/');

        if (relative.EndsWith(".py"))
            relative = relative[..^3];

        if (relative.EndsWith("/__init__"))
            relative = relative[..^9];

        return relative.Replace('/', '.');
    }

    private static HashSet<string> AnalyzeImports(string source, Dictionary<string, string> moduleMap)
    {
        var dependencies = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (Match match in ImportRegex().Matches(source))
        {
            var module = match.Groups["module"].Value;

            if (moduleMap.ContainsKey(module))
                dependencies.Add(module);
        }

        foreach (Match match in FromImportRegex().Matches(source))
        {
            var module = match.Groups["module"].Value;

            if (moduleMap.ContainsKey(module))
                dependencies.Add(module);
        }

        return dependencies;
    }

    private static Regex ImportRegex()
        => new(@"^\s*import\s+(?<module>[a-zA-Z_][\w\.]*)",
            RegexOptions.Multiline | RegexOptions.Compiled);

    private static Regex FromImportRegex()
        => new(@"^\s*from\s+(?<module>[a-zA-Z_][\w\.]*)\s+import",
            RegexOptions.Multiline | RegexOptions.Compiled);
}