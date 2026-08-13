export function dropdownPanelStyle(
  trigger: HTMLElement,
  compact = false
): Record<string, string> {
  const rect = trigger.getBoundingClientRect();
  const origin = fixedContainingOrigin(trigger);
  const gap = 6;
  const viewportRight = window.innerWidth - 8;
  const minWidth = compact ? 12 * 16 : rect.width;
  const width = Math.min(Math.max(rect.width, minWidth), window.innerWidth - 16);
  const rtl = document.documentElement.dir === 'rtl';

  let left = rtl ? rect.right - width : rect.left;
  left = Math.min(Math.max(8, left), viewportRight - width);

  const spaceBelow = window.innerHeight - rect.bottom - gap;
  const spaceAbove = rect.top - gap;
  const openUp = spaceBelow < 10 * 16 && spaceAbove > spaceBelow;
  const maxHeight = Math.max(8 * 16, Math.min(22 * 16, openUp ? spaceAbove : spaceBelow));

  const style: Record<string, string> = {
    position: 'fixed',
    left: `${left - origin.left}px`,
    right: 'auto',
    width: `${width}px`,
    zIndex: '1200',
    maxHeight: `${maxHeight}px`
  };

  if (openUp) {
    style['bottom'] = `${origin.bottom - rect.top + gap}px`;
    style['top'] = 'auto';
  } else {
    style['top'] = `${rect.bottom + gap - origin.top}px`;
    style['bottom'] = 'auto';
  }

  return style;
}

function fixedContainingOrigin(el: HTMLElement): DOMRect {
  let node: HTMLElement | null = el.parentElement;
  while (node && node !== document.body && node !== document.documentElement) {
    const style = getComputedStyle(node);
    const transform = style.transform !== 'none';
    const filter = style.filter !== 'none';
    const backdrop = Boolean(style.backdropFilter && style.backdropFilter !== 'none');
    const perspective = style.perspective !== 'none';
    const contain = /paint|layout|strict|content/.test(style.contain);
    const willChange = /\b(transform|filter|perspective)\b/.test(style.willChange);
    if (transform || filter || backdrop || perspective || contain || willChange) {
      return node.getBoundingClientRect();
    }
    node = node.parentElement;
  }
  return new DOMRect(0, 0, window.innerWidth, window.innerHeight);
}
