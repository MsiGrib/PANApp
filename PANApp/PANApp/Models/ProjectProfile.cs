using Avalonia;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PANApp.Models;

public class ProjectProfile : ReactiveObject
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

    private bool _hasAnalyzedGraph;
    [JsonIgnore]
    public bool HasAnalyzedGraph
    {
        get => _hasAnalyzedGraph;
        set => this.RaiseAndSetIfChanged(ref _hasAnalyzedGraph, value);
    }

    private List<GraphNode> _analyzedNodes = new();
    [JsonIgnore]
    public List<GraphNode> AnalyzedNodes
    {
        get => _analyzedNodes;
        set
        {
            this.RaiseAndSetIfChanged(ref _analyzedNodes, value);
            HasAnalyzedGraph = value != null && value.Count > 0;
        }
    }

    private List<GraphEdge> _analyzedEdges = new();
    [JsonIgnore]
    public List<GraphEdge> AnalyzedEdges
    {
        get => _analyzedEdges;
        set => this.RaiseAndSetIfChanged(ref _analyzedEdges, value);
    }

    private double _canvasWidth = 1200;
    [JsonIgnore]
    public double CanvasWidth
    {
        get => _canvasWidth;
        set => this.RaiseAndSetIfChanged(ref _canvasWidth, value);
    }

    private double _canvasHeight = 800;
    [JsonIgnore]
    public double CanvasHeight
    {
        get => _canvasHeight;
        set => this.RaiseAndSetIfChanged(ref _canvasHeight, value);
    }

    private Matrix _transformMatrix = Matrix.Identity;
    [JsonIgnore]
    public Matrix TransformMatrix
    {
        get => _transformMatrix;
        set => this.RaiseAndSetIfChanged(ref _transformMatrix, value);
    }
}