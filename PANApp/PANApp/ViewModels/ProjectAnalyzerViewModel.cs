using PANApp.Models;
using PANApp.Services;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace PANApp.ViewModels;

public class ProjectAnalyzerViewModel : ViewModelBase
{
    private readonly ProfileService _profileService;
    private readonly ProjectAnalyzerService _analyzerService;
    private readonly GraphLayoutService _layoutService;

    private double _graphCanvasWidth = 1200;
    public double GraphCanvasWidth
    {
        get => _graphCanvasWidth;
        set => this.RaiseAndSetIfChanged(ref _graphCanvasWidth, value);
    }

    private double _graphCanvasHeight = 800;
    public double GraphCanvasHeight
    {
        get => _graphCanvasHeight;
        set => this.RaiseAndSetIfChanged(ref _graphCanvasHeight, value);
    }

    private ObservableCollection<ProjectProfile> _profiles;
    public ObservableCollection<ProjectProfile> Profiles
    {
        get => _profiles;
        set => this.RaiseAndSetIfChanged(ref _profiles, value);
    }

    private ProjectProfile _selectedProfile;
    public ProjectProfile SelectedProfile
    {
        get => _selectedProfile;
        set => this.RaiseAndSetIfChanged(ref _selectedProfile, value);
    }

    private ObservableCollection<GraphNode> _graphNodes;
    public ObservableCollection<GraphNode> GraphNodes
    {
        get => _graphNodes;
        set => this.RaiseAndSetIfChanged(ref _graphNodes, value);
    }

    private ObservableCollection<GraphEdge> _graphEdges;
    public ObservableCollection<GraphEdge> GraphEdges
    {
        get => _graphEdges;
        set => this.RaiseAndSetIfChanged(ref _graphEdges, value);
    }

    private bool _isAnalyzing;
    public bool IsAnalyzing
    {
        get => _isAnalyzing;
        set => this.RaiseAndSetIfChanged(ref _isAnalyzing, value);
    }

    public ReactiveCommand<Unit, Unit> CreateProfileCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteProfileCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveProfilesCommand { get; }
    public ReactiveCommand<Unit, Unit> AnalyzeProjectCommand { get; }
    public ReactiveCommand<Unit, Unit> BrowseFolderCommand { get; }

    public Interaction<string, string> BrowseFolder { get; }

    public ProjectAnalyzerViewModel()
    {
        _profileService = new ProfileService();
        _analyzerService = new ProjectAnalyzerService();
        _layoutService = new GraphLayoutService();

        Profiles = new ObservableCollection<ProjectProfile>(_profileService.LoadProfiles());
        GraphNodes = new ObservableCollection<GraphNode>();
        GraphEdges = new ObservableCollection<GraphEdge>();

        BrowseFolder = new Interaction<string, string>();

        CreateProfileCommand = ReactiveCommand.Create(CreateProfile);
        DeleteProfileCommand = ReactiveCommand.Create(DeleteProfile,
            this.WhenAnyValue(x => x.SelectedProfile).Select(p => p != null));
        SaveProfilesCommand = ReactiveCommand.Create(SaveProfiles);
        AnalyzeProjectCommand = ReactiveCommand.CreateFromTask(AnalyzeProjectAsync,
            this.WhenAnyValue(x => x.SelectedProfile).Select(p => p != null && !string.IsNullOrWhiteSpace(p.ProjectPath)));
        BrowseFolderCommand = ReactiveCommand.CreateFromTask(BrowseFolderAsync,
            this.WhenAnyValue(x => x.SelectedProfile).Select(p => p != null));
    }

    private void CreateProfile()
    {
        var newProfile = new ProjectProfile
        {
            Name = $"Новый проект {Profiles.Count + 1}",
            Language = "C#",
            ProjectPath = ""
        };

        Profiles.Add(newProfile);
        SelectedProfile = newProfile;
    }

    private void DeleteProfile()
    {
        if (SelectedProfile == null) return;

        var index = Profiles.IndexOf(SelectedProfile);
        Profiles.Remove(SelectedProfile);

        if (Profiles.Count > 0)
            SelectedProfile = Profiles[Math.Min(index, Profiles.Count - 1)];
        else SelectedProfile = null;
    }

    private async Task BrowseFolderAsync()
    {
        if (SelectedProfile == null) return;

        var folderPath = await BrowseFolder.Handle(string.Empty);
        if (!string.IsNullOrEmpty(folderPath))
            SelectedProfile.ProjectPath = folderPath;
    }

    private void SaveProfiles()
        => _profileService.SaveProfiles(Profiles.ToList());

    private async Task AnalyzeProjectAsync()
    {
        if (SelectedProfile == null || string.IsNullOrWhiteSpace(SelectedProfile.ProjectPath))
            return;

        IsAnalyzing = true;
        GraphNodes.Clear();
        GraphEdges.Clear();

        try
        {
            var (nodes, edges, width, height) = await Task.Run(() => _analyzerService.AnalyzeGraph(SelectedProfile));

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                foreach (var node in nodes) GraphNodes.Add(node);
                foreach (var edge in edges) GraphEdges.Add(edge);
            });
        }
        finally
        {
            IsAnalyzing = false;
        }
    }
}