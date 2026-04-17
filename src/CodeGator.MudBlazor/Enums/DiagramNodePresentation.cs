namespace CodeGator.MudBlazor;

/// <summary>
/// This enumeration represents how diagram nodes are drawn in the SVG.
/// </summary>
/// <remarks>
/// Options include Mud surface cards, SVG assets or inline markup, or custom HTML
/// via <c>foreignObject</c>.
/// </remarks>
public enum DiagramNodePresentation
{
    /// <summary>
    /// This enumeration member renders the node using Mud surface components.
    /// </summary>
    Surface,

    /// <summary>
    /// This enumeration member renders the node using SVG assets or inline markup.
    /// </summary>
    Svg,

    /// <summary>
    /// This enumeration member renders the node using a host-supplied template.
    /// </summary>
    Custom
}
