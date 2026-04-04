namespace CodeGator.MudBlazor;

/// <summary>
/// This class describes a diagram vertex with identity, label, presentation, and
/// optional swimlane grouping.
/// </summary>
public sealed class DiagramNode
{
    /// <summary>
    /// This constructor initializes a diagram node with identifiers, optional
    /// description, visuals, and swimlane membership.
    /// </summary>
    /// <param name="id">This parameter specifies the stable node identifier.</param>
    /// <param name="label">This parameter specifies the display label.</param>
    /// <param name="description">This parameter specifies optional detail text.</param>
    /// <param name="visualStyle">This parameter selects Mud theme styling.</param>
    /// <param name="presentation">This parameter selects how the node is rendered.</param>
    /// <param name="svgHref">This parameter specifies an optional SVG asset URL.</param>
    /// <param name="svgMarkup">This parameter specifies optional inline SVG markup.</param>
    /// <param name="swimlaneId">This parameter specifies optional swimlane grouping.</param>
    public DiagramNode(
        string id,
        string label,
        string? description = null,
        DiagramNodeVisualStyle visualStyle = DiagramNodeVisualStyle.Default,
        DiagramNodePresentation presentation = DiagramNodePresentation.Surface,
        string? svgHref = null,
        string? svgMarkup = null,
        string? swimlaneId = null)
    {
        Id = id;
        Label = label;
        Description = description;
        VisualStyle = visualStyle;
        Presentation = presentation;
        SvgHref = svgHref;
        SvgMarkup = svgMarkup;
        SwimlaneId = swimlaneId;
    }

    /// <summary>
    /// This property exposes the stable identifier used for edges and layout keys.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// This property exposes the primary display text for the node.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// This property exposes optional longer description shown in tooltips or
    /// detail panels.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// This property selects Mud theme styling for default surface presentation.
    /// </summary>
    public DiagramNodeVisualStyle VisualStyle { get; }

    /// <summary>
    /// This property selects how the host renders the node inside the SVG.
    /// </summary>
    public DiagramNodePresentation Presentation { get; }

    /// <summary>
    /// This property exposes an optional SVG asset URL when using SVG presentation.
    /// </summary>
    public string? SvgHref { get; }

    /// <summary>
    /// This property exposes optional inline SVG markup when using SVG presentation.
    /// </summary>
    public string? SvgMarkup { get; }

    /// <summary>
    /// This property exposes an optional swimlane key for swimlane layouts.
    /// </summary>
    public string? SwimlaneId { get; }
}
