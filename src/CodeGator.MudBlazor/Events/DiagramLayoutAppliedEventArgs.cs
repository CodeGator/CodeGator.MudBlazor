namespace CodeGator.MudBlazor;

/// <summary>
/// This class reports the layout kind and node positions after a layout pass
/// completes.
/// </summary>
public sealed class DiagramLayoutAppliedEventArgs : EventArgs
{
    /// <summary>
    /// This property exposes the layout kind used for the completed pass.
    /// </summary>
    public DiagramLayoutKind LayoutKind { get; init; }

    /// <summary>
    /// This property indicates whether a custom <see cref="IDiagramLayout"/>
    /// algorithm was active.
    /// </summary>
    public bool UsesCustomLayoutAlgorithm { get; init; }

    /// <summary>
    /// This property exposes the node list associated with the layout snapshot.
    /// </summary>
    public required IReadOnlyList<DiagramNode> Nodes { get; init; }

    /// <summary>
    /// This property exposes the edge list associated with the layout snapshot.
    /// </summary>
    public required IReadOnlyList<DiagramEdge> Edges { get; init; }

    /// <summary>
    /// This property exposes node positions in diagram content coordinates after
    /// layout.
    /// </summary>
    public required IReadOnlyDictionary<string, (double X, double Y)> Positions { get; init; }
}
