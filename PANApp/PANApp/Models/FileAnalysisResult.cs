using System.Collections.Generic;

namespace PANApp.Models;

public sealed record FileAnalysisResult
{
    public string FilePath { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public List<DeclaredType> DeclaredTypes { get; set; } = new();
    public List<string> Dependencies { get; set; } = new();
}