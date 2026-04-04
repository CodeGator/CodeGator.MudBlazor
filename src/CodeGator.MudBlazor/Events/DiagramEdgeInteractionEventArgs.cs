namespace CodeGator.MudBlazor;

/// <summary>
/// This class carries arguments for edge click and context-menu callbacks raised
/// from pointer interaction.
/// </summary>
public sealed class DiagramEdgeInteractionEventArgs : EventArgs
{
    /// <summary>
    /// This property exposes the edge that was clicked or right-clicked.
    /// </summary>
    public required DiagramEdge Edge { get; init; }

    /// <summary>
    /// This property exposes the horizontal pointer coordinate in viewport pixels.
    /// </summary>
    public double ClientX { get; init; }

    /// <summary>
    /// This property exposes the vertical pointer coordinate in viewport pixels.
    /// </summary>
    public double ClientY { get; init; }
}
