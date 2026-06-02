using Avalonia;
using PANApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PANApp.Services;

public class GraphLayoutService
{
    public List<GraphEdge> CalculateLayoutAndEdges(List<GraphNode> nodes, List<(string SourceName, string TargetName)> edges)
    {
        if (nodes.Count == 0) return new List<GraphEdge>();

        int columns = (int)Math.Ceiling(Math.Sqrt(nodes.Count));
        int spacingX = 220;
        int spacingY = 120;
        int startX = 100;
        int startY = 100;

        for (int i = 0; i < nodes.Count; i++)
        {
            nodes[i].X = startX + (i % columns) * spacingX;
            nodes[i].Y = startY + (i / columns) * spacingY;
        }

        var resultEdges = new List<GraphEdge>();

        var nodeMap = nodes.GroupBy(n => n.DisplayName)
                           .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var edge in edges)
        {
            if (!nodeMap.TryGetValue(edge.SourceName, out var source) ||
                !nodeMap.TryGetValue(edge.TargetName, out var target))
            {
                continue;
            }

            var sourceCenterX = source.X + (source.Width / 2);
            var sourceCenterY = source.Y + (source.Height / 2);
            var targetCenterX = target.X + (target.Width / 2);
            var targetCenterY = target.Y + (target.Height / 2);

            var startPt = GetEdgePoint(sourceCenterX, sourceCenterY, source.Width, source.Height, targetCenterX, targetCenterY);
            var endPt = GetEdgePoint(targetCenterX, targetCenterY, target.Width, target.Height, sourceCenterX, sourceCenterY);

            var angle = Math.Atan2(endPt.Y - startPt.Y, endPt.X - startPt.X) * 180 / Math.PI;

            resultEdges.Add(new GraphEdge
            {
                StartPoint = new Point(startPt.X, startPt.Y),
                EndPoint = new Point(endPt.X, endPt.Y),
                Angle = angle
            });
        }

        return resultEdges;
    }

    private (double X, double Y) GetEdgePoint(double cx, double cy, double w, double h, double tx, double ty)
    {
        var dx = tx - cx;
        var dy = ty - cy;
        if (Math.Abs(dx) < 0.001 && Math.Abs(dy) < 0.001) return (cx, cy);

        var scale = Math.Abs(dx) * h > Math.Abs(dy) * w ? (w / 2) / Math.Abs(dx) : (h / 2) / Math.Abs(dy);
        return (cx + dx * scale, cy + dy * scale);
    }
}