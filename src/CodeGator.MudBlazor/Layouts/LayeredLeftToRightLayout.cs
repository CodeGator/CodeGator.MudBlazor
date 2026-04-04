namespace CodeGator.MudBlazor;

/// <summary>
/// This class lays out nodes in columns by depth, spreading each layer vertically.
/// </summary>
public sealed class LayeredLeftToRightLayout : IDiagramLayout
{
    /// <summary>
    /// This property defines horizontal spacing between depth columns.
    /// </summary>
    public double HorizontalGap { get; init; } = DiagramLayoutMetrics.DefaultHorizontalGap;

    /// <summary>
    /// This property defines vertical spacing between nodes within a column.
    /// </summary>
    public double VerticalGap { get; init; } = DiagramLayoutMetrics.DefaultVerticalGap;

    /// <summary>
    /// This property defines the X coordinate for the shallowest depth column.
    /// </summary>
    public double OriginX { get; init; } = DiagramLayoutMetrics.DefaultOriginX;

    /// <summary>
    /// This property defines the Y origin used when centering each column.
    /// </summary>
    public double OriginY { get; init; } = DiagramLayoutMetrics.DefaultOriginY;

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
            var colX = OriginX + group.Key * HorizontalGap;
            var height = Math.Max(0, (list.Count - 1) * VerticalGap);
            var startY = OriginY + (list.Count == 1 ? 0 : -height / 2.0);

            for (var i = 0; i < list.Count; i++)
            {
                positions[list[i].Id] = (colX, startY + i * VerticalGap);
            }
        }

        return positions;
    }
}
