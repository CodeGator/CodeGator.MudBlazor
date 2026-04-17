using Microsoft.AspNetCore.Components;

namespace CodeGator.MudBlazor;

/// <summary>
/// This class renders one diagram node inside the SVG at a translated position
/// using Mud surfaces, SVG assets, or custom templates.
/// </summary>
public partial class MudDiagramNode
{
    /// <summary>
    /// This property supplies the node model to render.
    /// </summary>
    [Parameter, EditorRequired]
    public DiagramNode Node { get; set; } = default!;

    /// <summary>
    /// This property supplies the SVG translate X coordinate as a string.
    /// </summary>
    [Parameter, EditorRequired]
    public string Tx { get; set; } = "";

    /// <summary>
    /// This property supplies the SVG translate Y coordinate as a string.
    /// </summary>
    [Parameter, EditorRequired]
    public string Ty { get; set; } = "";

    /// <summary>
    /// This property indicates whether the node is selected for highlight styling.
    /// </summary>
    [Parameter]
    public bool Selected { get; set; }

    /// <summary>
    /// This property supplies an optional template for custom node presentation.
    /// </summary>
    [Parameter]
    public RenderFragment<DiagramNodeRenderContext>? NodeTemplate { get; set; }

    /// <summary>
    /// This property indicates whether the node renders from an SVG href.
    /// </summary>
    bool IsSvgHref =>
        Node.Presentation == DiagramNodePresentation.Svg
        && !string.IsNullOrWhiteSpace(Node.SvgHref);

    /// <summary>
    /// This property indicates whether the node renders from inline SVG markup.
    /// </summary>
    bool IsSvgMarkup =>
        Node.Presentation == DiagramNodePresentation.Svg
        && !IsSvgHref
        && !string.IsNullOrWhiteSpace(Node.SvgMarkup);

    /// <summary>
    /// This property indicates whether the node uses a custom render template.
    /// </summary>
    bool IsCustom =>
        Node.Presentation == DiagramNodePresentation.Custom;

    /// <summary>
    /// This method builds the render context passed to custom templates.
    /// </summary>
    /// <returns>This return value carries node and selection for the template.</returns>
    DiagramNodeRenderContext CreateContext() =>
        new() { Node = Node, Selected = Selected };

    /// <summary>
    /// This property supplies Mud theme CSS classes for default surface nodes.
    /// </summary>
    string ThemeClass => Node.VisualStyle switch
    {
        DiagramNodeVisualStyle.Primary => "mud-theme-primary",
        DiagramNodeVisualStyle.Secondary => "mud-theme-secondary",
        DiagramNodeVisualStyle.Tertiary => "mud-theme-tertiary",
        _ => ""
    };

    /// <summary>
    /// This property supplies tooltip text derived from label and description.
    /// </summary>
    string TooltipText => string.IsNullOrWhiteSpace(Node.Description)
        ? Node.Label
        : $"{Node.Label}: {Node.Description}";

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="DiagramNode.Presentation"/> is <see cref="DiagramNodePresentation.Custom"/>
    /// and <see cref="NodeTemplate"/> is null.
    /// </exception>
    protected override void OnParametersSet()
    {
        if (IsCustom && NodeTemplate is null)
        {
            throw new InvalidOperationException(
                $"Node '{Node.Id}' uses {nameof(DiagramNodePresentation.Custom)} but {nameof(NodeTemplate)} was not provided on {nameof(MudDiagram)}.");
        }
    }
}
