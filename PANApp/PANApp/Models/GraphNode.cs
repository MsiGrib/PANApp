using ReactiveUI;

namespace PANApp.Models;

public class GraphNode : ReactiveObject
{
    private string _displayName = string.Empty;
    public string DisplayName
    {
        get => _displayName;
        set => this.RaiseAndSetIfChanged(ref _displayName, value);
    }

    private string _fullName = string.Empty;
    public string FullName
    {
        get => _fullName;
        set => this.RaiseAndSetIfChanged(ref _fullName, value);
    }

    private double _x;
    public double X
    {
        get => _x;
        set => this.RaiseAndSetIfChanged(ref _x, value);
    }

    private double _y;
    public double Y
    {
        get => _y;
        set => this.RaiseAndSetIfChanged(ref _y, value);
    }

    public double Width { get; set; } = 140;
    public double Height { get; set; } = 45;
}