namespace CodeGator.MudBlazor;

/// <summary>
/// This class lays out nodes in rows by depth, spreading each layer horizontally.
/// </summary>
public sealed class LayeredTopDownLayout : IDiagramLayout
{
    /// <summary>
    /// This property defines horizontal spacing between nodes within a row.
    /// </summary>
    public double HorizontalGap { get; init; } = DiagramLayoutMetrics.DefaultHorizontalGap;

    /// <summary>
    /// This property defines vertical spacing between depth layers.
    /// </summary>
    public double VerticalGap { get; init; } = DiagramLayoutMetrics.DefaultVerticalGap;

    /// <summary>
    /// This property defines the X origin used when centering each row.
    /// </summary>
    public double OriginX { get; init; } = DiagramLayoutMetrics.DefaultOriginX;

    /// <summary>
    /// This property defines the Y coordinate for the shallowest depth row.
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
            var rowY = OriginY + group.Key * VerticalGap;
            var width = Math.Max(0, (list.Count - 1) * HorizontalGap);
            var startX = OriginX + (list.Count == 1 ? 0 : -width / 2.0);

            for (var i = 0; i < list.Count; i++)
            {
                positions[list[i].Id] = (startX + i * HorizontalGap, rowY);
            }
        }

        return positions;
    }
}
