using ReactiveUI;
using System;

namespace PANApp.Models;

public sealed class ProjectProfile : ReactiveObject
{
    private string _id = Guid.NewGuid().ToString();
    public string Id
    {
        get => _id;
        set => this.RaiseAndSetIfChanged(ref _id, value);
    }

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    private string _language = "C#";
    public string Language
    {
        get => _language;
        set => this.RaiseAndSetIfChanged(ref _language, value);
    }

    private string _projectPath = string.Empty;
    public string ProjectPath
    {
        get => _projectPath;
        set => this.RaiseAndSetIfChanged(ref _projectPath, value);
    }
}