using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Utilities;

namespace CodeGator.MudBlazor;

/// <summary>
/// This class is an interactive SVG diagram component with zoom, pan, selection,
/// layouts, and optional printing.
/// </summary>
/// <remarks>
/// It composes <see cref="MudDiagramNode"/> for each vertex, uses JavaScript for
/// pointer gestures and print, and raises interaction and layout events for hosts.
/// </remarks>
public partial class MudDiagram
{
    /// <summary>
    /// This property supplies the JavaScript runtime used for module import and
    /// interop calls.
    /// </summary>
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// This field holds the sample nodes used when <see cref="Nodes"/> is null.
    /// </summary>
    static readonly IReadOnlyList<DiagramNode> DefaultNodes = new DiagramNode[]
    {
        new("n1", "Start", "Example diagram node.", DiagramNodeVisualStyle.Primary),
        new("n2", "Next", "Another generic node.", DiagramNodeVisualStyle.Secondary),
    };

    /// <summary>
    /// This field holds the sample edges used when <see cref="Edges"/> is null.
    /// </summary>
    static readonly IReadOnlyList<DiagramEdge> DefaultEdges = new DiagramEdge[]
    {
        new("n1", "n2"),
    };

    /// <summary>
    /// This field records the layout kind last applied to the bound graph.
    /// </summary>
    DiagramLayoutKind _boundLayoutKind;

    /// <summary>
    /// This field records the custom layout algorithm instance last bound, if any.
    /// </summary>
    IDiagramLayout? _boundLayoutAlgorithm;

    /// <summary>
    /// This field supplies a unique SVG marker id for arrowheads.
    /// </summary>
    readonly string _markerId = "cg-diagram-arrow-" + Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// This field supplies a unique SVG pattern id for the optional grid fill.
    /// </summary>
    readonly string _gridPatternId = "cg-diagram-grid-" + Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// This field references the root SVG element for interop and printing.
    /// </summary>
    ElementReference _svgRef;

    /// <summary>
    /// This field references the scrollable wrapper measured for viewport fitting.
    /// </summary>
    ElementReference _svgWrapRef;

    /// <summary>
    /// This field holds the imported JavaScript module for diagram interop.
    /// </summary>
    IJSObjectReference? _module;

    /// <summary>
    /// This method ensures the JavaScript interop module is loaded once.
    /// </summary>
    async Task EnsureInteropModuleAsync()
    {
        _module ??= await JS.InvokeAsync<IJSObjectReference>("import", DiagramInterop.ModuleImportPath);
    }

    /// <summary>
    /// This field holds the .NET reference passed to JavaScript for callbacks.
    /// </summary>
    DotNetObjectReference<MudDiagram>? _dotNetRef;

    /// <summary>
    /// This field caches the resolved node list used for rendering and hit testing.
    /// </summary>
    IReadOnlyList<DiagramNode> _resolvedNodes = Array.Empty<DiagramNode>();

    /// <summary>
    /// This field caches the resolved edge list used for rendering and hit testing.
    /// </summary>
    IReadOnlyList<DiagramEdge> _resolvedEdges = Array.Empty<DiagramEdge>();

    /// <summary>
    /// This field maps node ids to content-space positions after layout or drag.
    /// </summary>
    Dictionary<string, (double X, double Y)> _positions = new(StringComparer.Ordinal);

    /// <summary>
    /// This field tracks the bound <see cref="Nodes"/> reference for change
    /// detection.
    /// </summary>
    object? _boundNodesToken;

    /// <summary>
    /// This field tracks the bound <see cref="Edges"/> reference for change
    /// detection.
    /// </summary>
    object? _boundEdgesToken;

    /// <summary>
    /// This property supplies the heading text shown in the diagram chrome.
    /// </summary>
    [Parameter]
    public string Title { get; set; } = "MudDiagram";

    /// <summary>
    /// This property supplies optional secondary text below the title.
    /// </summary>
    [Parameter]
    public string? Subtitle { get; set; }

    /// <summary>
    /// This property supplies optional badge text beside the title.
    /// </summary>
    [Parameter]
    public string? ContextBadge { get; set; } = "CodeGator.MudBlazor";

    /// <summary>
    /// This property supplies optional legend content rendered below the header.
    /// </summary>
    [Parameter]
    public RenderFragment? Legend { get; set; }

    /// <summary>
    /// This property selects the built-in layout kind when no custom algorithm is
    /// set.
    /// </summary>
    [Parameter]
    public DiagramLayoutKind Layout { get; set; } = DiagramLayoutKind.HierarchicalTopDown;

    /// <summary>
    /// This property supplies an optional layout implementation that overrides
    /// <see cref="Layout"/>.
    /// </summary>
    [Parameter]
    public IDiagramLayout? LayoutAlgorithm { get; set; }

    /// <summary>
    /// This property supplies an optional template for custom node presentation.
    /// </summary>
    [Parameter]
    public RenderFragment<DiagramNodeRenderContext>? NodeTemplate { get; set; }

    /// <summary>
    /// This property supplies the CSS max-height value for the scrollable viewport.
    /// </summary>
    [Parameter]
    public string ViewportMaxHeightCss { get; set; } = "280px";

    /// <summary>
    /// This property supplies the graph nodes to render, or null for defaults.
    /// </summary>
    [Parameter]
    public IReadOnlyList<DiagramNode>? Nodes { get; set; }

    /// <summary>
    /// This property supplies directed edges between nodes, or null for defaults.
    /// </summary>
    [Parameter]
    public IReadOnlyList<DiagramEdge>? Edges { get; set; }

    /// <summary>
    /// This event callback is raised after a primary click on a node without drag.
    /// </summary>
    [Parameter]
    public EventCallback<DiagramNodeInteractionEventArgs> OnNodeClick { get; set; }

    /// <summary>
    /// This event callback is raised when the context menu opens on a node.
    /// </summary>
    [Parameter]
    public EventCallback<DiagramNodeInteractionEventArgs> OnNodeContextMenu { get; set; }

    /// <summary>
    /// This event callback is raised after a primary click on an edge without drag.
    /// </summary>
    [Parameter]
    public EventCallback<DiagramEdgeInteractionEventArgs> OnEdgeClick { get; set; }

    /// <summary>
    /// This event callback is raised when the context menu opens on an edge.
    /// </summary>
    [Parameter]
    public EventCallback<DiagramEdgeInteractionEventArgs> OnEdgeContextMenu { get; set; }

    /// <summary>
    /// This event callback is raised after layout completes and the diagram renders.
    /// </summary>
    [Parameter]
    public EventCallback<DiagramLayoutAppliedEventArgs> OnLayoutApplied { get; set; }

    /// <summary>
    /// This property indicates whether to draw the background grid pattern.
    /// </summary>
    [Parameter]
    public bool ShowGrid { get; set; }

    /// <summary>
    /// This property defines the grid cell size in content units before clamping.
    /// </summary>
    [Parameter]
    public double GridSpacing { get; set; } = 40;

    /// <summary>
    /// This property defines the grid layer opacity between zero and one.
    /// </summary>
    [Parameter]
    public double GridOpacity { get; set; } = 0.35;

    /// <summary>
    /// This property supplies the CSS stroke color used for grid lines.
    /// </summary>
    [Parameter]
    public string GridStroke { get; set; } = "var(--mud-palette-divider)";

    /// <summary>
    /// This property defines the grid line width in pattern space.
    /// </summary>
    [Parameter]
    public double GridStrokeWidth { get; set; } = 1;

    /// <summary>
    /// This property supplies the effective grid spacing after clamping to a
    /// minimum.
    /// </summary>
    double EffectiveGridSpacing => Math.Max(4, GridSpacing);

    /// <summary>
    /// This property sets the outer <c>MudPaper</c> elevation level.
    /// </summary>
    [Parameter]
    public int Elevation { get; set; } = 2;

    /// <summary>
    /// This property indicates whether the outer <c>MudPaper</c> uses an outline.
    /// </summary>
    [Parameter]
    public bool Outlined { get; set; } = true;

    /// <summary>
    /// This property indicates whether the outer <c>MudPaper</c> uses square
    /// corners.
    /// </summary>
    [Parameter]
    public bool Square { get; set; }

    /// <summary>
    /// This property builds the root CSS class list including the host class.
    /// </summary>
    string RootClass => new CssBuilder("mud-width-full").AddClass(Class).Build();

    /// <summary>
    /// This field defines the scale factor from public zoom to internal view scale.
    /// </summary>
    public const double ZoomToEffectiveScale = 0.25;

    /// <summary>
    /// This field defines the default public zoom level representing one hundred
    /// percent.
    /// </summary>
    public const double DefaultZoomLevel = 1.0;

    /// <summary>
    /// This field defines the default minimum bound for public zoom.
    /// </summary>
    public const double MinZoomDefault = 1.0;

    /// <summary>
    /// This field defines the default maximum bound for public zoom.
    /// </summary>
    public const double MaxZoomDefault = 4.5;

    /// <summary>
    /// This property supplies the public zoom factor bound to the host.
    /// </summary>
    [Parameter]
    public double Zoom { get; set; } = DefaultZoomLevel;

    /// <summary>
    /// This event callback notifies the host when zoom changes from the diagram.
    /// </summary>
    [Parameter]
    public EventCallback<double> ZoomChanged { get; set; }

    /// <summary>
    /// This property defines the minimum allowed public zoom.
    /// </summary>
    [Parameter]
    public double MinZoom { get; set; } = MinZoomDefault;

    /// <summary>
    /// This property defines the maximum allowed public zoom.
    /// </summary>
    [Parameter]
    public double MaxZoom { get; set; } = MaxZoomDefault;

    /// <summary>
    /// This property indicates whether to fit the graph after graph changes.
    /// </summary>
    [Parameter]
    public bool AutoFitToViewport { get; set; }

    /// <summary>
    /// This property indicates whether to grow content to fill the scroll viewport.
    /// </summary>
    [Parameter]
    public bool ExpandDiagramToFillViewport { get; set; } = true;

    /// <summary>
    /// This field tracks currently selected node ids for multi-select behavior.
    /// </summary>
    readonly HashSet<string> _selectedIds = new(StringComparer.Ordinal);

    /// <summary>
    /// This field caches the measured scroll wrapper client width in CSS pixels.
    /// </summary>
    double _wrapClientW;

    /// <summary>
    /// This field caches the measured scroll wrapper client height in CSS pixels.
    /// </summary>
    double _wrapClientH;

    /// <summary>
    /// This field schedules a fit-to-viewport pass after the next render.
    /// </summary>
    bool _scheduleFitToViewport;

    /// <summary>
    /// This field schedules raising <see cref="OnLayoutApplied"/> after render.
    /// </summary>
    bool _scheduleLayoutAppliedEvent;

    /// <summary>
    /// This field records the prior <see cref="AutoFitToViewport"/> value to detect
    /// transitions.
    /// </summary>
    bool _prevAutoFitToViewport;

    /// <summary>
    /// This field stores the hovered edge key for highlight styling, if any.
    /// </summary>
    string? _hoveredEdgeKey;

    /// <summary>
    /// This field stores the view box minimum X in content coordinates.
    /// </summary>
    double _vbMinX;

    /// <summary>
    /// This field stores the view box minimum Y in content coordinates.
    /// </summary>
    double _vbMinY;

    /// <summary>
    /// This property defines the content width spanned by the diagram bounds.
    /// </summary>
    double ViewBoxWidth { get; set; } = 920;

    /// <summary>
    /// This property defines the content height spanned by the diagram bounds.
    /// </summary>
    double ViewBoxHeight { get; set; } = 420;

    /// <summary>
    /// This field stores the display origin minimum X for the current zoom window.
    /// </summary>
    double _displayMinX;

    /// <summary>
    /// This field stores the display origin minimum Y for the current zoom window.
    /// </summary>
    double _displayMinY;

    /// <summary>
    /// This field stores the internal effective zoom applied to the view box.
    /// </summary>
    double _effectiveZoom = DefaultZoomLevel * ZoomToEffectiveScale;

    /// <summary>
    /// This property supplies the visible width in content units at the current
    /// zoom.
    /// </summary>
    double DisplayWidth => ViewBoxWidth / _effectiveZoom;

    /// <summary>
    /// This property supplies the visible height in content units at the current
    /// zoom.
    /// </summary>
    double DisplayHeight => ViewBoxHeight / _effectiveZoom;

    /// <summary>
    /// This property supplies the SVG pixel width from measurement or a fallback.
    /// </summary>
    double SvgPixelWidth => _wrapClientW >= 4 ? _wrapClientW : 920;

    /// <summary>
    /// This property supplies the SVG pixel height from measurement or a fallback.
    /// </summary>
    double SvgPixelHeight => _wrapClientH >= 4 ? _wrapClientH : 420;

    /// <summary>
    /// This property serializes the current selection for the SVG data attribute.
    /// </summary>
    string SelectionDataAttribute => string.Join("|", _selectedIds.OrderBy(x => x));

    /// <summary>
    /// This method returns the sole selected node when exactly one id is selected.
    /// </summary>
    /// <returns>This return value is the selected node, or null when not singular.</returns>
    DiagramNode? SingleSelectedNode()
    {
        if (_selectedIds.Count != 1)
        {
            return null;
        }

        var id = _selectedIds.First();
        return _resolvedNodes.FirstOrDefault(n => n.Id == id);
    }

    /// <summary>
    /// This method resolves the active layout implementation for the current
    /// parameters.
    /// </summary>
    /// <returns>This return value is the layout used to compute positions.</returns>
    IDiagramLayout ResolveLayout() =>
        LayoutAlgorithm ?? DiagramBuiltinLayouts.For(Layout);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _resolvedNodes = Nodes ?? DefaultNodes;
        _resolvedEdges = Edges ?? DefaultEdges;

        var nodesToken = (object?)Nodes ?? DefaultNodes;
        var edgesToken = (object?)Edges ?? DefaultEdges;
        var graphChanged = !Equals(_boundNodesToken, nodesToken) || !Equals(_boundEdgesToken, edgesToken);
        var layoutChanged = _boundLayoutKind != Layout
            || !ReferenceEquals(_boundLayoutAlgorithm, LayoutAlgorithm);
        if (graphChanged)
        {
            _boundNodesToken = nodesToken;
            _boundEdgesToken = edgesToken;
        }

        if (graphChanged || layoutChanged)
        {
            _boundLayoutKind = Layout;
            _boundLayoutAlgorithm = LayoutAlgorithm;
            _positions = new Dictionary<string, (double X, double Y)>(ResolveLayout().Compute(_resolvedNodes, _resolvedEdges), StringComparer.Ordinal);
            _effectiveZoom = EffectiveFromPublicZoom(ClampZoom(Zoom));
            RecomputeViewBox();
            CenterDisplayOrigin();
            _selectedIds.RemoveWhere(id => _resolvedNodes.All(n => n.Id != id));
            if (AutoFitToViewport)
            {
                _scheduleFitToViewport = true;
            }

            _scheduleLayoutAppliedEvent = true;
        }
        else
        {
            var pub = ClampZoom(Zoom);
            var pubInternal = PublicZoomFromEffective(_effectiveZoom);
            if (Math.Abs(pub - pubInternal) > 1e-5)
            {
                var zNewEff = EffectiveFromPublicZoom(pub);
                if (Math.Abs(zNewEff - _effectiveZoom) > 1e-9)
                {
                    var dwOld = ViewBoxWidth / _effectiveZoom;
                    var dhOld = ViewBoxHeight / _effectiveZoom;
                    var cx = _displayMinX + dwOld * 0.5;
                    var cy = _displayMinY + dhOld * 0.5;
                    ApplyZoomPreservingFocal(zNewEff, cx, cy);
                }
            }
        }

        if (AutoFitToViewport && !_prevAutoFitToViewport)
        {
            _scheduleFitToViewport = true;
        }

        _prevAutoFitToViewport = AutoFitToViewport;
    }

    /// <summary>
    /// This method recomputes the content view box from node positions and padding.
    /// </summary>
    void RecomputeViewBox()
    {
        if (_positions.Count == 0)
        {
            _vbMinX = 0;
            _vbMinY = 0;
            ViewBoxWidth = 920;
            ViewBoxHeight = 420;
            ApplyViewportMinimumToViewBox();
            CenterDisplayOrigin();
            return;
        }

        const double nodeHalfW = 110;
        const double nodeHalfH = 28;
        const double pad = 56;
        var xs = _positions.Values.Select(p => p.X);
        var ys = _positions.Values.Select(p => p.Y);
        var minX = xs.Min() - nodeHalfW - pad;
        var maxX = xs.Max() + nodeHalfW + pad;
        var minY = ys.Min() - nodeHalfH - pad;
        var maxY = ys.Max() + nodeHalfH + pad;
        var spanX = maxX - minX;
        var spanY = maxY - minY;
        var viewW = Math.Max(400, spanX);
        var viewH = Math.Max(320, spanY);
        var midX = (minX + maxX) * 0.5;
        var midY = (minY + maxY) * 0.5;
        _vbMinX = midX - viewW * 0.5;
        _vbMinY = midY - viewH * 0.5;
        ViewBoxWidth = viewW;
        ViewBoxHeight = viewH;
        ApplyViewportMinimumToViewBox();
    }

    /// <summary>
    /// This method grows the view box so diagram content fills the measured
    /// viewport when enabled.
    /// </summary>
    /// <returns>This return value is <c>true</c> when the view box size changed.</returns>
    bool ApplyViewportMinimumToViewBox()
    {
        if (!ExpandDiagramToFillViewport)
        {
            return false;
        }

        if (_wrapClientW < 4 || _wrapClientH < 4)
        {
            return false;
        }

        var minW = _wrapClientW * _effectiveZoom;
        var minH = _wrapClientH * _effectiveZoom;
        if (ViewBoxWidth >= minW - 0.01 && ViewBoxHeight >= minH - 0.01)
        {
            return false;
        }

        var w0 = ViewBoxWidth;
        var h0 = ViewBoxHeight;
        var dw0 = w0 / _effectiveZoom;
        var dh0 = h0 / _effectiveZoom;
        var focalX = _displayMinX + dw0 * 0.5;
        var focalY = _displayMinY + dh0 * 0.5;

        var midX = _vbMinX + ViewBoxWidth * 0.5;
        var midY = _vbMinY + ViewBoxHeight * 0.5;
        ViewBoxWidth = Math.Max(ViewBoxWidth, minW);
        ViewBoxHeight = Math.Max(ViewBoxHeight, minH);
        _vbMinX = midX - ViewBoxWidth * 0.5;
        _vbMinY = midY - ViewBoxHeight * 0.5;

        var dw1 = ViewBoxWidth / _effectiveZoom;
        var dh1 = ViewBoxHeight / _effectiveZoom;
        _displayMinX = focalX - dw1 * 0.5;
        _displayMinY = focalY - dh1 * 0.5;
        return true;
    }

    /// <summary>
    /// This method clamps the visible display rectangle to valid pan limits.
    /// </summary>
    void ClampDisplayToViewBoxBounds()
    {
        var w = ViewBoxWidth;
        var h = ViewBoxHeight;
        if (w <= 0 || h <= 0)
        {
            return;
        }

        var dw = w / _effectiveZoom;
        var dh = h / _effectiveZoom;
        var minMx = _vbMinX + Math.Min(0, w - dw);
        var maxMx = _vbMinX + Math.Max(0, w - dw);
        var minMy = _vbMinY + Math.Min(0, h - dh);
        var maxMy = _vbMinY + Math.Max(0, h - dh);
        _displayMinX = Math.Clamp(_displayMinX, minMx, maxMx);
        _displayMinY = Math.Clamp(_displayMinY, minMy, maxMy);
    }

    /// <summary>
    /// This method reads the scroll wrapper size from JavaScript and updates
    /// cached measurements.
    /// </summary>
    /// <returns>This return value is <c>true</c> when the wrapper size changed.</returns>
    async Task<bool> RefreshWrapClientSizeAsync()
    {
        try
        {
            await EnsureInteropModuleAsync();
            var raw = await _module!.InvokeAsync<string>("getWrapInnerSize", _svgWrapRef);
            if (string.IsNullOrEmpty(raw))
            {
                return false;
            }

            var parts = raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2
                || !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var gw)
                || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var gh))
            {
                return false;
            }

            if (gw < 4 || gh < 4)
            {
                return false;
            }

            var changed = Math.Abs(gw - _wrapClientW) > 0.5 || Math.Abs(gh - _wrapClientH) > 0.5;
            _wrapClientW = gw;
            _wrapClientH = gh;
            return changed;
        }
        catch (JSDisconnectedException)
        {
            return false;
        }
        catch (JSException)
        {
            return false;
        }
    }

    /// <summary>
    /// This method centers the visible window within the padded view box bounds.
    /// </summary>
    void CenterDisplayOrigin()
    {
        var w = ViewBoxWidth;
        var h = ViewBoxHeight;
        var dw = w / _effectiveZoom;
        var dh = h / _effectiveZoom;
        _displayMinX = _vbMinX + (w - dw) * 0.5;
        _displayMinY = _vbMinY + (h - dh) * 0.5;
    }

    /// <summary>
    /// This method changes zoom while keeping a focal point stationary in content
    /// space.
    /// </summary>
    /// <param name="zNew">This parameter supplies the new effective zoom scale.</param>
    /// <param name="focalX">This parameter supplies the focal X in content space.</param>
    /// <param name="focalY">This parameter supplies the focal Y in content space.</param>
    void ApplyZoomPreservingFocal(double zNew, double focalX, double focalY)
    {
        var zOld = _effectiveZoom;
        if (Math.Abs(zOld - zNew) < 0.0001)
        {
            return;
        }

        var w = ViewBoxWidth;
        var h = ViewBoxHeight;
        if (w <= 0 || h <= 0)
        {
            return;
        }

        var dwOld = w / zOld;
        var dhOld = h / zOld;
        var dwNew = w / zNew;
        var dhNew = h / zNew;

        var nmx = focalX - (focalX - _displayMinX) * (dwNew / dwOld);
        var nmy = focalY - (focalY - _displayMinY) * (dhNew / dhOld);

        var minMx = _vbMinX + Math.Min(0, w - dwNew);
        var maxMx = _vbMinX + Math.Max(0, w - dwNew);
        var minMy = _vbMinY + Math.Min(0, h - dhNew);
        var maxMy = _vbMinY + Math.Max(0, h - dhNew);
        _displayMinX = Math.Clamp(nmx, minMx, maxMx);
        _displayMinY = Math.Clamp(nmy, minMy, maxMy);
        _effectiveZoom = zNew;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            await EnsureInteropModuleAsync();
            _dotNetRef ??= DotNetObjectReference.Create(this);
            await _module!.InvokeVoidAsync("attachDiagram", _svgRef, _dotNetRef);

            var wrapChanged = await RefreshWrapClientSizeAsync();
            var expanded = false;
            if (firstRender || wrapChanged)
            {
                expanded = ApplyViewportMinimumToViewBox();
                if (expanded)
                {
                    ClampDisplayToViewBoxBounds();
                }
            }

            if (_scheduleLayoutAppliedEvent)
            {
                _scheduleLayoutAppliedEvent = false;
                if (OnLayoutApplied.HasDelegate)
                {
                    await OnLayoutApplied.InvokeAsync(BuildLayoutAppliedEventArgs());
                }
            }

            if (_scheduleFitToViewport)
            {
                await FitToViewportAsync();
            }
            else if (expanded || wrapChanged)
            {
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (JSDisconnectedException) { }
        catch (JSException) { }
    }

    /// <summary>
    /// This method applies node positions reported from JavaScript after dragging.
    /// </summary>
    [JSInvokable]
    public Task OnNodesMoved(string[] ids, double[] xs, double[] ys)
    {
        if (ids.Length != xs.Length || xs.Length != ys.Length)
        {
            return Task.CompletedTask;
        }

        for (var i = 0; i < ids.Length; i++)
        {
            if (_positions.ContainsKey(ids[i]))
            {
                _positions[ids[i]] = (xs[i], ys[i]);
            }
        }
        return InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// This method handles a primary node click from JavaScript without drag.
    /// </summary>
    [JSInvokable]
    public async Task OnNodeClicked(string id, double clientX, double clientY)
    {
        _selectedIds.Clear();
        DiagramNode? node = null;
        if (_resolvedNodes.FirstOrDefault(n => n.Id == id) is { } n)
        {
            _selectedIds.Add(n.Id);
            node = n;
        }

        await InvokeAsync(StateHasChanged);

        if (node is not null && OnNodeClick.HasDelegate)
        {
            await OnNodeClick.InvokeAsync(new DiagramNodeInteractionEventArgs { Node = node, ClientX = clientX, ClientY = clientY });
        }
    }

    /// <summary>
    /// This method handles a node context menu request from JavaScript.
    /// </summary>
    [JSInvokable]
    public async Task OnNodeContextMenuFromJs(string id, double clientX, double clientY)
    {
        var node = _resolvedNodes.FirstOrDefault(n => n.Id == id);
        if (node is null)
        {
            return;
        }

        if (OnNodeContextMenu.HasDelegate)
        {
            await OnNodeContextMenu.InvokeAsync(new DiagramNodeInteractionEventArgs { Node = node, ClientX = clientX, ClientY = clientY });
        }
    }

    /// <summary>
    /// This method handles a primary edge click from JavaScript without drag.
    /// </summary>
    [JSInvokable]
    public async Task OnEdgeClickedFromJs(string fromId, string toId, double clientX, double clientY)
    {
        var edge = _resolvedEdges.FirstOrDefault(e => e.FromId == fromId && e.ToId == toId);
        if (edge is null)
        {
            return;
        }

        if (OnEdgeClick.HasDelegate)
        {
            await OnEdgeClick.InvokeAsync(new DiagramEdgeInteractionEventArgs { Edge = edge, ClientX = clientX, ClientY = clientY });
        }
    }

    /// <summary>
    /// This method handles an edge context menu request from JavaScript.
    /// </summary>
    [JSInvokable]
    public async Task OnEdgeContextMenuFromJs(string fromId, string toId, double clientX, double clientY)
    {
        var edge = _resolvedEdges.FirstOrDefault(e => e.FromId == fromId && e.ToId == toId);
        if (edge is null)
        {
            return;
        }

        if (OnEdgeContextMenu.HasDelegate)
        {
            await OnEdgeContextMenu.InvokeAsync(new DiagramEdgeInteractionEventArgs { Edge = edge, ClientX = clientX, ClientY = clientY });
        }
    }

    /// <summary>
    /// This method replaces the selection from JavaScript after marquee selection.
    /// </summary>
    [JSInvokable]
    public Task SetSelectedNodeIds(string[] ids)
    {
        _selectedIds.Clear();
        foreach (var id in ids)
        {
            if (_resolvedNodes.Any(n => n.Id == id))
            {
                _selectedIds.Add(id);
            }
        }
        return InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// This method converts a public zoom value to internal effective scale.
    /// </summary>
    static double EffectiveFromPublicZoom(double publicZoom) => publicZoom * ZoomToEffectiveScale;

    /// <summary>
    /// This method converts internal effective scale to a public zoom value.
    /// </summary>
    static double PublicZoomFromEffective(double effectiveZoom) => effectiveZoom / ZoomToEffectiveScale;

    /// <summary>
    /// This method clamps a public zoom value to configured bounds.
    /// </summary>
    double ClampZoom(double publicZoom)
    {
        if (double.IsNaN(publicZoom) || publicZoom <= 0)
        {
            return DefaultZoomLevel;
        }

        return Math.Clamp(publicZoom, MinZoom, MaxZoom);
    }

    /// <summary>
    /// This method applies a new public zoom, optionally preserving a focal point.
    /// </summary>
    async Task SetZoomAsync(double publicZoom, double? focalX = null, double? focalY = null)
    {
        var z = ClampZoom(publicZoom);
        var eff = EffectiveFromPublicZoom(z);
        if (Math.Abs(eff - _effectiveZoom) < 0.0001)
        {
            return;
        }

        var dwOld = ViewBoxWidth / _effectiveZoom;
        var dhOld = ViewBoxHeight / _effectiveZoom;
        var fx = focalX ?? (_displayMinX + dwOld * 0.5);
        var fy = focalY ?? (_displayMinY + dhOld * 0.5);
        ApplyZoomPreservingFocal(eff, fx, fy);

        if (ZoomChanged.HasDelegate)
        {
            await ZoomChanged.InvokeAsync(z);
        }
        else
        {
            if (ApplyViewportMinimumToViewBox())
            {
                ClampDisplayToViewBoxBounds();
            }

            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// This method increases zoom by a fixed factor relative to the current level.
    /// </summary>
    public Task ZoomInAsync() => SetZoomAsync(PublicZoomFromEffective(_effectiveZoom) * 1.15);

    /// <summary>
    /// This method decreases zoom by a fixed factor relative to the current level.
    /// </summary>
    public Task ZoomOutAsync() => SetZoomAsync(PublicZoomFromEffective(_effectiveZoom) / 1.15);

    /// <summary>
    /// This method resets zoom to <see cref="DefaultZoomLevel"/>.
    /// </summary>
    public Task ResetZoomAsync() => SetZoomAsync(DefaultZoomLevel);

    /// <summary>
    /// This method opens a print preview for the diagram SVG in a new window.
    /// </summary>
    /// <param name="documentTitle">This parameter overrides the print title.</param>
    /// <param name="documentSubtitle">This parameter overrides the print subtitle.</param>
    /// <returns>This return value is <c>false</c> when printing could not start.</returns>
    public async Task<bool> PrintAsync(string? documentTitle = null, string? documentSubtitle = null)
    {
        try
        {
            await EnsureInteropModuleAsync();
            return await _module!.InvokeAsync<bool>(
                "printDiagram",
                _svgRef,
                documentTitle ?? Title,
                documentSubtitle ?? Subtitle ?? string.Empty);
        }
        catch (JSDisconnectedException)
        {
            return false;
        }
        catch (JSException)
        {
            return false;
        }
    }

    /// <summary>
    /// This method recomputes positions from the active layout and schedules render.
    /// </summary>
    /// <param name="refitViewport">This parameter controls post-layout fit behavior.</param>
    public Task ApplyLayoutAsync(bool? refitViewport = null)
    {
        _positions = new Dictionary<string, (double X, double Y)>(ResolveLayout().Compute(_resolvedNodes, _resolvedEdges), StringComparer.Ordinal);
        _boundLayoutKind = Layout;
        _boundLayoutAlgorithm = LayoutAlgorithm;
        _effectiveZoom = EffectiveFromPublicZoom(ClampZoom(Zoom));
        RecomputeViewBox();
        CenterDisplayOrigin();

        var fit = refitViewport ?? AutoFitToViewport;
        if (fit)
        {
            _scheduleFitToViewport = true;
        }

        _scheduleLayoutAppliedEvent = true;

        return InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// This method builds arguments for <see cref="OnLayoutApplied"/>.
    /// </summary>
    DiagramLayoutAppliedEventArgs BuildLayoutAppliedEventArgs() =>
        new()
        {
            LayoutKind = Layout,
            UsesCustomLayoutAlgorithm = LayoutAlgorithm is not null,
            Nodes = _resolvedNodes,
            Edges = _resolvedEdges,
            Positions = new Dictionary<string, (double X, double Y)>(_positions, StringComparer.Ordinal),
        };

    /// <summary>
    /// This method fits the entire diagram inside the visible scroll viewport.
    /// </summary>
    public async Task FitToViewportAsync()
    {
        _scheduleFitToViewport = false;

        try
        {
            await EnsureInteropModuleAsync();
            var module = _module!;

            double pw = 0, ph = 0;
            for (var attempt = 0; attempt < 4; attempt++)
            {
                var raw = await module.InvokeAsync<string>("getWrapInnerSize", _svgWrapRef);
                if (!string.IsNullOrEmpty(raw))
                {
                    var parts = raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2
                        && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var gw)
                        && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var gh))
                    {
                        pw = gw;
                        ph = gh;
                    }
                }
                if (pw >= 4 && ph >= 4)
                {
                    break;
                }

                await Task.Delay(40);
            }

            if (pw < 4 || ph < 4)
            {
                return;
            }

            _wrapClientW = pw;
            _wrapClientH = ph;

            var w = ViewBoxWidth;
            var h = ViewBoxHeight;
            if (w <= 0 || h <= 0)
            {
                return;
            }

            var dw = Math.Min(pw, ph * w / h);
            var dh = Math.Min(ph, pw * h / w);
            if (dw <= 0 || dh <= 0)
            {
                return;
            }

            var effTarget = w / dw;
            var publicTarget = PublicZoomFromEffective(effTarget);
            publicTarget = ClampZoom(publicTarget);
            var eff = EffectiveFromPublicZoom(publicTarget);

            if (Math.Abs(eff - _effectiveZoom) < 1e-9)
            {
                CenterDisplayOrigin();
            }
            else
            {
                _effectiveZoom = eff;
                CenterDisplayOrigin();
            }

            if (ApplyViewportMinimumToViewBox())
            {
                ClampDisplayToViewBoxBounds();
            }

            if (ZoomChanged.HasDelegate)
            {
                await ZoomChanged.InvokeAsync(publicTarget);
            }

            await InvokeAsync(StateHasChanged);
        }
        catch (JSDisconnectedException) { }
        catch (JSException) { }
    }

    /// <summary>
    /// This method applies wheel zoom from JavaScript at a focal point in content
    /// space.
    /// </summary>
    [JSInvokable]
    public Task OnWheelZoom(double deltaY, double focalX, double focalY)
    {
        var delta = deltaY switch
        {
            < 0 => 1.08,
            > 0 => 1 / 1.08,
            _ => 1.0
        };
        if (delta == 1.0)
        {
            return Task.CompletedTask;
        }

        var nextEff = _effectiveZoom * delta;
        var nextPublic = ClampZoom(PublicZoomFromEffective(nextEff));
        return SetZoomAsync(nextPublic, focalX, focalY);
    }

    /// <summary>
    /// This method records the hovered edge for hover styling in markup.
    /// </summary>
    void SetHoveredEdge(DiagramEdge edge) => _hoveredEdgeKey = EdgeKey(edge);

    /// <summary>
    /// This method clears the hovered edge highlight.
    /// </summary>
    void ClearHoveredEdge() => _hoveredEdgeKey = null;

    /// <summary>
    /// This method builds a stable string key for an edge pair.
    /// </summary>
    static string EdgeKey(DiagramEdge e) => e.FromId + "->" + e.ToId;

    /// <summary>
    /// This method builds the native SVG tooltip text for an edge.
    /// </summary>
    string EdgeTooltip(DiagramEdge e)
    {
        var from = LabelForId(e.FromId);
        var to = LabelForId(e.ToId);
        var kind = string.IsNullOrEmpty(e.Label) ? "Link" : e.Label;
        return $"{from} → {to} ({kind})";
    }

    /// <summary>
    /// This method formats a number for general SVG attribute output.
    /// </summary>
    static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>
    /// This method formats a number with higher precision for SVG viewBox output.
    /// </summary>
    static string Fsvg(double v) => v.ToString("0.##########", CultureInfo.InvariantCulture);

    /// <summary>
    /// This method returns the stroke color for an edge based on hover state.
    /// </summary>
    string EdgeStroke(DiagramEdge e) =>
        string.Equals(_hoveredEdgeKey, EdgeKey(e), StringComparison.Ordinal)
            ? "var(--mud-palette-primary)"
            : "var(--mud-palette-text-disabled)";

    /// <summary>
    /// This method enumerates outgoing edges for a node id.
    /// </summary>
    IEnumerable<DiagramEdge> OutgoingEdges(string id) =>
        _resolvedEdges.Where(e => e.FromId == id);

    /// <summary>
    /// This method enumerates incoming edges for a node id.
    /// </summary>
    IEnumerable<DiagramEdge> IncomingEdges(string id) =>
        _resolvedEdges.Where(e => e.ToId == id);

    /// <summary>
    /// This method resolves a display label for a node id, falling back to the id.
    /// </summary>
    string LabelForId(string id) =>
        _resolvedNodes.FirstOrDefault(n => n.Id == id)?.Label ?? id;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _dotNetRef?.Dispose();
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
        }
    }
}
