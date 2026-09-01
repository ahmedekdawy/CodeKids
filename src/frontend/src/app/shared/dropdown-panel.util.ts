export function attachDropdownPanelToBody(panel: HTMLElement | null | undefined): void {
  if (panel && panel.parentElement !== document.body) {
    document.body.appendChild(panel);
  }
}

export function isInsideDropdown(
  target: EventTarget | null,
  host: HTMLElement,
  panel: HTMLElement | null | undefined
): boolean {
  if (!(target instanceof Node)) return false;
  return host.contains(target) || !!panel?.contains(target);
}

export function applyDropdownPanelStyle(
  panel: HTMLElement,
  trigger: HTMLElement,
  compact = false
): void {
  const rect = trigger.getBoundingClientRect();
  const origin = fixedContainingOrigin(panel);
  const gap = 6;
  const viewportRight = window.innerWidth - 8;
  const minWidth = compact ? 12 * 16 : rect.width;
  const width = Math.min(Math.max(rect.width, minWidth), window.innerWidth - 16);

  const rtl = getComputedStyle(trigger).direction === 'rtl';
  let left = rtl ? rect.right - width : rect.left;
  left = Math.min(Math.max(8, left), viewportRight - width);

  const spaceBelow = window.innerHeight - rect.bottom - gap;
  const spaceAbove = rect.top - gap;
  const openUp = spaceBelow < 10 * 16 && spaceAbove > spaceBelow;
  const maxHeight = Math.max(8 * 16, Math.min(22 * 16, openUp ? spaceAbove : spaceBelow));

  panel.dir = getComputedStyle(trigger).direction;
  panel.style.position = 'fixed';
  panel.style.inset = 'auto';
  panel.style.setProperty('inset-inline-start', 'auto');
  panel.style.setProperty('inset-inline-end', 'auto');
  panel.style.setProperty('inset-block-start', 'auto');
  panel.style.setProperty('inset-block-end', 'auto');
  panel.style.margin = '0';
  panel.style.right = 'auto';
  panel.style.left = `${left - origin.left}px`;
  panel.style.width = `${width}px`;
  panel.style.minWidth = `${width}px`;
  panel.style.maxWidth = `${width}px`;
  panel.style.zIndex = '2500';
  panel.style.maxHeight = `${maxHeight}px`;
  panel.style.visibility = 'visible';
  panel.classList.add('ss-panel--placed', 'ms-panel--placed');

  if (openUp) {
    panel.style.top = 'auto';
    panel.style.bottom = `${origin.bottom - rect.top + gap}px`;
  } else {
    panel.style.bottom = 'auto';
    panel.style.top = `${rect.bottom + gap - origin.top}px`;
  }
}

function createsFixedContainingBlock(style: CSSStyleDeclaration): boolean {
  if (style.transform !== 'none') return true;
  if (style.translate && style.translate !== 'none') return true;
  if (style.scale && style.scale !== 'none') return true;
  if (style.rotate && style.rotate !== 'none') return true;
  if (style.filter !== 'none') return true;
  if (style.backdropFilter && style.backdropFilter !== 'none') return true;
  if (style.perspective !== 'none') return true;
  if (/paint|layout|strict|content/.test(style.contain)) return true;
  if (/\b(transform|filter|perspective)\b/.test(style.willChange)) return true;
  const zoom = style.zoom;
  if (zoom && zoom !== 'normal' && zoom !== '1') return true;
  return false;
}

function fixedContainingOrigin(el: HTMLElement): DOMRect {
  let node: HTMLElement | null = el.parentElement;
  while (node && node !== document.documentElement) {
    if (createsFixedContainingBlock(getComputedStyle(node))) {
      return node.getBoundingClientRect();
    }
    if (node === document.body) break;
    node = node.parentElement;
  }
  return new DOMRect(0, 0, window.innerWidth, window.innerHeight);
}
