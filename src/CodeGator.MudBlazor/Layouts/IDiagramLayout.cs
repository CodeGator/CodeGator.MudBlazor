namespace CodeGator.MudBlazor;

/// <summary>
/// This interface represents layout positioning from the current nodes and edges.
/// </summary>
/// <remarks>
/// Implementations return a map of node id to content coordinates; the diagram host
/// applies zoom and pan separately.
/// </remarks>
public interface IDiagramLayout
{
    /// <summary>
    /// This method computes node positions for the supplied graph.
    /// </summary>
    /// <param name="nodes">This parameter supplies the nodes to place.</param>
    /// <param name="edges">This parameter supplies directed edges between nodes.</param>
    /// <returns>
    /// This return value maps each node id to its X/Y position in content space.
    /// </returns>
    IReadOnlyDictionary<string, (double X, double Y)> Compute(
        IReadOnlyList<DiagramNode> nodes,
        IReadOnlyList<DiagramEdge> edges);
}
