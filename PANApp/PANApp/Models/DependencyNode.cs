using System.Collections.ObjectModel;

namespace PANApp.Models;

public sealed record DependencyNode
{
    public string ModuleName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public ObservableCollection<DependencyNode> Dependencies { get; set; } = new();
}