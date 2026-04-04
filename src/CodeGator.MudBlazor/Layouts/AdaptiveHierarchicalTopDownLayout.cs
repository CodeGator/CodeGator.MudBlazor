namespace CodeGator.MudBlazor;

/// <summary>
/// This class selects a hierarchical top-down strategy based on whether the graph
/// is a polyforest.
/// </summary>
/// <remarks>
/// Polyforests use <see cref="TreeTopDownLayout"/>; general directed graphs use
/// <see cref="LayeredTopDownLayout"/>.
/// </remarks>
public sealed class AdaptiveHierarchicalTopDownLayout : IDiagramLayout
{
    /// <summary>
    /// This field holds the tree layout used when the graph is a polyforest.
    /// </summary>
    readonly TreeTopDownLayout _tree = new();

    /// <summary>
    /// This field holds the layered layout used for general directed graphs.
    /// </summary>
    readonly LayeredTopDownLayout _layered = new();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, (double X, double Y)> Compute(
        IReadOnlyList<DiagramNode> nodes,
        IReadOnlyList<DiagramEdge> edges)
    {
        if (nodes.Count == 0)
        {
            return new Dictionary<string, (double X, double Y)>(StringComparer.Ordinal);
        }

        return DiagramGraphTopology.IsPolyforest(nodes, edges)
            ? _tree.Compute(nodes, edges)
            : _layered.Compute(nodes, edges);
    }
}
