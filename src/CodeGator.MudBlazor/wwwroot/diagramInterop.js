const attached = new WeakMap();

const NODE_HALF_W = 110;
const NODE_HALF_H = 28;

function mudPaletteVar(name, fallback) {
  const v = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
  return v || fallback;
}

function clientToSvg(svg, clientX, clientY) {
  const pt = svg.createSVGPoint();
  pt.x = clientX;
  pt.y = clientY;
  const ctm = svg.getScreenCTM();
  if (!ctm) return { x: clientX, y: clientY };
  return pt.matrixTransform(ctm.inverse());
}

function parseTranslate(g) {
  const tx = parseFloat(g.dataset.tx || '0');
  const ty = parseFloat(g.dataset.ty || '0');
  return { tx, ty };
}

function setNodePosition(g, tx, ty) {
  g.setAttribute('transform', `translate(${tx},${ty})`);
  g.dataset.tx = String(tx);
  g.dataset.ty = String(ty);
}

function updateEdges(svg) {
  svg.querySelectorAll('[data-cg-edge-group]').forEach((g) => {
    const fromId = g.dataset.from;
    const toId = g.dataset.to;
    const from = svg.querySelector(`[data-cg-node="${CSS.escape(fromId)}"]`);
    const to = svg.querySelector(`[data-cg-node="${CSS.escape(toId)}"]`);
    if (!from || !to) return;
    const x1 = parseFloat(from.dataset.tx);
    const y1 = parseFloat(from.dataset.ty);
    const x2 = parseFloat(to.dataset.tx);
    const y2 = parseFloat(to.dataset.ty);
    g.querySelectorAll('line').forEach((line) => {
      line.setAttribute('x1', String(x1));
      line.setAttribute('y1', String(y1));
      line.setAttribute('x2', String(x2));
      line.setAttribute('y2', String(y2));
    });
  });
}

function parseSelection(svg) {
  const raw = svg.dataset.cgSelection || '';
  if (!raw) return [];
  return raw.split('|').filter(Boolean);
}

function rectsIntersect(nx, ny, mx0, my0, mx1, my1) {
  const nl = nx - NODE_HALF_W;
  const nr = nx + NODE_HALF_W;
  const nt = ny - NODE_HALF_H;
  const nb = ny + NODE_HALF_H;
  const ml = Math.min(mx0, mx1);
  const mr = Math.max(mx0, mx1);
  const mt = Math.min(my0, my1);
  const mb = Math.max(my0, my1);
  return !(nr < ml || nl > mr || nb < mt || nt > mb);
}

function collectNodesInRect(svg, x0, y0, x1, y1) {
  const ids = [];
  svg.querySelectorAll('[data-cg-node]').forEach((g) => {
    const id = g.getAttribute('data-cg-node');
    const tx = parseFloat(g.dataset.tx);
    const ty = parseFloat(g.dataset.ty);
    if (rectsIntersect(tx, ty, x0, y0, x1, y1)) ids.push(id);
  });
  return ids;
}

function pointerTargetElement(e) {
  const t = e.target;
  if (!t) return null;
  if (t.nodeType === 1) return t;
  return t.parentElement;
}

export function attachDiagram(svgElement, dotNetRef) {
  const svg = svgElement.ownerSVGElement || svgElement;
  if (!svg || attached.get(svg)) return;

  attached.set(svg, true);

  let drag = null;
  let marquee = null;
  let edgeTap = null;

  const threshold = 6;

  function onPointerDown(e) {
    if (e.button !== 0) return;

    const el = pointerTargetElement(e);
    const node = el?.closest?.('[data-cg-node]');
    if (node && svg.contains(node)) {
      const id = node.getAttribute('data-cg-node');
      const sel = parseSelection(svg);
      const inSel = sel.includes(id);
      const moveIds = inSel && sel.length > 0 ? [...sel] : [id];

      const groups = [];
      const origins = new Map();
      for (const mid of moveIds) {
        const g = svg.querySelector(`[data-cg-node="${CSS.escape(mid)}"]`);
        if (!g) continue;
        groups.push(g);
        origins.set(mid, parseTranslate(g));
      }
      if (groups.length === 0) return;

      const p0 = clientToSvg(svg, e.clientX, e.clientY);
      const primary = svg.querySelector(`[data-cg-node="${CSS.escape(id)}"]`) || groups[0];

      drag = {
        groups,
        moveIds,
        origins,
        primaryEl: primary,
        pointerId: e.pointerId,
        startClient: { x: e.clientX, y: e.clientY },
        startSvg: p0,
        moved: false,
      };

      for (const g of groups) g.classList.add('cg-diagram-node--dragging');
      try {
        primary.setPointerCapture(e.pointerId);
      } catch { }
      e.preventDefault();
      e.stopPropagation();
      return;
    }

    const edgeG = el?.closest?.('[data-cg-edge-group]');
    if (edgeG && svg.contains(edgeG)) {
      edgeTap = {
        pointerId: e.pointerId,
        fromId: edgeG.dataset.from,
        toId: edgeG.dataset.to,
        startClient: { x: e.clientX, y: e.clientY },
        moved: false,
      };
      try {
        svg.setPointerCapture(e.pointerId);
      } catch { }
      e.preventDefault();
      e.stopPropagation();
      return;
    }

    const p0 = clientToSvg(svg, e.clientX, e.clientY);
    const rect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
    const primary = mudPaletteVar('--mud-palette-primary', '#594AE2');
    rect.setAttribute('fill', primary);
    rect.setAttribute('fill-opacity', '0.14');
    rect.setAttribute('stroke', primary);
    rect.setAttribute('stroke-width', '1');
    rect.setAttribute('stroke-dasharray', '4 3');
    rect.setAttribute('vector-effect', 'non-scaling-stroke');
    rect.setAttribute('x', String(p0.x));
    rect.setAttribute('y', String(p0.y));
    rect.setAttribute('width', '0');
    rect.setAttribute('height', '0');
    rect.setAttribute('pointer-events', 'none');
    svg.appendChild(rect);

    marquee = {
      pointerId: e.pointerId,
      startSvg: p0,
      el: rect,
      moved: false,
    };

    try {
      svg.setPointerCapture(e.pointerId);
    } catch { }
    e.preventDefault();
    e.stopPropagation();
  }

  function onPointerMove(e) {
    if (edgeTap && e.pointerId === edgeTap.pointerId) {
      const dx = e.clientX - edgeTap.startClient.x;
      const dy = e.clientY - edgeTap.startClient.y;
      if (dx * dx + dy * dy > threshold * threshold) edgeTap.moved = true;
      e.preventDefault();
      e.stopPropagation();
      return;
    }

    if (marquee && e.pointerId === marquee.pointerId) {
      const p = clientToSvg(svg, e.clientX, e.clientY);
      const x0 = marquee.startSvg.x;
      const y0 = marquee.startSvg.y;
      const x1 = p.x;
      const y1 = p.y;
      const dx = x1 - x0;
      const dy = y1 - y0;
      if (dx * dx + dy * dy > threshold * threshold) marquee.moved = true;

      const ml = Math.min(x0, x1);
      const mt = Math.min(y0, y1);
      const mw = Math.abs(x1 - x0);
      const mh = Math.abs(y1 - y0);
      marquee.el.setAttribute('x', String(ml));
      marquee.el.setAttribute('y', String(mt));
      marquee.el.setAttribute('width', String(mw));
      marquee.el.setAttribute('height', String(mh));
      e.preventDefault();
      e.stopPropagation();
      return;
    }

    if (!drag || e.pointerId !== drag.pointerId) return;

    const dx = e.clientX - drag.startClient.x;
    const dy = e.clientY - drag.startClient.y;
    if (dx * dx + dy * dy > threshold * threshold) drag.moved = true;

    const p = clientToSvg(svg, e.clientX, e.clientY);
    const ddx = p.x - drag.startSvg.x;
    const ddy = p.y - drag.startSvg.y;

    for (const mid of drag.moveIds) {
      const g = svg.querySelector(`[data-cg-node="${CSS.escape(mid)}"]`);
      const o = drag.origins.get(mid);
      if (!g || !o) continue;
      setNodePosition(g, o.tx + ddx, o.ty + ddy);
    }
    updateEdges(svg);
    e.preventDefault();
    e.stopPropagation();
  }

  function onPointerUp(e) {
    if (edgeTap && e.pointerId === edgeTap.pointerId) {
      try {
        svg.releasePointerCapture(e.pointerId);
      } catch { }
      const moved = edgeTap.moved;
      const fromId = edgeTap.fromId;
      const toId = edgeTap.toId;
      edgeTap = null;
      if (!moved) {
        dotNetRef.invokeMethodAsync('OnEdgeClickedFromJs', fromId, toId, e.clientX, e.clientY);
      }
      e.preventDefault();
      e.stopPropagation();
      return;
    }

    if (marquee && e.pointerId === marquee.pointerId) {
      try {
        svg.releasePointerCapture(e.pointerId);
      } catch { }
      const el = marquee.el;
      const moved = marquee.moved;
      const x0 = marquee.startSvg.x;
      const y0 = marquee.startSvg.y;
      marquee = null;
      el.remove();

      const p = clientToSvg(svg, e.clientX, e.clientY);
      if (!moved) {
        dotNetRef.invokeMethodAsync('SetSelectedNodeIds', []);
      } else {
        const ids = collectNodesInRect(svg, x0, y0, p.x, p.y);
        dotNetRef.invokeMethodAsync('SetSelectedNodeIds', ids);
      }
      e.preventDefault();
      e.stopPropagation();
      return;
    }

    if (!drag || e.pointerId !== drag.pointerId) return;

    const moved = drag.moved;
    const groups = drag.groups;
    const moveIds = drag.moveIds;

    try {
      drag.primaryEl.releasePointerCapture(e.pointerId);
    } catch { }
    for (const g of groups) g.classList.remove('cg-diagram-node--dragging');
    drag = null;

    if (moved) {
      const ids = [];
      const xs = [];
      const ys = [];
      for (const mid of moveIds) {
        const g = svg.querySelector(`[data-cg-node="${CSS.escape(mid)}"]`);
        if (!g) continue;
        ids.push(mid);
        xs.push(parseFloat(g.dataset.tx));
        ys.push(parseFloat(g.dataset.ty));
      }
      dotNetRef.invokeMethodAsync('OnNodesMoved', ids, xs, ys);
    } else {
      dotNetRef.invokeMethodAsync('OnNodeClicked', moveIds[0], e.clientX, e.clientY);
    }
    e.preventDefault();
    e.stopPropagation();
  }

  function onPointerCancel(e) {
    if (edgeTap && e.pointerId === edgeTap.pointerId) {
      try {
        svg.releasePointerCapture(e.pointerId);
      } catch { }
      edgeTap = null;
      return;
    }
    if (marquee && e.pointerId === marquee.pointerId) {
      try {
        svg.releasePointerCapture(e.pointerId);
      } catch { }
      marquee.el.remove();
      marquee = null;
      return;
    }
    if (drag && e.pointerId === drag.pointerId) {
      try {
        drag.primaryEl.releasePointerCapture(e.pointerId);
      } catch { }
      for (const g of drag.groups) g.classList.remove('cg-diagram-node--dragging');
      drag = null;
    }
  }

  function onWheel(e) {
    if (drag || marquee || edgeTap) {
      e.preventDefault();
      return;
    }
    if (e.buttons !== 0) {
      e.preventDefault();
      return;
    }
    e.preventDefault();
    e.stopPropagation();
    const p = clientToSvg(svg, e.clientX, e.clientY);
    dotNetRef.invokeMethodAsync('OnWheelZoom', e.deltaY, p.x, p.y);
  }

  function gateContains(e) {
    const el = pointerTargetElement(e);
    return el && svg.contains(el);
  }

  function onContextMenu(e) {
    if (!gateContains(e)) return;
    const el = pointerTargetElement(e);
    const node = el?.closest?.('[data-cg-node]');
    if (node && svg.contains(node)) {
      e.preventDefault();
      e.stopPropagation();
      const id = node.getAttribute('data-cg-node');
      dotNetRef.invokeMethodAsync('OnNodeContextMenuFromJs', id, e.clientX, e.clientY);
      return;
    }
    const edgeG = el?.closest?.('[data-cg-edge-group]');
    if (edgeG && svg.contains(edgeG)) {
      e.preventDefault();
      e.stopPropagation();
      dotNetRef.invokeMethodAsync(
        'OnEdgeContextMenuFromJs',
        edgeG.dataset.from,
        edgeG.dataset.to,
        e.clientX,
        e.clientY,
      );
    }
  }

  window.addEventListener(
    'pointerdown',
    (e) => {
      if (!gateContains(e)) return;
      onPointerDown(e);
    },
    { capture: true },
  );

  window.addEventListener('contextmenu', onContextMenu, { capture: true });

  window.addEventListener(
    'pointermove',
    (e) => {
      if (!drag && !marquee && !edgeTap) return;
      onPointerMove(e);
    },
    { capture: true },
  );

  window.addEventListener(
    'pointerup',
    (e) => {
      if (!drag && !marquee && !edgeTap) return;
      onPointerUp(e);
    },
    { capture: true },
  );

  window.addEventListener(
    'pointercancel',
    (e) => {
      if (!drag && !marquee && !edgeTap) return;
      onPointerCancel(e);
    },
    { capture: true },
  );

  svg.addEventListener('wheel', onWheel, { capture: true, passive: false });
}

export function getWrapInnerSize(element) {
  if (!element) return '0,0';
  const w = element.clientWidth || 0;
  const h = element.clientHeight || 0;
  return `${w},${h}`;
}

function escapeHtmlPrint(s) {
  return String(s)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

export function printDiagram(svgElement, documentTitle, documentSubtitle) {
  if (!svgElement) return false;
  const svg =
    svgElement.tagName && svgElement.tagName.toLowerCase() === 'svg'
      ? svgElement
      : svgElement.ownerSVGElement;
  if (!svg) return false;

  const clone = svg.cloneNode(true);
  const prevClass = clone.getAttribute('class') || '';
  clone.setAttribute('class', `${prevClass} cg-diagram-print-svg`.trim());

  const w = window.open('', '_blank');
  if (!w) return false;

  const title = documentTitle != null && documentTitle !== '' ? String(documentTitle) : 'Diagram';
  const sub = documentSubtitle != null ? String(documentSubtitle) : '';

  const doc = w.document;
  doc.open();
  doc.write('<!DOCTYPE html><html><head><meta charset="utf-8">');
  doc.write(`<title>${escapeHtmlPrint(title)}</title>`);

  document.querySelectorAll('link[rel="stylesheet"]').forEach((link) => {
    try {
      if (link.href) doc.write(`<link rel="stylesheet" href="${link.href}">`);
    } catch { }
  });

  doc.write(`<style>
@page { margin: 12mm; }
body { margin: 0; padding: 12px; box-sizing: border-box; -webkit-print-color-adjust: exact; print-color-adjust: exact; }
.cg-diagram-print-wrap { display: flex; flex-direction: column; align-items: center; }
.cg-diagram-print-svg { max-width: 100%; height: auto; -webkit-print-color-adjust: exact; print-color-adjust: exact; }
h1.cg-print-title { font-family: system-ui, Segoe UI, sans-serif; font-size: 14pt; margin: 0 0 6px; font-weight: 600; }
p.cg-print-sub { font-family: system-ui, Segoe UI, sans-serif; font-size: 10pt; margin: 0 0 12px; color: #555; }
.cg-diagram-print-svg foreignObject * {
  -webkit-print-color-adjust: exact !important;
  print-color-adjust: exact !important;
}
.cg-diagram-print-svg foreignObject [class*="mud-paper"] {
  box-shadow: none !important;
  border-style: solid !important;
  border-width: 1px !important;
  border-color: #616161 !important;
  background-color: #ffffff !important;
}
.cg-diagram-print-svg foreignObject .mud-theme-primary {
  border-color: #1565c0 !important;
  background-color: #e3f2fd !important;
}
.cg-diagram-print-svg foreignObject .mud-theme-secondary {
  border-color: #6a1b9a !important;
  background-color: #f3e5f5 !important;
}
.cg-diagram-print-svg foreignObject .mud-theme-tertiary {
  border-color: #00695c !important;
  background-color: #e0f2f1 !important;
}
@media print {
  .cg-diagram-print-svg foreignObject [class*="mud-paper"] {
    box-shadow: none !important;
    border: 1px solid #616161 !important;
    background-color: #ffffff !important;
  }
  .cg-diagram-print-svg foreignObject .mud-theme-primary {
    border-color: #1565c0 !important;
    background-color: #e3f2fd !important;
  }
  .cg-diagram-print-svg foreignObject .mud-theme-secondary {
    border-color: #6a1b9a !important;
    background-color: #f3e5f5 !important;
  }
  .cg-diagram-print-svg foreignObject .mud-theme-tertiary {
    border-color: #00695c !important;
    background-color: #e0f2f1 !important;
  }
}
</style>`);
  doc.write('</head><body><div class="cg-diagram-print-wrap">');
  doc.write(`<h1 class="cg-print-title">${escapeHtmlPrint(title)}</h1>`);
  if (sub) doc.write(`<p class="cg-print-sub">${escapeHtmlPrint(sub)}</p>`);
  doc.write(clone.outerHTML);
  doc.write('</div></body></html>');
  doc.close();

  const doPrint = () => {
    try {
      w.focus();
      w.print();
    } finally {
      w.addEventListener(
        'afterprint',
        () => {
          try {
            w.close();
          } catch { }
        },
        { once: true },
      );
    }
  };

  if (doc.readyState === 'complete') {
    setTimeout(doPrint, 200);
  } else {
    w.addEventListener('load', () => setTimeout(doPrint, 200));
  }

  return true;
}
