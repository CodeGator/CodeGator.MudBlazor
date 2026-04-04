namespace CodeGator.MudBlazor;

/// <summary>
/// This class provides graph helpers used by layout algorithms.
/// </summary>
/// <remarks>
/// Helpers include polyforest detection and shortest-path depth maps from roots.
/// </remarks>
public static class DiagramGraphTopology
{
    /// <summary>
    /// This method determines whether the directed graph is a polyforest.
    /// </summary>
    /// <remarks>
    /// A polyforest allows at most one incoming edge per node.
    /// </remarks>
    /// <param name="nodes">This parameter supplies the node set.</param>
    /// <param name="edges">This parameter supplies directed edges.</param>
    /// <returns>This return value is <c>true</c> when the graph is a polyforest.</returns>
    public static bool IsPolyforest(IReadOnlyList<DiagramNode> nodes, IReadOnlyList<DiagramEdge> edges)
    {
        var idSet = new HashSet<string>(nodes.Select(n => n.Id), StringComparer.Ordinal);
        var incoming = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var n in nodes)
        {
            incoming[n.Id] = 0;
        }

        foreach (var e in edges)
        {
            if (!idSet.Contains(e.FromId) || !idSet.Contains(e.ToId))
            {
                continue;
            }

            incoming[e.ToId]++;
            if (incoming[e.ToId] > 1)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// This method computes shortest-path depths from roots in the directed graph.
    /// </summary>
    /// <param name="nodes">This parameter supplies the node set.</param>
    /// <param name="edges">This parameter supplies directed edges.</param>
    /// <returns>
    /// This return value maps each node id to its non-negative depth.
    /// </returns>
    public static Dictionary<string, int> ComputeShortestDepths(
        IReadOnlyList<DiagramNode> nodes,
        IReadOnlyList<DiagramEdge> edges)
    {
        var idSet = new HashSet<string>(nodes.Select(n => n.Id), StringComparer.Ordinal);
        var hasIncoming = new HashSet<string>(StringComparer.Ordinal);
        foreach (var e in edges)
        {
            if (idSet.Contains(e.ToId) && idSet.Contains(e.FromId))
            {
                hasIncoming.Add(e.ToId);
            }
        }

        var roots = nodes.Where(n => !hasIncoming.Contains(n.Id)).Select(n => n.Id).ToList();
        if (roots.Count == 0)
        {
            roots = nodes.Select(n => n.Id).OrderBy(x => x, StringComparer.Ordinal).Take(1).ToList();
        }

        var depth = new Dictionary<string, int>(StringComparer.Ordinal);
        var queue = new Queue<string>();
        foreach (var r in roots)
        {
            depth[r] = 0;
            queue.Enqueue(r);
        }

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            var d = depth[id];
            foreach (var edge in edges)
            {
                if (edge.FromId != id)
                {
                    continue;
                }

                if (!idSet.Contains(edge.ToId))
                {
                    continue;
                }

                if (depth.TryGetValue(edge.ToId, out var existing) && existing <= d + 1)
                {
                    continue;
                }

                depth[edge.ToId] = d + 1;
                queue.Enqueue(edge.ToId);
            }
        }

        var maxAssigned = depth.Count > 0 ? depth.Values.Max() : -1;
        var fallback = maxAssigned + 1;
        if (fallback < 0)
        {
            fallback = 0;
        }

        foreach (var n in nodes)
        {
            if (!depth.ContainsKey(n.Id))
            {
                depth[n.Id] = fallback;
            }
        }

        return depth;
    }
}
