using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PANApp.Comparers;
using PANApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PANApp.Services;

public class ProjectAnalyzerService
{
    public (List<GraphNode> Nodes, List<GraphEdge> Edges, double Width, double Height) AnalyzeGraph(ProjectProfile profile)
    {
        var basePath = profile.ProjectPath.TrimEnd('\\', '/');
        if (!Directory.Exists(basePath))
            return (new List<GraphNode>(), new List<GraphEdge>(), 0.0, 0.0);

        var files = Directory
            .EnumerateFiles(basePath, "*.cs", SearchOption.AllDirectories)
            .Where(f =>
                !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                !f.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) &&
                !f.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (files.Count == 0)
            return (new List<GraphNode>(), new List<GraphEdge>(), 0.0, 0.0);

        var parsedFiles = new List<(string FilePath, string RelativePath, CompilationUnitSyntax Root)>();

        foreach (var file in files)
        {
            var text = ReadFileSafe(file);
            var tree = CSharpSyntaxTree.ParseText(text, path: file);
            var root = tree.GetCompilationUnitRoot();

            var relativePath = Path.GetRelativePath(basePath, file).Replace('\\', '/');
            parsedFiles.Add((file, relativePath, root));
        }

        var typeToFiles = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var fileToTypes = new Dictionary<string, List<DeclaredType>>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in parsedFiles)
        {
            var declaredTypes = item.Root.DescendantNodes()
                .OfType<TypeDeclarationSyntax>()
                .Select(t =>
                {
                    var ns = GetNamespace(t);

                    return new DeclaredType
                    {
                        Name = t.Identifier.ValueText,
                        FullName = string.IsNullOrWhiteSpace(ns)
                            ? t.Identifier.ValueText
                            : $"{ns}.{t.Identifier.ValueText}"
                    };
                })
                .ToList();

            if (declaredTypes.Count == 0)
            {
                var fileName = Path.GetFileNameWithoutExtension(item.FilePath);

                declaredTypes.Add(new DeclaredType
                {
                    Name = fileName,
                    FullName = fileName
                });
            }

            fileToTypes[item.FilePath] = declaredTypes;

            foreach (var declaredType in declaredTypes)
            {
                if (!typeToFiles.TryGetValue(declaredType.Name, out var set))
                {
                    set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    typeToFiles[declaredType.Name] = set;
                }

                set.Add(item.FilePath);
            }
        }

        var nodes = parsedFiles.Select(item =>
        {
            var types = fileToTypes[item.FilePath];

            string displayName;
            string fullName;

            if (types.Count == 1)
            {
                displayName = types[0].Name;
                fullName = types[0].FullName;
            }
            else
            {
                displayName = Path.GetFileNameWithoutExtension(item.FilePath);
                fullName = string.Join(Environment.NewLine,
                    types.Select(t => t.FullName));
            }

            return new GraphNode
            {
                Id = item.RelativePath,
                DisplayName = displayName,
                FullName = fullName
            };
        }).ToList();

        var rawEdges = new HashSet<(string Source, string Target)>(new EdgeComparer());

        foreach (var item in parsedFiles)
        {
            var sourceFile = item.FilePath;
            var sourceId = item.RelativePath;

            var referencedNames = CollectReferencedTypeNames(item.Root)
                .Where(name => typeToFiles.ContainsKey(name))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var typeName in referencedNames)
            {
                foreach (var targetFile in typeToFiles[typeName])
                {
                    if (string.Equals(targetFile, sourceFile, StringComparison.OrdinalIgnoreCase))
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

    private static string? GetNamespace(TypeDeclarationSyntax type)
    {
        SyntaxNode? current = type.Parent;

        while (current != null)
        {
            if (current is NamespaceDeclarationSyntax ns)
                return ns.Name.ToString();

            if (current is FileScopedNamespaceDeclarationSyntax fileNs)
                return fileNs.Name.ToString();

            current = current.Parent;
        }

        return null;
    }

    private static IEnumerable<string> CollectReferencedTypeNames(CompilationUnitSyntax root)
    {
        foreach (var node in root.DescendantNodes())
        {
            switch (node)
            {
                case ObjectCreationExpressionSyntax obj:
                    yield return GetTypeName(obj.Type);
                    break;

                case BaseTypeSyntax baseType:
                    yield return GetTypeName(baseType.Type);
                    break;

                case IdentifierNameSyntax id:
                    yield return id.Identifier.ValueText;
                    break;

                case GenericNameSyntax generic:
                    yield return generic.Identifier.ValueText;
                    break;

                case QualifiedNameSyntax qualified:
                    yield return qualified.Right.Identifier.ValueText;
                    break;

                case AliasQualifiedNameSyntax aliasQualified:
                    yield return aliasQualified.Name.Identifier.ValueText;
                    break;
            }
        }
    }

    private static string GetTypeName(TypeSyntax typeSyntax)
        => typeSyntax switch
        {
            IdentifierNameSyntax id => id.Identifier.ValueText,
            QualifiedNameSyntax q => q.Right.Identifier.ValueText,
            GenericNameSyntax g => g.Identifier.ValueText,
            AliasQualifiedNameSyntax a => a.Name.Identifier.ValueText,
            NullableTypeSyntax n => GetTypeName(n.ElementType),
            ArrayTypeSyntax arr => GetTypeName(arr.ElementType),
            _ => typeSyntax.ToString().Split('<')[0].Split('.').Last()
        };

    private static string ReadFileSafe(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch
        {
            return string.Empty;
        }
    }
}