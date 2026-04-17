namespace CodeGator.MudBlazor;

/// <summary>
/// This class describes a directed link between two diagram nodes.
/// </summary>
public sealed class DiagramEdge
{
    /// <summary>
    /// This constructor initializes a new instance of the DiagramEdge class.
    /// </summary>
    /// <remarks>
    /// Connects <paramref name="fromId"/> to <paramref name="toId"/> with an optional <paramref name="label"/>.
    /// </remarks>
    /// <param name="fromId">This parameter specifies the source node id.</param>
    /// <param name="toId">This parameter specifies the target node id.</param>
    /// <param name="label">This parameter specifies an optional edge label.</param>
    public DiagramEdge(string fromId, string toId, string? label = null)
    {
        FromId = fromId;
        ToId = toId;
        Label = label;
    }

    /// <summary>
    /// This property exposes the source node identifier for the connector.
    /// </summary>
    public string FromId { get; }

    /// <summary>
    /// This property exposes the target node identifier for the connector.
    /// </summary>
    public string ToId { get; }

    /// <summary>
    /// This property exposes optional text shown on the edge or in tooltips.
    /// </summary>
    public string? Label { get; }
}
