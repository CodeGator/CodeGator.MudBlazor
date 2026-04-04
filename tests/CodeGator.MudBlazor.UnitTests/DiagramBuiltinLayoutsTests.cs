namespace CodeGator.MudBlazor.UnitTests;

using CodeGator.MudBlazor;

/// <summary>
/// This class contains unit tests for <see cref="DiagramBuiltinLayouts"/>.
/// </summary>
/// <remarks>
/// These tests ensure each <see cref="DiagramLayoutKind"/> resolves to a layout implementation
/// and that invalid enumeration values are rejected.
/// </remarks>
public sealed class DiagramBuiltinLayoutsTests
{
    /// <summary>
    /// This field supplies every defined <see cref="DiagramLayoutKind"/> value for theory tests.
    /// </summary>
    public static TheoryData<DiagramLayoutKind> AllLayoutKinds =>
    [
        DiagramLayoutKind.HierarchicalTopDown,
        DiagramLayoutKind.HierarchicalLeftToRight,
        DiagramLayoutKind.Radial,
        DiagramLayoutKind.ForceDirected,
        DiagramLayoutKind.Swimlanes,
        DiagramLayoutKind.CircularRing,
    ];

    /// <summary>
    /// This method verifies that <see cref="DiagramBuiltinLayouts.For"/> returns a non-null layout instance.
    /// </summary>
    /// <param name="kind">This parameter supplies the layout kind to resolve.</param>
    [Theory]
    [MemberData(nameof(AllLayoutKinds))]
    public void For_returns_layout_instance(DiagramLayoutKind kind)
    {
        var layout = DiagramBuiltinLayouts.For(kind);

        Assert.NotNull(layout);
        Assert.IsAssignableFrom<IDiagramLayout>(layout);
    }

    /// <summary>
    /// This method verifies that an undefined <see cref="DiagramLayoutKind"/> value throws
    /// <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [Fact]
    public void For_throws_for_invalid_enum_value()
    {
        var invalid = (DiagramLayoutKind)999;

        Assert.Throws<ArgumentOutOfRangeException>(() => DiagramBuiltinLayouts.For(invalid));
    }
}
