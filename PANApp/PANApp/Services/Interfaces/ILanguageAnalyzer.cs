using PANApp.Models;
using System.Collections.Generic;

namespace PANApp.Services.Interfaces;

public interface ILanguageAnalyzer
{
    public string Language { get; }
    public List<FileAnalysisResult> AnalyzeProject(string basePath);
}