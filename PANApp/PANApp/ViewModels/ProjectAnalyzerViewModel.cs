using PANApp.Models;
using PANApp.Models.Configs;
using PANApp.Services.Implementations;
using ReactiveUI;
using System;
using System.Collections.Generic;
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
    private readonly NoteService _noteService;

    private ObservableCollection<ProjectProfileConfig> _profiles;
    public ObservableCollection<ProjectProfileConfig> Profiles
    {
        get => _profiles;
        set => this.RaiseAndSetIfChanged(ref _profiles, value);
    }

    private ProjectProfileConfig _selectedProfile;
    public ProjectProfileConfig SelectedProfile
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

    private GraphNode? _selectedNode;
    public GraphNode? SelectedNode
    {
        get => _selectedNode;
        set => this.RaiseAndSetIfChanged(ref _selectedNode, value);
    }

    private int _totalNotesCount;
    public int TotalNotesCount
    {
        get => _totalNotesCount;
        set => this.RaiseAndSetIfChanged(ref _totalNotesCount, value);
    }

    private bool _isProfileDetailsVisible = true;
    public bool IsProfileDetailsVisible
    {
        get => _isProfileDetailsVisible;
        set => this.RaiseAndSetIfChanged(ref _isProfileDetailsVisible, value);
    }

    public NoteViewModel NoteVm { get; }

    public List<string> AvailableLanguages { get; } =
    [
        "C#",
        "C# Avalonia/WPF"
    ];

    public ReactiveCommand<Unit, Unit> CreateProfileCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteProfileCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveProfilesCommand { get; }
    public ReactiveCommand<Unit, Unit> AnalyzeProjectCommand { get; }
    public ReactiveCommand<Unit, Unit> BrowseFolderCommand { get; }
    public ReactiveCommand<GraphNode, Unit> OnNodeClickedCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowAllNotesCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleProfileDetailsCommand { get; }

    public Interaction<string, string> BrowseFolder { get; }

    public ProjectAnalyzerViewModel()
    {
        _profileService = new ProfileService();
        _analyzerService = new ProjectAnalyzerService();
        _noteService = new NoteService();

        NoteVm = new NoteViewModel(OnNotesChanged);

        Profiles = new ObservableCollection<ProjectProfileConfig>(_profileService.LoadProfiles());
        GraphNodes = new ObservableCollection<GraphNode>();
        GraphEdges = new ObservableCollection<GraphEdge>();

        BrowseFolder = new Interaction<string, string>();

        var canAnalyze = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(profile =>
            {
                if (profile == null) return Observable.Return(false);

                return profile.WhenAnyValue(p => p.ProjectPath)
                    .Select(path => !string.IsNullOrWhiteSpace(path));
            })
            .Switch()
            .StartWith(false);

        var canBrowseOrDelete = this.WhenAnyValue(x => x.SelectedProfile)
            .Select(profile => profile != null)
            .StartWith(false);

        CreateProfileCommand = ReactiveCommand.Create(CreateProfile);
        DeleteProfileCommand = ReactiveCommand.Create(DeleteProfile, canBrowseOrDelete);
        SaveProfilesCommand = ReactiveCommand.Create(SaveProfiles);
        AnalyzeProjectCommand = ReactiveCommand.CreateFromTask(AnalyzeProjectAsync, canAnalyze);
        BrowseFolderCommand = ReactiveCommand.CreateFromTask(BrowseFolderAsync, canBrowseOrDelete);
        OnNodeClickedCommand = ReactiveCommand.Create<GraphNode>(OnNodeClicked);
        ShowAllNotesCommand = ReactiveCommand.Create(ShowAllNotes, canBrowseOrDelete);
        ToggleProfileDetailsCommand = ReactiveCommand.Create(ToggleProfileDetails);

        this.WhenAnyValue(x => x.SelectedProfile)
            .Subscribe(LoadGraphFromProfile);
    }

    private void ToggleProfileDetails()
        => IsProfileDetailsVisible = !IsProfileDetailsVisible;

    private void LoadGraphFromProfile(ProjectProfileConfig profile)
    {
        GraphNodes.Clear();
        GraphEdges.Clear();
        NoteVm.ClosePanel();

        if (profile == null || !profile.HasAnalyzedGraph)
        {
            GraphCanvasWidth = 1200;
            GraphCanvasHeight = 800;
            TotalNotesCount = 0;
            return;
        }

        foreach (var node in profile.AnalyzedNodes)
        {
            node.NoteCount = _noteService.GetNoteCountByModule(profile.Id, node.Id);
            GraphNodes.Add(node);
        }

        foreach (var edge in profile.AnalyzedEdges)
            GraphEdges.Add(edge);

        GraphCanvasWidth = profile.CanvasWidth;
        GraphCanvasHeight = profile.CanvasHeight;

        TotalNotesCount = _noteService.GetNoteCountByProfile(profile.Id);
    }

    private void OnNodeClicked(GraphNode node)
    {
        if (SelectedProfile == null || node == null) return;

        SelectedNode = node;
        NoteVm.LoadNotesForModule(SelectedProfile.Id, node.Id, node.DisplayName);
    }

    private void ShowAllNotes()
    {
        if (SelectedProfile == null) return;

        NoteVm.LoadAllNotesForProfile(SelectedProfile.Id, SelectedProfile.Name);

        NoteVm.UpdateModuleNames(moduleId =>
        {
            var node = SelectedProfile.AnalyzedNodes.FirstOrDefault(n => n.Id == moduleId);
            return node?.DisplayName ?? moduleId;
        });
    }

    private void OnNotesChanged()
    {
        if (SelectedProfile == null) return;

        foreach (var node in GraphNodes)
            node.NoteCount = _noteService.GetNoteCountByModule(SelectedProfile.Id, node.Id);

        TotalNotesCount = _noteService.GetNoteCountByProfile(SelectedProfile.Id);

        if (NoteVm.IsAllNotesMode) ShowAllNotes();
    }

    private void CreateProfile()
    {
        var newProfile = new ProjectProfileConfig
        {
            Name = $"New Project {Profiles.Count + 1}",
            Language = string.Empty,
            ProjectPath = string.Empty,
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
    {
        _profileService.SaveProfiles(Profiles.ToList());
    }

    private async Task AnalyzeProjectAsync()
    {
        if (SelectedProfile == null || string.IsNullOrWhiteSpace(SelectedProfile.ProjectPath))
            return;

        IsAnalyzing = true;

        try
        {
            var (nodes, edges, width, height) = await Task.Run(() => _analyzerService.AnalyzeGraph(SelectedProfile));

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                SelectedProfile.AnalyzedNodes = nodes;
                SelectedProfile.AnalyzedEdges = edges;
                SelectedProfile.CanvasWidth = Math.Max(width, 1200);
                SelectedProfile.CanvasHeight = Math.Max(height, 800);

                GraphNodes.Clear();
                GraphEdges.Clear();

                foreach (var node in nodes)
                {
                    node.NoteCount = _noteService.GetNoteCountByModule(SelectedProfile.Id, node.Id);
                    GraphNodes.Add(node);
                }

                foreach (var edge in edges) GraphEdges.Add(edge);

                GraphCanvasWidth = SelectedProfile.CanvasWidth;
                GraphCanvasHeight = SelectedProfile.CanvasHeight;

                TotalNotesCount = _noteService.GetNoteCountByProfile(SelectedProfile.Id);
            });
        }
        finally
        {
            IsAnalyzing = false;
        }
    }
}