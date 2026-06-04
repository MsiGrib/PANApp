using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PANApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PANApp.Services.Implementations.LanguageAnalyzers;

public static class СSharpRoslynProjectGraphAnalyzer
{
    public static List<FileAnalysisResult> AnalyzeCSharpFiles(string basePath, IEnumerable<string> sourceFiles)
    {
        var files = sourceFiles.ToList();
        if (files.Count == 0)
            return new List<FileAnalysisResult>();

        var trees = files
            .Select(file => CSharpSyntaxTree.ParseText(ReadFileSafe(file), path: file))
            .ToList();

        var compilation = CSharpCompilation.Create(
            assemblyName: "PANApp.ProjectGraphAnalysis",
            syntaxTrees: trees,
            references: GetMetadataReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var declaredTypesByFile = new Dictionary<string, List<DeclaredType>>(StringComparer.OrdinalIgnoreCase);

        foreach (var tree in trees)
        {
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
            var root = tree.GetRoot();
            var filePath = tree.FilePath ?? string.Empty;

            var declared = new List<DeclaredType>();

            foreach (var typeDecl in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
                AddDeclaredType(model, typeDecl, declared);

            foreach (var enumDecl in root.DescendantNodes().OfType<EnumDeclarationSyntax>())
                AddDeclaredType(model, enumDecl, declared);

            foreach (var delegateDecl in root.DescendantNodes().OfType<DelegateDeclarationSyntax>())
                AddDeclaredType(model, delegateDecl, declared);

            declaredTypesByFile[filePath] = declared
                .GroupBy(x => x.FullName, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
        }

        var result = new List<FileAnalysisResult>(trees.Count);

        foreach (var tree in trees)
        {
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
            var root = tree.GetRoot();
            var filePath = tree.FilePath ?? string.Empty;
            var dependencies = CollectDependencies(root, model).ToList();

            result.Add(new FileAnalysisResult
            {
                FilePath = filePath,
                RelativePath = Path.GetRelativePath(basePath, filePath).Replace('\\', '/'),
                DeclaredTypes = declaredTypesByFile.TryGetValue(filePath, out var declared)
                    ? declared
                    : new List<DeclaredType>(),
                Dependencies = dependencies
            });
        }

        return result;
    }

    private static IEnumerable<string> CollectDependencies(SyntaxNode root, SemanticModel model)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in root.DescendantNodes())
        {
            ITypeSymbol? typeSymbol = null;

            if (node is SimpleNameSyntax simpleName)
                typeSymbol = model.GetSymbolInfo(simpleName).Symbol as ITypeSymbol;
            else if (node is TypeSyntax typeSyntax)
                typeSymbol = model.GetTypeInfo(typeSyntax).Type;

            var key = GetTypeKey(typeSymbol);
            if (string.IsNullOrWhiteSpace(key)) continue;

            if (seen.Add(key)) yield return key;
        }
    }

    private static void AddDeclaredType(SemanticModel model, MemberDeclarationSyntax declaration, List<DeclaredType> declared)
    {
        if (model.GetDeclaredSymbol(declaration) is not INamedTypeSymbol symbol)
            return;

        var key = GetTypeKey(symbol);
        if (string.IsNullOrWhiteSpace(key))
            return;

        declared.Add(new DeclaredType
        {
            Name = symbol.Name,
            FullName = key
        });
    }

    private static string? GetTypeKey(ITypeSymbol? symbol)
    {
        if (symbol is null)
            return null;

        if (symbol is IArrayTypeSymbol arrayType)
            return GetTypeKey(arrayType.ElementType);

        if (symbol is IPointerTypeSymbol pointerType)
            return GetTypeKey(pointerType.PointedAtType);

        if (symbol is INamedTypeSymbol named && named.IsGenericType)
            symbol = named.OriginalDefinition;

        var format = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

        return symbol.ToDisplayString(format).Replace("global::", "", StringComparison.Ordinal);
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        var tpa = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;

        if (!string.IsNullOrWhiteSpace(tpa))
        {
            return tpa
                .Split(Path.PathSeparator)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(p => MetadataReference.CreateFromFile(p));
        }

        return new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Uri).Assembly.Location)
        };
    }

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