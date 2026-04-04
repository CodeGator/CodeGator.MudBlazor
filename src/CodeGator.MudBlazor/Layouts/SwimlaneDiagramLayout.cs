namespace CodeGator.MudBlazor;

/// <summary>
/// This class places nodes in horizontal rows grouped by
/// <see cref="DiagramNode.SwimlaneId"/>.
/// </summary>
public sealed class SwimlaneDiagramLayout : IDiagramLayout
{
    /// <summary>
    /// This property defines the X coordinate where each row begins placing nodes.
    /// </summary>
    public double OriginX { get; init; } = DiagramLayoutMetrics.DefaultOriginX;

    /// <summary>
    /// This property defines the Y coordinate for the first swimlane row.
    /// </summary>
    public double OriginY { get; init; } = DiagramLayoutMetrics.DefaultOriginY;

    /// <summary>
    /// This property defines vertical spacing between swimlane rows.
    /// </summary>
    public double LaneRowHeight { get; init; } = 150;

    /// <summary>
    /// This property defines horizontal spacing between nodes within a lane.
    /// </summary>
    public double NodeStepX { get; init; } = DiagramLayoutMetrics.DefaultHorizontalGap;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, (double X, double Y)> Compute(
        IReadOnlyList<DiagramNode> nodes,
        IReadOnlyList<DiagramEdge> edges)
    {
        _ = edges;
        var positions = new Dictionary<string, (double X, double Y)>(StringComparer.Ordinal);
        if (nodes.Count == 0)
        {
            return positions;
        }

        var groups = nodes
            .GroupBy(LaneKey, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToList();

        for (var li = 0; li < groups.Count; li++)
        {
            var row = groups[li].OrderBy(n => n.Id, StringComparer.Ordinal).ToList();
            var y = OriginY + li * LaneRowHeight;
            for (var j = 0; j < row.Count; j++)
            {
                var x = OriginX + j * NodeStepX;
                positions[row[j].Id] = (x, y);
            }
        }

        return positions;
    }

    /// <summary>
    /// This method returns the swimlane grouping key for a node.
    /// </summary>
    /// <param name="n">This parameter supplies the node to classify.</param>
    /// <returns>This return value is the lane id or empty when unset.</returns>
    static string LaneKey(DiagramNode n) =>
        string.IsNullOrEmpty(n.SwimlaneId) ? "" : n.SwimlaneId!;
}
