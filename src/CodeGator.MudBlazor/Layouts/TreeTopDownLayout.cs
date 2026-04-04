namespace CodeGator.MudBlazor;

/// <summary>
/// This class lays out a directed forest as layered trees with balanced subtree
/// widths.
/// </summary>
public sealed class TreeTopDownLayout : IDiagramLayout
{
    /// <summary>
    /// This property defines vertical spacing between parent and child rows.
    /// </summary>
    public double VerticalGap { get; init; } = DiagramLayoutMetrics.DefaultVerticalGap;

    /// <summary>
    /// This property defines the X coordinate where subtree placement begins.
    /// </summary>
    public double OriginX { get; init; } = DiagramLayoutMetrics.DefaultOriginX;

    /// <summary>
    /// This property defines the Y coordinate for the root depth row.
    /// </summary>
    public double OriginY { get; init; } = DiagramLayoutMetrics.DefaultOriginY;

    /// <summary>
    /// This property defines the minimum horizontal width reserved for leaf nodes.
    /// </summary>
    public double MinLeafSubtreeWidth { get; init; } = DiagramLayoutMetrics.DefaultMinLeafSubtreeWidth;

    /// <summary>
    /// This property defines horizontal spacing between sibling subtrees.
    /// </summary>
    public double SubtreeSiblingGap { get; init; } = DiagramLayoutMetrics.DefaultSubtreeSiblingGap;

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

        var idSet = new HashSet<string>(nodes.Select(n => n.Id), StringComparer.Ordinal);
        var childSets = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var indegree = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var n in nodes)
        {
            childSets[n.Id] = new HashSet<string>(StringComparer.Ordinal);
            indegree[n.Id] = 0;
        }

        foreach (var e in edges)
        {
            if (!idSet.Contains(e.FromId) || !idSet.Contains(e.ToId))
            {
                continue;
            }

            if (childSets[e.FromId].Add(e.ToId))
            {
                indegree[e.ToId]++;
            }
        }

        var children = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var kv in childSets)
        {
            children[kv.Key] = kv.Value.OrderBy(x => x, StringComparer.Ordinal).ToList();
        }

        var roots = nodes.Where(n => indegree[n.Id] == 0).Select(n => n.Id).OrderBy(x => x, StringComparer.Ordinal).ToList();
        if (roots.Count == 0)
        {
            roots = [nodes.Select(n => n.Id).OrderBy(x => x, StringComparer.Ordinal).First()];
        }

        var cursor = OriginX;
        foreach (var root in roots)
        {
            var (width, _) = PlaceSubtree(root, 0, cursor, positions, children);
            cursor += width + SubtreeSiblingGap;
        }

        return positions;
    }

    /// <summary>
    /// This method recursively assigns positions for a subtree rooted at the given
    /// node.
    /// </summary>
    /// <param name="id">This parameter identifies the subtree root node.</param>
    /// <param name="depth">This parameter supplies the zero-based depth row.</param>
    /// <param name="leftEdge">This parameter supplies the left boundary for layout.</param>
    /// <param name="positions">This parameter receives assigned positions.</param>
    /// <param name="childrenMap">This parameter maps parents to ordered children.</param>
    /// <returns>
    /// This return value supplies total subtree width and the parent center X.
    /// </returns>
    (double width, double midX) PlaceSubtree(
        string id,
        int depth,
        double leftEdge,
        Dictionary<string, (double X, double Y)> positions,
        Dictionary<string, List<string>> childrenMap)
    {
        var rowY = OriginY + depth * VerticalGap;
        var ch = childrenMap[id];
        if (ch.Count == 0)
        {
            var cx = leftEdge + MinLeafSubtreeWidth / 2.0;
            positions[id] = (cx, rowY);
            return (MinLeafSubtreeWidth, cx);
        }

        var cur = leftEdge;
        var childMids = new List<double>();
        foreach (var c in ch)
        {
            var (w, mid) = PlaceSubtree(c, depth + 1, cur, positions, childrenMap);
            childMids.Add(mid);
            cur += w + SubtreeSiblingGap;
        }

        cur -= SubtreeSiblingGap;
        var parentX = ch.Count == 1
            ? childMids[0]
            : (childMids.Min() + childMids.Max()) / 2.0;
        positions[id] = (parentX, rowY);
        var totalWidth = cur - leftEdge;
        return (totalWidth, parentX);
    }
}
