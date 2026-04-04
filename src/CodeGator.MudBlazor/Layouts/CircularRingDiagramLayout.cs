namespace CodeGator.MudBlazor;

/// <summary>
/// This class lays out nodes evenly on a single circle around a configurable
/// center.
/// </summary>
public sealed class CircularRingDiagramLayout : IDiagramLayout
{
    /// <summary>
    /// This property defines the X coordinate of the ring center.
    /// </summary>
    public double CenterX { get; init; } = DiagramLayoutMetrics.DefaultRadialCenterX;

    /// <summary>
    /// This property defines the Y coordinate of the ring center.
    /// </summary>
    public double CenterY { get; init; } = DiagramLayoutMetrics.DefaultRadialCenterY;

    /// <summary>
    /// This property defines the circle radius, or zero to derive a default from
    /// node count.
    /// </summary>
    public double Radius { get; init; }

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

        var list = nodes.OrderBy(n => n.Id, StringComparer.Ordinal).ToList();
        var n = list.Count;
        var r = Radius > 0 ? Radius : Math.Max(130, 38 * n);
        for (var i = 0; i < n; i++)
        {
            var theta = 2 * Math.PI * i / n - Math.PI / 2;
            var x = CenterX + r * Math.Cos(theta);
            var y = CenterY + r * Math.Sin(theta);
            positions[list[i].Id] = (x, y);
        }

        return positions;
    }
}
