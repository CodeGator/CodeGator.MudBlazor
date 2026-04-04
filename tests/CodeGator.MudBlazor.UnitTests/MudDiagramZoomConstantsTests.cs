namespace CodeGator.MudBlazor.UnitTests;

using CodeGator.MudBlazor;

/// <summary>
/// This class contains unit tests for public zoom-related constants on <see cref="MudDiagram"/>.
/// </summary>
/// <remarks>
/// These tests guard basic ordering and positivity invariants for default zoom behavior.
/// </remarks>
public sealed class MudDiagramZoomConstantsTests
{
    /// <summary>
    /// This method verifies that default zoom limits and scale constants are internally consistent.
    /// </summary>
    [Fact]
    public void Zoom_constants_are_ordered_and_sensible()
    {
        Assert.True(MudDiagram.MinZoomDefault > 0);
        Assert.True(MudDiagram.MaxZoomDefault >= MudDiagram.DefaultZoomLevel);
        Assert.True(MudDiagram.DefaultZoomLevel >= MudDiagram.MinZoomDefault);
        Assert.True(MudDiagram.ZoomToEffectiveScale > 0);
    }
}
