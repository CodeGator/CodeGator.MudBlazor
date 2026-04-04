namespace CodeGator.MudBlazor;

/// <summary>
/// This class carries arguments for node click and context-menu callbacks raised
/// from pointer interaction.
/// </summary>
public sealed class DiagramNodeInteractionEventArgs : EventArgs
{
    /// <summary>
    /// This property exposes the node that was clicked or right-clicked.
    /// </summary>
    public required DiagramNode Node { get; init; }

    /// <summary>
    /// This property exposes the horizontal pointer coordinate in viewport pixels.
    /// </summary>
    public double ClientX { get; init; }

    /// <summary>
    /// This property exposes the vertical pointer coordinate in viewport pixels.
    /// </summary>
    public double ClientY { get; init; }
}
