import html2canvas from 'html2canvas';

/** Capture an element as a PNG download, optionally marking it for export-only CSS. */
export async function downloadElementAsPng(
  element: HTMLElement,
  fileName: string
): Promise<void> {
  element.classList.add('exporting');
  try {
    // Allow layout to hide controls before capture.
    await new Promise((resolve) => requestAnimationFrame(() => resolve(undefined)));
    const canvas = await html2canvas(element, {
      backgroundColor: '#0a182a',
      scale: Math.min(2, window.devicePixelRatio || 1),
      useCORS: true,
      logging: false,
      scrollX: 0,
      scrollY: 0
    });
    const link = document.createElement('a');
    link.download = fileName.endsWith('.png') ? fileName : `${fileName}.png`;
    link.href = canvas.toDataURL('image/png');
    link.click();
  } finally {
    element.classList.remove('exporting');
  }
}
