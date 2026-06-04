using PANApp.Comparers;
using PANApp.Models;
using PANApp.Services.Implementations.LanguageAnalyzers;
using PANApp.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PANApp.Services.Implementations;

public class ProjectAnalyzerService
{
    private readonly Dictionary<string, ILanguageAnalyzer> _analyzers;

    public ProjectAnalyzerService()
    {
        _analyzers = new Dictionary<string, ILanguageAnalyzer>(StringComparer.OrdinalIgnoreCase)
        {
            { "C#", new CSharpLanguageAnalyzer() },
            { "C# Avalonia/WPF", new CSharpAvaloniaWpfLanguageAnalyzer() },
            { "Python", new PythonLanguageAnalyzer() }
        };
    }

    public (List<GraphNode> Nodes, List<GraphEdge> Edges, double Width, double Height) AnalyzeGraph(ProjectProfile profile)
    {
        var basePath = profile.ProjectPath.TrimEnd('\\', '/');

        if (!Directory.Exists(basePath))
            return (new List<GraphNode>(), new List<GraphEdge>(), 0.0, 0.0);

        if (!_analyzers.TryGetValue(profile.Language, out var analyzer))
            return (new List<GraphNode>(), new List<GraphEdge>(), 0.0, 0.0);

        var analyzedFiles = analyzer.AnalyzeProject(basePath);
        if (analyzedFiles.Count == 0)
            return (new List<GraphNode>(), new List<GraphEdge>(), 0.0, 0.0);

        var typeToFiles = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in analyzedFiles)
        {
            foreach (var declaredType in file.DeclaredTypes)
            {
                AddIndex(typeToFiles, declaredType.Name, file.FilePath);
                AddIndex(typeToFiles, declaredType.FullName, file.FilePath);
            }
        }

        var nodes = analyzedFiles.Select(item =>
        {
            var displayName = item.DeclaredTypes.Count == 1
                ? item.DeclaredTypes[0].Name
                : Path.GetFileNameWithoutExtension(item.FilePath);

            return new GraphNode
            {
                Id = item.RelativePath,
                DisplayName = displayName,
                FullName = item.RelativePath
            };
        }).ToList();

        var rawEdges = new HashSet<(string Source, string Target)>(new EdgeComparer());

        foreach (var file in analyzedFiles)
        {
            var sourceId = file.RelativePath;
            var sourcePath = file.FilePath;

            foreach (var dependency in file.Dependencies.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!typeToFiles.TryGetValue(dependency, out var targetFiles))
                    continue;

                foreach (var targetFile in targetFiles)
                {
                    if (string.Equals(targetFile, sourcePath, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var targetId = Path.GetRelativePath(basePath, targetFile).Replace('\\', '/');
                    rawEdges.Add((sourceId, targetId));
                }
            }
        }

        var layoutService = new GraphLayoutService();
        var layout = layoutService.CalculateLayoutAndEdges(nodes, rawEdges.ToList());

        return (nodes, layout.Edges, layout.Width, layout.Height);
    }

    private static void AddIndex(Dictionary<string, HashSet<string>> index, string key, string filePath)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        if (!index.TryGetValue(key, out var set))
        {
            set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            index[key] = set;
        }

        set.Add(filePath);
    }
}