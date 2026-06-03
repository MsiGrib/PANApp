using System.Collections.Generic;

namespace PANApp.Models;

public sealed record GraphLayoutResult
{
    public List<GraphEdge> Edges { get; init; } = new();
    public double Width { get; init; } = 0.0;
    public double Height { get; init; } = 0.0;
}