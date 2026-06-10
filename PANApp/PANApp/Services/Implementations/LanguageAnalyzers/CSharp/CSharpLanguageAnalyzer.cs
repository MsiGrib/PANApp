using PANApp.Datas;
using PANApp.Models;
using PANApp.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PANApp.Services.Implementations.LanguageAnalyzers.CSharp;

public sealed class CSharpLanguageAnalyzer : ILanguageAnalyzer
{
    public string Language => AvailableLanguagesData.CSharp;

    public List<FileAnalysisResult> AnalyzeProject(string basePath)
    {
        if (!Directory.Exists(basePath)) return new List<FileAnalysisResult>();

        var files = Directory.EnumerateFiles(basePath, "*.cs", SearchOption.AllDirectories)
            .Where(IsValidSourceFile)
            .ToList();

        return СSharpRoslynProjectGraphAnalyzer.AnalyzeCSharpFiles(basePath, files);
    }

    private static bool IsValidSourceFile(string file)
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
}