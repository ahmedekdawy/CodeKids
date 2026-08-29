import html2canvas from 'html2canvas';

function defaultExportBackground(element: HTMLElement): string {
  if (element.classList.contains('timetable-wrap')) {
    return '#ffffff';
  }
  return document.documentElement.dataset['theme'] === 'light' ? '#f6f8fb' : '#0a182a';
}

function inlineTimetableExportStyles(root: HTMLElement): void {
  const wrap = root.classList.contains('timetable-wrap')
    ? root
    : root.querySelector<HTMLElement>('.timetable-wrap');
  if (!wrap) return;

  wrap.style.background = '#ffffff';
  wrap.style.color = '#0f172a';
  wrap.style.overflow = 'visible';
  wrap.style.maxHeight = 'none';
  wrap.style.height = 'auto';
  wrap.style.borderColor = '#334155';

  const table = wrap.querySelector<HTMLElement>('.timetable');
  if (table) {
    table.style.fontFamily = "'Cairo', 'Space Grotesk', 'Segoe UI', Tahoma, sans-serif";
    table.style.color = '#0f172a';
  }

  wrap.querySelectorAll<HTMLElement>('.timetable thead th').forEach((el) => {
    el.style.background = '#f1f5f9';
    el.style.color = '#0f172a';
    el.style.position = 'static';
    el.style.borderColor = '#334155';
  });

  wrap.querySelectorAll<HTMLElement>('.timetable-day-col, .timetable-day').forEach((el) => {
    el.style.background = '#e2e8f0';
    el.style.color = '#0f172a';
    el.style.position = 'static';
    el.style.borderColor = '#334155';
  });

  wrap.querySelectorAll<HTMLElement>('.timetable-shift:not(.pm)').forEach((el) => {
    el.style.background = '#fef9c3';
    el.style.color = '#0f172a';
  });

  wrap.querySelectorAll<HTMLElement>('.timetable-shift.pm').forEach((el) => {
    el.style.background = '#ecfdf5';
    el.style.color = '#0f172a';
  });

  wrap.querySelectorAll<HTMLElement>('.timetable-session-id').forEach((el) => {
    el.style.color = '#0f172a';
  });

  wrap.querySelectorAll<HTMLElement>('.timetable-session-time').forEach((el) => {
    el.style.color = '#1e293b';
  });

  wrap.querySelectorAll<HTMLElement>('.timetable-cell:not(.pm)').forEach((el) => {
    el.style.background = '#ffffff';
    el.style.borderColor = '#334155';
  });

  wrap.querySelectorAll<HTMLElement>('.timetable-cell.pm').forEach((el) => {
    el.style.background = '#f8fafc';
    el.style.borderColor = '#334155';
  });

  wrap.querySelectorAll<HTMLElement>('.timetable-entry').forEach((el) => {
    el.style.background = '#ffffff';
    el.style.color = '#0f172a';
    el.style.borderColor = '#64748b';
  });

  wrap.querySelectorAll<HTMLElement>('.timetable-course').forEach((el) => {
    el.style.color = '#0f172a';
  });

  wrap.querySelectorAll<HTMLElement>('.timetable-teacher').forEach((el) => {
    el.style.color = '#475569';
  });

  wrap.querySelectorAll<HTMLElement>('.row-actions').forEach((el) => {
    el.style.display = 'none';
  });

  wrap.querySelectorAll<HTMLElement>('th, td').forEach((el) => {
    el.style.borderColor = '#334155';
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
