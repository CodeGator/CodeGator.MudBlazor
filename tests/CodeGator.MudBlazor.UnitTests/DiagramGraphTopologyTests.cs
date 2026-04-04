namespace CodeGator.MudBlazor.UnitTests;

using CodeGator.MudBlazor;

/// <summary>
/// This class contains unit tests for <see cref="DiagramGraphTopology"/> graph helpers.
/// </summary>
/// <remarks>
/// These tests cover polyforest detection and shortest-path depth maps used by diagram layouts.
/// </remarks>
public sealed class DiagramGraphTopologyTests
{
    static DiagramNode N(string id, string? swimlane = null) =>
        new(id, id, swimlaneId: swimlane);

    /// <summary>
    /// This method verifies that a simple tree is classified as a polyforest.
    /// </summary>
    [Fact]
    public void IsPolyforest_returns_true_for_tree_with_single_parent_each()
    {
        var nodes = new[] { N("a"), N("b"), N("c") };
        var edges = new[] { new DiagramEdge("a", "b"), new DiagramEdge("a", "c") };

        Assert.True(DiagramGraphTopology.IsPolyforest(nodes, edges));
    }

    /// <summary>
    /// This method verifies that a node with two incoming edges is not a polyforest.
    /// </summary>
    [Fact]
    public void IsPolyforest_returns_false_when_node_has_two_incoming_edges()
    {
        var nodes = new[] { N("a"), N("b"), N("c") };
        var edges = new[]
        {
            new DiagramEdge("a", "c"),
            new DiagramEdge("b", "c"),
        };

        Assert.False(DiagramGraphTopology.IsPolyforest(nodes, edges));
    }

    /// <summary>
    /// This method verifies that edges whose endpoints are missing from the node set are ignored.
    /// </summary>
    [Fact]
    public void IsPolyforest_ignores_edges_whose_endpoints_are_not_in_the_node_set()
    {
        var nodes = new[] { N("a"), N("b") };
        var edges = new[]
        {
            new DiagramEdge("a", "b"),
            new DiagramEdge("x", "y"),
        };

        Assert.True(DiagramGraphTopology.IsPolyforest(nodes, edges));
    }

    /// <summary>
    /// This method verifies that depths increase by one along a directed chain.
    /// </summary>
    [Fact]
    public void ComputeShortestDepths_assigns_increasing_depth_along_a_chain()
    {
        var nodes = new[] { N("a"), N("b"), N("c") };
        var edges = new[] { new DiagramEdge("a", "b"), new DiagramEdge("b", "c") };

        var depths = DiagramGraphTopology.ComputeShortestDepths(nodes, edges);

        Assert.Equal(0, depths["a"]);
        Assert.Equal(1, depths["b"]);
        Assert.Equal(2, depths["c"]);
    }

    /// <summary>
    /// This method verifies that multiple roots are assigned depth zero.
    /// </summary>
    [Fact]
    public void ComputeShortestDepths_marks_multiple_roots_at_depth_zero()
    {
        var nodes = new[] { N("a"), N("b"), N("c") };
        var edges = new[] { new DiagramEdge("a", "c"), new DiagramEdge("b", "c") };

        var depths = DiagramGraphTopology.ComputeShortestDepths(nodes, edges);

        Assert.Equal(0, depths["a"]);
        Assert.Equal(0, depths["b"]);
        Assert.Equal(1, depths["c"]);
    }

    /// <summary>
    /// This method verifies that every node id receives a depth entry, including isolated nodes.
    /// </summary>
    [Fact]
    public void ComputeShortestDepths_maps_every_node_id()
    {
        var nodes = new[] { N("a"), N("b"), N("island") };
        var edges = new[] { new DiagramEdge("a", "b") };

        var depths = DiagramGraphTopology.ComputeShortestDepths(nodes, edges);

        Assert.Equal(3, depths.Count);
        Assert.Equal(0, depths["island"]);
    }
}
