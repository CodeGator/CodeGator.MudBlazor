namespace CodeGator.MudBlazor;

/// <summary>
/// This class arranges nodes on concentric rings by shortest-path depth from
/// roots.
/// </summary>
public sealed class RadialDiagramLayout : IDiagramLayout
{
    /// <summary>
    /// This property defines the radial distance between depth rings.
    /// </summary>
    public double RingGap { get; init; } = DiagramLayoutMetrics.DefaultRadialRingGap;

    /// <summary>
    /// This property defines the X coordinate of the layout center.
    /// </summary>
    public double CenterX { get; init; } = DiagramLayoutMetrics.DefaultRadialCenterX;

    /// <summary>
    /// This property defines the Y coordinate of the layout center.
    /// </summary>
    public double CenterY { get; init; } = DiagramLayoutMetrics.DefaultRadialCenterY;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, (double X, double Y)> Compute(
        IReadOnlyList<DiagramNode> nodes,
        IReadOnlyList<DiagramEdge> edges)
    {
        var positions = new Dictionary<string, (double X, double Y)>(StringComparer.Ordinal);
        if (nodes.Count == 0)
        {
            return positions;
        }

        var depths = DiagramGraphTopology.ComputeShortestDepths(nodes, edges);
        var byDepth = nodes
            .GroupBy(n => depths[n.Id])
            .OrderBy(g => g.Key);

        foreach (var group in byDepth)
        {
            var list = group.OrderBy(n => n.Id, StringComparer.Ordinal).ToList();
            var ring = group.Key * RingGap;
            if (list.Count == 1 && ring == 0)
            {
                positions[list[0].Id] = (CenterX, CenterY);
                continue;
            }

            var n = list.Count;
            for (var i = 0; i < n; i++)
            {
                var theta = 2 * Math.PI * i / n - Math.PI / 2;
                var x = CenterX + ring * Math.Cos(theta);
                var y = CenterY + ring * Math.Sin(theta);
                positions[list[i].Id] = (x, y);
            }
        }

        return positions;
    }
}
