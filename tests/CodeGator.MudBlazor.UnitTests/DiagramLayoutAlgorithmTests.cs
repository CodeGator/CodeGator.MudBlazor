namespace CodeGator.MudBlazor.UnitTests;

using CodeGator.MudBlazor;

/// <summary>
/// This class contains unit tests for built-in <see cref="IDiagramLayout"/> implementations.
/// </summary>
/// <remarks>
/// These tests assert that small sample graphs receive a finite position for every node.
/// </remarks>
public sealed class DiagramLayoutAlgorithmTests
{
    /// <summary>
    /// This field holds a three-node chain used by most layout tests.
    /// </summary>
    static readonly DiagramNode[] SampleNodes =
    [
        new("n1", "A"),
        new("n2", "B"),
        new("n3", "C"),
    ];

    /// <summary>
    /// This field holds edges matching <see cref="SampleNodes"/> in sequence.
    /// </summary>
    static readonly DiagramEdge[] SampleEdges =
    [
        new("n1", "n2"),
        new("n2", "n3"),
    ];

    /// <summary>
    /// This field holds nodes with distinct swimlane ids for swimlane layout tests.
    /// </summary>
    static readonly DiagramNode[] SwimlaneNodes =
    [
        new("a", "A", swimlaneId: "Lane1"),
        new("b", "B", swimlaneId: "Lane2"),
    ];

    /// <summary>
    /// This method verifies that <see cref="AdaptiveHierarchicalTopDownLayout"/> returns no positions when the graph is empty.
    /// </summary>
    [Fact]
    public void AdaptiveHierarchicalTopDownLayout_returns_empty_for_no_nodes()
    {
        var layout = new AdaptiveHierarchicalTopDownLayout();

        var positions = layout.Compute(Array.Empty<DiagramNode>(), Array.Empty<DiagramEdge>());

        Assert.Empty(positions);
    }

    /// <summary>
    /// This method verifies that each built-in layout assigns every node finite coordinates.
    /// </summary>
    /// <param name="kind">This parameter supplies which built-in layout to exercise.</param>
    [Theory]
    [MemberData(nameof(DiagramBuiltinLayoutsTests.AllLayoutKinds), MemberType = typeof(DiagramBuiltinLayoutsTests))]
    public void Builtin_layout_via_For_places_every_node(DiagramLayoutKind kind)
    {
        var layout = DiagramBuiltinLayouts.For(kind);
        var nodes = kind == DiagramLayoutKind.Swimlanes ? SwimlaneNodes : SampleNodes;

        var positions = layout.Compute(nodes, SampleEdges);

        Assert.Equal(nodes.Length, positions.Count);
        foreach (var n in nodes)
        {
            Assert.True(positions.ContainsKey(n.Id));
            var (x, y) = positions[n.Id];
            Assert.False(double.IsNaN(x));
            Assert.False(double.IsNaN(y));
            Assert.False(double.IsInfinity(x));
            Assert.False(double.IsInfinity(y));
        }
    }

    /// <summary>
    /// This method verifies that a polyforest uses the tree top-down layout ordering on the Y axis.
    /// </summary>
    [Fact]
    public void AdaptiveHierarchicalTopDownLayout_routes_polyforest_to_tree_layout()
    {
        var nodes = new[] { new DiagramNode("r", "Root"), new DiagramNode("c", "Child") };
        var edges = new[] { new DiagramEdge("r", "c") };
        Assert.True(DiagramGraphTopology.IsPolyforest(nodes, edges));

        var layout = new AdaptiveHierarchicalTopDownLayout();
        var positions = layout.Compute(nodes, edges);

        Assert.Equal(2, positions.Count);
        Assert.True(positions["r"].Y <= positions["c"].Y);
    }

    /// <summary>
    /// This method verifies that a non-polyforest graph still receives positions for all nodes.
    /// </summary>
    [Fact]
    public void AdaptiveHierarchicalTopDownLayout_routes_non_polyforest_to_layered_layout()
    {
        var nodes = new[]
        {
            new DiagramNode("a", "A"),
            new DiagramNode("b", "B"),
            new DiagramNode("c", "C"),
        };
        var edges = new[]
        {
            new DiagramEdge("a", "c"),
            new DiagramEdge("b", "c"),
        };
        Assert.False(DiagramGraphTopology.IsPolyforest(nodes, edges));

        var layout = new AdaptiveHierarchicalTopDownLayout();
        var positions = layout.Compute(nodes, edges);

        Assert.Equal(3, positions.Count);
    }
}
