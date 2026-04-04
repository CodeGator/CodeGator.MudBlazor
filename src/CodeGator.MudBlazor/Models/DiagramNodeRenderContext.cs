namespace CodeGator.MudBlazor;

/// <summary>
/// This class passes the current node and selection state into custom node render
/// fragments.
/// </summary>
public sealed class DiagramNodeRenderContext
{
    /// <summary>
    /// This property exposes the node being rendered by the template.
    /// </summary>
    public required DiagramNode Node { get; init; }

    /// <summary>
    /// This property indicates whether the node is currently selected.
    /// </summary>
    public required bool Selected { get; init; }
}
