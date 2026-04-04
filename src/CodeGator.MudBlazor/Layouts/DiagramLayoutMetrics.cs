namespace CodeGator.MudBlazor;

/// <summary>
/// This class holds default numeric constants shared by built-in diagram layouts.
/// </summary>
/// <remarks>
/// Values cover spacing, origin placement, and radial layout defaults.
/// </remarks>
public static class DiagramLayoutMetrics
{
    /// <summary>
    /// This field defines the default horizontal spacing between adjacent nodes.
    /// </summary>
    public const double DefaultHorizontalGap = 220;

    /// <summary>
    /// This field defines the default vertical spacing between depth layers.
    /// </summary>
    public const double DefaultVerticalGap = 140;

    /// <summary>
    /// This field defines the default origin X coordinate for layout placement.
    /// </summary>
    public const double DefaultOriginX = 120;

    /// <summary>
    /// This field defines the default origin Y coordinate for layout placement.
    /// </summary>
    public const double DefaultOriginY = 90;

    /// <summary>
    /// This field defines the default radial distance between concentric rings.
    /// </summary>
    public const double DefaultRadialRingGap = 160;

    /// <summary>
    /// This field defines the default radial layout center X coordinate.
    /// </summary>
    public const double DefaultRadialCenterX = 380;

    /// <summary>
    /// This field defines the default radial layout center Y coordinate.
    /// </summary>
    public const double DefaultRadialCenterY = 280;

    /// <summary>
    /// This field defines the default minimum width reserved for leaf subtrees.
    /// </summary>
    public const double DefaultMinLeafSubtreeWidth = 220;

    /// <summary>
    /// This field defines the default gap between sibling subtrees in tree layout.
    /// </summary>
    public const double DefaultSubtreeSiblingGap = 40;
}
