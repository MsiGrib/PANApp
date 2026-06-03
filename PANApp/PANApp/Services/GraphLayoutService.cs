using Avalonia;
using PANApp.Comparers;
using PANApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PANApp.Services;

public class GraphLayoutService
{
    private const double OuterMargin = 140;
    private const double LevelGapX = 520;
    private const double ComponentGapY = 180;
    private const double CellGapX = 280;
    private const double CellGapY = 150;
    private const double ArrowLength = 12;
    private const double ArrowWidth = 8;

    public GraphLayoutResult CalculateLayoutAndEdges(List<GraphNode> nodes, List<(string SourceName, string TargetName)> edges)
    {
        if (nodes.Count == 0) return new GraphLayoutResult();

        var nodeMap = nodes.ToDictionary(n => n.Id, StringComparer.OrdinalIgnoreCase);

        var validEdges = edges
            .Where(e =>
                nodeMap.ContainsKey(e.SourceName) &&
                nodeMap.ContainsKey(e.TargetName) &&
                !string.Equals(e.SourceName, e.TargetName, StringComparison.OrdinalIgnoreCase))
            .Distinct(new EdgeComparer())
            .ToList();

        var outgoing = nodeMap.Keys.ToDictionary(
            id => id,
            _ => new List<string>(),
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var (source, target) in validEdges)
            outgoing[source].Add(target);

        var components = GetStronglyConnectedComponents(nodeMap.Keys, outgoing);

        var componentIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < components.Count; i++)
        {
            foreach (var id in components[i])
                componentIndex[id] = i;
        }

        var compOutgoing = components.Select(_ => new HashSet<int>()).ToList();
        var compInDegree = new int[components.Count];

        foreach (var (source, target) in validEdges)
        {
            var cs = componentIndex[source];
            var ct = componentIndex[target];

            if (cs == ct) continue;

            if (compOutgoing[cs].Add(ct))
                compInDegree[ct]++;
        }

        var compLevel = new int[components.Count];
        var queue = new Queue<int>(Enumerable.Range(0, components.Count).Where(i => compInDegree[i] == 0));

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var next in compOutgoing[current])
            {
                compLevel[next] = Math.Max(compLevel[next], compLevel[current] + 1);
                compInDegree[next]--;

                if (compInDegree[next] == 0) queue.Enqueue(next);
            }
        }

        var groups = components
            .Select((ids, idx) => new
            {
                Index = idx,
                Level = compLevel[idx],
                Nodes = ids.Select(id => nodeMap[id])
                    .OrderBy(n => n.DisplayName)
                    .ThenBy(n => n.FullName)
                    .ToList()
            })
            .GroupBy(x => x.Level)
            .OrderBy(g => g.Key)
            .ToList();

        var maxX = 0.0;
        var maxY = 0.0;

        foreach (var levelGroup in groups)
        {
            var x = OuterMargin + levelGroup.Key * LevelGapX;
            var currentY = OuterMargin;

            foreach (var component in levelGroup
                .OrderByDescending(c => c.Nodes.Count)
                .ThenBy(c => c.Nodes[0].DisplayName))
            {
                var componentHeight = PlaceComponent(component.Nodes, x, currentY);
                currentY += componentHeight + ComponentGapY;
            }
        }

        var resultEdges = new List<GraphEdge>();

        foreach (var (sourceName, targetName) in validEdges)
        {
            var source = nodeMap[sourceName];
            var target = nodeMap[targetName];

            var sourceCenter = new Point(
                source.X + source.Width / 2,
                source.Y + source.Height / 2
            );

            var targetCenter = new Point(
                target.X + target.Width / 2,
                target.Y + target.Height / 2
            );

            var startPt = GetEdgePoint(sourceCenter, source.Width, source.Height, targetCenter);
            var endPt = GetEdgePoint(targetCenter, target.Width, target.Height, sourceCenter);

            var dx = endPt.X - startPt.X;
            var dy = endPt.Y - startPt.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);

            if (length < 0.0001) continue;

            var ux = dx / length;
            var uy = dy / length;

            var tip = endPt;
            var basePt = new Point(tip.X - ux * ArrowLength, tip.Y - uy * ArrowLength);

            var perpX = -uy * (ArrowWidth / 2);
            var perpY = ux * (ArrowWidth / 2);

            var left = new Point(basePt.X + perpX, basePt.Y + perpY);
            var right = new Point(basePt.X - perpX, basePt.Y - perpY);

            resultEdges.Add(new GraphEdge
            {
                StartPoint = startPt,
                EndPoint = basePt,
                ArrowTip = tip,
                ArrowLeft = left,
                ArrowRight = right
            });
        }

        maxX = nodes.Max(n => n.X + n.Width);
        maxY = nodes.Max(n => n.Y + n.Height);

        return new GraphLayoutResult
        {
            Edges = resultEdges,
            Width = maxX + OuterMargin,
            Height = maxY + OuterMargin
        };
    }

    private static double PlaceComponent(List<GraphNode> nodes, double left, double top)
    {
        if (nodes.Count == 0) return 0;

        if (nodes.Count == 1)
        {
            var node = nodes[0];
            node.X = left;
            node.Y = top;

            return node.Height + 40;
        }

        var cols = Math.Min(3, (int)Math.Ceiling(Math.Sqrt(nodes.Count)));
        var rows = (int)Math.Ceiling(nodes.Count / (double)cols);

        for (int i = 0; i < nodes.Count; i++)
        {
            var row = i / cols;
            var col = i % cols;

            nodes[i].X = left + 20 + col * CellGapX;
            nodes[i].Y = top + 20 + row * CellGapY;
        }

        return rows * CellGapY + 40;
    }

    private static Point GetEdgePoint(Point fromCenter, double width, double height, Point toCenter)
    {
        var dx = toCenter.X - fromCenter.X;
        var dy = toCenter.Y - fromCenter.Y;

        if (Math.Abs(dx) < 0.0001 && Math.Abs(dy) < 0.0001) return fromCenter;

        var scaleX = double.PositiveInfinity;
        var scaleY = double.PositiveInfinity;

        if (Math.Abs(dx) > 0.0001) scaleX = (width / 2) / Math.Abs(dx);

        if (Math.Abs(dy) > 0.0001) scaleY = (height / 2) / Math.Abs(dy);

        var scale = Math.Min(scaleX, scaleY);

        return new Point(fromCenter.X + dx * scale, fromCenter.Y + dy * scale);
    }

    private static List<List<string>> GetStronglyConnectedComponents(IEnumerable<string> nodeIds, Dictionary<string, List<string>> outgoing)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var order = new List<string>();

        void Dfs1(string id)
        {
            visited.Add(id);

            foreach (var next in outgoing[id])
                if (!visited.Contains(next)) Dfs1(next);

            order.Add(id);
        }

        foreach (var id in nodeIds)
            if (!visited.Contains(id)) Dfs1(id);

        var reversed = nodeIds.ToDictionary(
            id => id,
            _ => new List<string>(),
            StringComparer.OrdinalIgnoreCase);

        foreach (var (source, targets) in outgoing)
        {
            foreach (var target in targets)
                reversed[target].Add(source);
        }

        var visited2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var components = new List<List<string>>();

        void Dfs2(string id, List<string> component)
        {
            visited2.Add(id);
            component.Add(id);

            foreach (var next in reversed[id])
                if (!visited2.Contains(next)) Dfs2(next, component);
        }

        for (int i = order.Count - 1; i >= 0; i--)
        {
            var id = order[i];
            if (visited2.Contains(id)) continue;

            var component = new List<string>();
            Dfs2(id, component);
            components.Add(component);
        }

        return components;
    }
}