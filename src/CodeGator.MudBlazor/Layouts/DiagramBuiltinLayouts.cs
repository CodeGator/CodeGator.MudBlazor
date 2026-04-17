namespace CodeGator.MudBlazor;

/// <summary>
/// This class resolves a <see cref="DiagramLayoutKind"/> to a built-in
/// <see cref="IDiagramLayout"/> implementation.
/// </summary>
public static class DiagramBuiltinLayouts
{
    /// <summary>
    /// This method returns the built-in layout implementation for the given kind.
    /// </summary>
    /// <param name="kind">This parameter selects which layout strategy to create.</param>
    /// <returns>This return value is a new layout instance for the supplied kind.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="kind"/> is not a defined <see cref="DiagramLayoutKind"/> value.
    /// </exception>
    public static IDiagramLayout For(DiagramLayoutKind kind) => kind switch
    {
        DiagramLayoutKind.HierarchicalTopDown => new AdaptiveHierarchicalTopDownLayout(),
        DiagramLayoutKind.HierarchicalLeftToRight => new LayeredLeftToRightLayout(),
        DiagramLayoutKind.Radial => new RadialDiagramLayout(),
        DiagramLayoutKind.ForceDirected => new ForceDirectedDiagramLayout(),
        DiagramLayoutKind.Swimlanes => new SwimlaneDiagramLayout(),
        DiagramLayoutKind.CircularRing => new CircularRingDiagramLayout(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
}
