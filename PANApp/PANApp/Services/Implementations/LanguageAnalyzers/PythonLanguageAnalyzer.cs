using PANApp.Models;
using PANApp.Services.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PANApp.Services.Implementations.LanguageAnalyzers;

public sealed class PythonLanguageAnalyzer : ILanguageAnalyzer
{
    public string Language => "Python";

    public List<FileAnalysisResult> AnalyzeProject(string basePath)
    {
        if (!Directory.Exists(basePath)) return new();

        return Directory
            .EnumerateFiles(basePath, "*.py", SearchOption.AllDirectories)
            .Select(file => new FileAnalysisResult
            {
                FilePath = file,
                RelativePath = Path.GetRelativePath(basePath, file).Replace('\\', '/'),
                DeclaredTypes = new(),
                Dependencies = new()
            })
            .ToList();
    }
}