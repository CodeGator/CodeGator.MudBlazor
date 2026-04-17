namespace CodeGator.MudBlazor;

/// <summary>
/// This enumeration represents built-in layout strategies selectable on
/// <see cref="MudDiagram"/>.
/// </summary>
public enum DiagramLayoutKind
{
    /// <summary>
    /// This enumeration member selects adaptive hierarchical top-down placement.
    /// </summary>
    HierarchicalTopDown,

    /// <summary>
    /// This enumeration member selects layered left-to-right placement.
    /// </summary>
    HierarchicalLeftToRight,

    /// <summary>
    /// This enumeration member selects radial rings by depth from roots.
    /// </summary>
    Radial,

    /// <summary>
    /// This enumeration member selects an iterative force-directed simulation.
    /// </summary>
    ForceDirected,

    /// <summary>
    /// This enumeration member selects swimlane rows by <c>SwimlaneId</c>.
    /// </summary>
    Swimlanes,

    /// <summary>
    /// This enumeration member selects a single circle ring for all nodes.
    /// </summary>
    CircularRing
}
