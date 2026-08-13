import html2canvas from 'html2canvas';

/** Capture an element as a PNG download, including content that is scrolled out of view. */
export async function downloadElementAsPng(
  element: HTMLElement,
  fileName: string
): Promise<void> {
  element.classList.add('exporting');
  const clone = element.cloneNode(true) as HTMLElement;
  clone.classList.add('exporting');
  const width = Math.max(element.scrollWidth, element.offsetWidth);
  const height = Math.max(element.scrollHeight, element.offsetHeight);
  clone.style.cssText = [
    'position:fixed',
    'left:0',
    'top:0',
    'z-index:-1',
    'overflow:visible',
    'max-height:none',
    'max-width:none',
    'height:auto',
    `width:${width}px`
  ].join(';');
  document.body.appendChild(clone);

  try {
    await new Promise((resolve) => requestAnimationFrame(() => resolve(undefined)));
    const captureHeight = Math.max(clone.scrollHeight, height);
    const captureWidth = Math.max(clone.scrollWidth, width);
    const canvas = await html2canvas(clone, {
      backgroundColor: '#0a182a',
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
      y: 0
    });
    const link = document.createElement('a');
    link.download = fileName.endsWith('.png') ? fileName : `${fileName}.png`;
    link.href = canvas.toDataURL('image/png');
    link.click();
  } finally {
    clone.remove();
    element.classList.remove('exporting');
  }
}
