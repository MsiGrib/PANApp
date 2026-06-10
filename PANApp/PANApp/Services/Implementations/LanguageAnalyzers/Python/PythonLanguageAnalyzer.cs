using PANApp.Datas;
using PANApp.Models;
using PANApp.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PANApp.Services.Implementations.LanguageAnalyzers.Python;

public sealed class PythonLanguageAnalyzer : ILanguageAnalyzer
{
    public string Language => AvailableLanguagesData.Python;

    public List<FileAnalysisResult> AnalyzeProject(string basePath)
    {
        if (!Directory.Exists(basePath)) return new();

        var files = Directory
            .EnumerateFiles(basePath, "*.py", SearchOption.AllDirectories)
            .Where(IsValidPythonFile)
            .ToList();

        return PythonProjectGraphAnalyzer
            .AnalyzePythonFiles(basePath, files);
    }

    private static bool IsValidPythonFile(string file)
    {
        var path = file.Replace('\\', '/');

        if (path.Contains("/venv/", StringComparison.OrdinalIgnoreCase))
            return false;

        if (path.Contains("/.venv/", StringComparison.OrdinalIgnoreCase))
            return false;

        if (path.Contains("/env/", StringComparison.OrdinalIgnoreCase))
            return false;

        if (path.Contains("/__pycache__/", StringComparison.OrdinalIgnoreCase))
            return false;

        if (path.Contains("/site-packages/", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }
}