using PANApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PANApp.Services;

public class ProjectAnalyzerService
{
    public (List<GraphNode> Nodes, List<GraphEdge> Edges) AnalyzeGraph(ProjectProfile profile)
    {
        var nodes = new List<GraphNode>();
        var edges = new List<(string SourceName, string TargetName)>();

        var basePath = profile.ProjectPath.TrimEnd('\\', '/');
        if (!Directory.Exists(basePath)) return (nodes, new List<GraphEdge>());

        string[] extensions = profile.Language switch
        {
            "C#" => new[] { "*.cs" },
            "Python" => new[] { "*.py" },
            _ => new[] { "*.cs", "*.py" }
        };

        var files = extensions.SelectMany(ext =>
            Directory.GetFiles(basePath, ext, SearchOption.AllDirectories)).ToList();

        var entityMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var relativePath = Path.GetRelativePath(basePath, file).Replace('\\', '/');

            var node = new GraphNode
            {
                DisplayName = fileName,
                FullName = relativePath
            };

            nodes.Add(node);

            if (!entityMap.ContainsKey(fileName))
            {
                entityMap[fileName] = relativePath;
            }
        }

        foreach (var file in files)
        {
            var sourceName = Path.GetFileNameWithoutExtension(file);
            var content = ReadFileSample(file, 30000);

            var dependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (profile.Language == "C#")
            {
                var usingMatches = Regex.Matches(content, @"using\s+[\w\.]+\.([\w]+);");
                foreach (Match match in usingMatches)
                {
                    dependencies.Add(match.Groups[1].Value);
                }
            }
            else if (profile.Language == "Python")
            {
                var importMatches = Regex.Matches(content, @"^(?:from|import)\s+[\w\.]+\.?([\w]+)", RegexOptions.Multiline);
                foreach (Match match in importMatches)
                {
                    dependencies.Add(match.Groups[1].Value);
                }
            }

            foreach (var targetName in entityMap.Keys)
            {
                if (sourceName.Equals(targetName, StringComparison.OrdinalIgnoreCase)) continue;

                if (Regex.IsMatch(content, $@"\b{Regex.Escape(targetName)}\b"))
                {
                    dependencies.Add(targetName);
                }
            }

            foreach (var dep in dependencies)
            {
                if (entityMap.ContainsKey(dep))
                {
                    edges.Add((sourceName, dep));
                }
            }
        }

        var uniqueEdges = edges.Distinct().ToList();
        var layoutService = new GraphLayoutService();

        var finalEdges = layoutService.CalculateLayoutAndEdges(nodes, uniqueEdges);

        return (nodes, finalEdges);
    }

    private string ReadFileSample(string path, int maxChars)
    {
        try
        {
            using var reader = new StreamReader(path);
            var buffer = new char[maxChars];
            var read = reader.Read(buffer, 0, maxChars);
            return new string(buffer, 0, read);
        }
        catch
        {
            return string.Empty;
        }
    }
}