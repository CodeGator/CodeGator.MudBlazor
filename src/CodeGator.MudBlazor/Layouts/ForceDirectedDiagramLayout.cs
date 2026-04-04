namespace CodeGator.MudBlazor;

/// <summary>
/// This class positions nodes using an iterative force-directed simulation.
/// </summary>
/// <remarks>
/// The simulation applies attraction along edges and repulsion between nodes until
/// positions stabilize.
/// </remarks>
public sealed class ForceDirectedDiagramLayout : IDiagramLayout
{
    /// <summary>
    /// This property defines how many simulation iterations to run.
    /// </summary>
    public int Iterations { get; init; } = 90;

    /// <summary>
    /// This property defines the initial placement radius for seed positions.
    /// </summary>
    public double InitialRadius { get; init; } = 220;

    /// <summary>
    /// This property defines the X coordinate of the simulation center.
    /// </summary>
    public double CenterX { get; init; } = 420;

    /// <summary>
    /// This property defines the Y coordinate of the simulation center.
    /// </summary>
    public double CenterY { get; init; } = 320;

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

        var list = nodes.OrderBy(n => n.Id, StringComparer.Ordinal).ToList();
        var n = list.Count;
        var idToIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < n; i++)
        {
            idToIndex[list[i].Id] = i;
        }

        var px = new double[n];
        var py = new double[n];
        var r = Math.Min(InitialRadius, 80 + n * 28);
        for (var i = 0; i < n; i++)
        {
            var theta = 2 * Math.PI * i / Math.Max(1, n) - Math.PI / 2;
            px[i] = CenterX + r * Math.Cos(theta);
            py[i] = CenterY + r * Math.Sin(theta);
        }

        var area = Math.Max(400.0 * 300.0, n * 18000.0);
        var k = Math.Sqrt(area / Math.Max(1, n));
        var t0 = Math.Min(200.0, 40 + n * 8);
        const double eps = 1e-4;

        var edgePairs = new List<(int U, int V)>();
        foreach (var e in edges)
        {
            if (!idToIndex.TryGetValue(e.FromId, out var u) || !idToIndex.TryGetValue(e.ToId, out var v))
            {
                continue;
            }

            if (u == v)
            {
                continue;
            }

            edgePairs.Add((u, v));
        }

        var dispX = new double[n];
        var dispY = new double[n];

        for (var iter = 0; iter < Iterations; iter++)
        {
            var temperature = t0 * (1.0 - iter / (double)Math.Max(1, Iterations));

            Array.Clear(dispX);
            Array.Clear(dispY);

            for (var i = 0; i < n; i++)
            {
                for (var j = i + 1; j < n; j++)
                {
                    var dx = px[j] - px[i];
                    var dy = py[j] - py[i];
                    var dist = Math.Sqrt(dx * dx + dy * dy) + eps;
                    var rep = k * k / dist;
                    var fx = dx / dist * rep;
                    var fy = dy / dist * rep;
                    dispX[i] -= fx;
                    dispY[i] -= fy;
                    dispX[j] += fx;
                    dispY[j] += fy;
                }
            }

            foreach (var (u, v) in edgePairs)
            {
                var dx = px[v] - px[u];
                var dy = py[v] - py[u];
                var dist = Math.Sqrt(dx * dx + dy * dy) + eps;
                var att = dist * dist / k;
                var fx = dx / dist * att;
                var fy = dy / dist * att;
                dispX[u] += fx;
                dispY[u] += fy;
                dispX[v] -= fx;
                dispY[v] -= fy;
            }

            for (var i = 0; i < n; i++)
            {
                var mag = Math.Sqrt(dispX[i] * dispX[i] + dispY[i] * dispY[i]) + eps;
                var step = Math.Min(mag, temperature);
                px[i] += dispX[i] / mag * step;
                py[i] += dispY[i] / mag * step;
            }
        }

        for (var i = 0; i < n; i++)
        {
            positions[list[i].Id] = (px[i], py[i]);
        }

        return positions;
    }
}
