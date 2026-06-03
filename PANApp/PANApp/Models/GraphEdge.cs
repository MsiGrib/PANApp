using Avalonia;

namespace PANApp.Models;

public sealed record GraphEdge
{
    public Point StartPoint { get; set; }
    public Point EndPoint { get; set; }
    public Point ArrowTip { get; set; }
    public Point ArrowLeft { get; set; }
    public Point ArrowRight { get; set; }
}