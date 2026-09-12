import html2canvas from 'html2canvas';

function defaultExportBackground(element: HTMLElement): string {
  if (element.classList.contains('timetable-wrap')) {
    return '#fffdf7';
  }
  return document.documentElement.dataset['theme'] === 'light' ? '#f6f8fb' : '#0a182a';
}

function inlineTimetableExportStyles(root: HTMLElement): void {
  const wrap = root.classList.contains('timetable-wrap')
    ? root
    : root.querySelector<HTMLElement>('.timetable-wrap');
  if (!wrap) return;

  wrap.style.background =
    'radial-gradient(circle at 8% 12%, rgba(255, 214, 120, 0.35), transparent 22%), radial-gradient(circle at 92% 10%, rgba(168, 216, 255, 0.28), transparent 24%), linear-gradient(180deg, #fff8e8 0%, #fffdf7 48%, #f7fbff 100%)';
  wrap.style.color = '#1f2a44';
  wrap.style.overflow = 'visible';
  wrap.style.maxHeight = 'none';
  wrap.style.height = 'auto';
  wrap.style.borderColor = '#e8c46a';
  wrap.style.borderWidth = '3px';
  wrap.style.borderStyle = 'solid';
  wrap.style.borderRadius = '28px';
  wrap.style.padding = '1.1rem 1rem 1.25rem';

  const table = wrap.querySelector<HTMLElement>('.timetable');
  if (table) {
    table.style.fontFamily = "'Cairo', 'Baloo 2', 'Segoe UI', Tahoma, sans-serif";
    table.style.color = '#1f2a44';
    table.style.borderSpacing = '0.28rem';
  }

  wrap.querySelectorAll<HTMLElement>('.timetable thead th').forEach((el) => {
    el.style.background = 'linear-gradient(180deg, #ffe9a8, #f0c75a)';
    el.style.color = '#1d3a75';
    el.style.position = 'static';
    el.style.border = '2px solid #e0b13a';
    el.style.borderRadius = '16px';
  });

  wrap.querySelectorAll<HTMLElement>('.timetable-day-col, .timetable-day').forEach((el) => {
    el.style.background = 'linear-gradient(180deg, #ffe082, #ffd54f)';
    el.style.color = '#1d3a75';
    el.style.position = 'static';
    el.style.border = '2px solid #e0b13a';
    el.style.borderRadius = '18px';
  });

  wrap.querySelectorAll<HTMLElement>('.timetable-shift:not(.pm)').forEach((el) => {
    el.style.background = 'linear-gradient(180deg, #fff3c4, #f5d36a)';
    el.style.color = '#1d3a75';
  });

  wrap.querySelectorAll<HTMLElement>('.timetable-shift.pm').forEach((el) => {
    el.style.background = 'linear-gradient(180deg, #d8f5e8, #9ed9bf)';
    el.style.color = '#14553a';
  });

  wrap.querySelectorAll<HTMLElement>('.timetable-session-id').forEach((el) => {
    el.style.color = '#1d3a75';
  });

  wrap.querySelectorAll<HTMLElement>('.timetable-session-time').forEach((el) => {
    el.style.color = '#4a3b12';
  });

  wrap.querySelectorAll<HTMLElement>('.timetable-cell').forEach((el) => {
    el.style.background = 'rgba(255, 255, 255, 0.55)';
    el.style.border = '2px dashed rgba(180, 150, 90, 0.35)';
    el.style.borderRadius = '18px';
  });

  wrap.querySelectorAll<HTMLElement>('.timetable-cell.pm').forEach((el) => {
    el.style.background = 'rgba(232, 255, 246, 0.55)';
  });

  const toneColors: Record<string, { bg: string; border: string }> = {
    '0': { bg: '#ffe0e8', border: '#f5a3b8' },
    '1': { bg: '#e8d8ff', border: '#c4a6f5' },
    '2': { bg: '#d8ecff', border: '#9ec4f0' },
    '3': { bg: '#fff0b8', border: '#e6c85a' },
    '4': { bg: '#d8f5e8', border: '#8fd0b0' },
    '5': { bg: '#ffe6cc', border: '#f0b878' },
    '6': { bg: '#e0f0ff', border: '#9ebfe8' },
    '7': { bg: '#f3e0ff', border: '#d0a8ef' }
  };

  wrap.querySelectorAll<HTMLElement>('.timetable-entry').forEach((el) => {
    const tone = el.getAttribute('data-tone') ?? '0';
    const colors = toneColors[tone] ?? toneColors['0'];
    el.style.background = colors.bg;
    el.style.color = '#1f2a44';
    el.style.border = `2px solid ${colors.border}`;
    el.style.borderRadius = '16px';
  });

  wrap.querySelectorAll<HTMLElement>('.timetable-course').forEach((el) => {
    el.style.color = '#1f2a44';
  });

  wrap.querySelectorAll<HTMLElement>('.timetable-teacher').forEach((el) => {
    el.style.color = '#4b5568';
  });

  wrap.querySelectorAll<HTMLElement>('.row-actions').forEach((el) => {
    el.style.display = 'none';
  });
}

/** Capture an element as a PNG download, including content that is scrolled out of view. */
export async function downloadElementAsPng(
  element: HTMLElement,
  fileName: string,
  options?: { backgroundColor?: string }
): Promise<void> {
  await document.fonts.ready;

  const isTimetable = element.classList.contains('timetable-wrap');
  const backgroundColor = options?.backgroundColor ?? defaultExportBackground(element);

  element.classList.add('exporting');

  const prevOverflow = element.style.overflow;
  const prevMaxHeight = element.style.maxHeight;
  const prevHeight = element.style.height;

  if (isTimetable) {
    element.style.overflow = 'visible';
    element.style.maxHeight = 'none';
    element.style.height = 'auto';
  }

  try {
    await new Promise<void>((resolve) => requestAnimationFrame(() => requestAnimationFrame(() => resolve())));

    const captureWidth = Math.max(element.scrollWidth, element.offsetWidth, 1);
    const captureHeight = Math.max(element.scrollHeight, element.offsetHeight, 1);

    const canvas = await html2canvas(element, {
      backgroundColor,
      scale: Math.min(2, window.devicePixelRatio || 1),
      useCORS: true,
      logging: false,
      width: captureWidth,
      height: captureHeight,
      windowWidth: captureWidth,
      windowHeight: captureHeight,
      scrollX: 0,
      scrollY: 0,
      x: 0,
      y: 0,
      onclone: (_doc, cloned) => {
        if (isTimetable) {
          inlineTimetableExportStyles(cloned as HTMLElement);
        }
      }
    });

    const link = document.createElement('a');
    link.download = fileName.endsWith('.png') ? fileName : `${fileName}.png`;
    link.href = canvas.toDataURL('image/png');
    document.body.appendChild(link);
    link.click();
    link.remove();
  } finally {
    element.classList.remove('exporting');
    element.style.overflow = prevOverflow;
    element.style.maxHeight = prevMaxHeight;
    element.style.height = prevHeight;
  }
}
