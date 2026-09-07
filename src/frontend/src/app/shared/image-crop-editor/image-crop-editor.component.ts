import { Component, DestroyRef, ElementRef, computed, effect, inject, input, output, signal, viewChild } from '@angular/core';
import { LocaleService } from '../../i18n/locale.service';
import { TranslatePipe } from '../translate.pipe';

type CropHandle = 'nw' | 'ne' | 'sw' | 'se' | 'n' | 's' | 'e' | 'w';
type DragMode = 'new' | 'move' | CropHandle;
type Rotation = 0 | 90 | 180 | 270;

interface CropRect {
  x: number;
  y: number;
  w: number;
  h: number;
}

interface DragState {
  mode: DragMode;
  originX: number;
  originY: number;
  startRect: CropRect;
}

export interface AspectPreset {
  key: string;
  labelKey: string;
  ratio: number | null;
}

const MIN_SIZE = 0.02;

const ASPECT_PRESETS: AspectPreset[] = [
  { key: 'free', labelKey: 'teacher.questionImage.ratioFree', ratio: null },
  { key: '1x1', labelKey: 'teacher.questionImage.ratio1x1', ratio: 1 },
  { key: '4x3', labelKey: 'teacher.questionImage.ratio4x3', ratio: 4 / 3 },
  { key: '3x4', labelKey: 'teacher.questionImage.ratio3x4', ratio: 3 / 4 },
  { key: '16x9', labelKey: 'teacher.questionImage.ratio16x9', ratio: 16 / 9 }
];

@Component({
  selector: 'app-image-crop-editor',
  imports: [TranslatePipe],
  templateUrl: './image-crop-editor.component.html',
  styleUrl: './image-crop-editor.component.css'
})
export class ImageCropEditorComponent {
  private readonly locale = inject(LocaleService);
  private readonly destroyRef = inject(DestroyRef);

  readonly source = input.required<File>();
  readonly applied = output<File>();
  readonly cancelled = output<void>();

  readonly aspectPresets = ASPECT_PRESETS;
  readonly cornerHandles: CropHandle[] = ['nw', 'ne', 'sw', 'se'];
  readonly edgeHandles: CropHandle[] = ['n', 's', 'e', 'w'];

  readonly ready = signal(false);
  readonly working = signal(false);
  readonly error = signal('');
  readonly rect = signal<CropRect>({ x: 0, y: 0, w: 1, h: 1 });
  readonly rotation = signal<Rotation>(0);
  readonly flipH = signal(false);
  readonly flipV = signal(false);
  readonly aspectKey = signal('free');
  readonly scalePercent = signal(100);
  readonly grid = signal(true);

  private readonly canvasEl = viewChild<ElementRef<HTMLCanvasElement>>('workCanvas');
  private readonly sourceImage = signal<HTMLImageElement | null>(null);
  private readonly workSize = signal({ width: 0, height: 0 });
  private drag: DragState | null = null;

  readonly cropPixels = computed(() => {
    const work = this.workSize();
    const crop = this.rect();
    return {
      width: Math.max(1, Math.round(crop.w * work.width)),
      height: Math.max(1, Math.round(crop.h * work.height))
    };
  });

  readonly outputSize = computed(() => {
    const crop = this.cropPixels();
    const scale = this.scalePercent() / 100;
    return {
      width: Math.max(1, Math.round(crop.width * scale)),
      height: Math.max(1, Math.round(crop.height * scale))
    };
  });

  constructor() {
    effect((onCleanup) => {
      const url = URL.createObjectURL(this.source());
      const image = new Image();
      image.onload = () => {
        this.sourceImage.set(image);
        this.resetAll();
        this.ready.set(true);
      };
      image.onerror = () => this.error.set(this.locale.t('teacher.questionImage.cropLoadFailed'));
      image.src = url;
      onCleanup(() => URL.revokeObjectURL(url));
    });

    effect(() => {
      this.redraw();
    });

    this.destroyRef.onDestroy(() => this.detachDragListeners());
  }

  aspectRatio(): number | null {
    return ASPECT_PRESETS.find((preset) => preset.key === this.aspectKey())?.ratio ?? null;
  }

  setAspect(key: string): void {
    this.aspectKey.set(key);
    const ratio = this.aspectRatio();
    if (!ratio) return;

    const crop = this.rect();
    const centerX = crop.x + crop.w / 2;
    const centerY = crop.y + crop.h / 2;
    const norm = this.normalizedAspect(ratio);
    let w = crop.w;
    let h = w / norm;
    if (h > 1) {
      h = 1;
      w = h * norm;
    }
    if (w > 1) {
      w = 1;
      h = w / norm;
    }
    this.rect.set({
      x: this.clamp(centerX - w / 2, 0, 1 - w),
      y: this.clamp(centerY - h / 2, 0, 1 - h),
      w,
      h
    });
  }

  rotate(direction: -1 | 1): void {
    const next = (((this.rotation() + direction * 90) % 360) + 360) % 360;
    this.rotation.set(next as Rotation);
    const crop = this.rect();
    this.rect.set(
      direction === 1
        ? { x: 1 - crop.y - crop.h, y: crop.x, w: crop.h, h: crop.w }
        : { x: crop.y, y: 1 - crop.x - crop.w, w: crop.h, h: crop.w }
    );
    if (this.aspectRatio()) this.setAspect(this.aspectKey());
  }

  toggleFlipH(): void {
    this.flipH.update((value) => !value);
    const crop = this.rect();
    this.rect.set({ ...crop, x: 1 - crop.x - crop.w });
  }

  toggleFlipV(): void {
    this.flipV.update((value) => !value);
    const crop = this.rect();
    this.rect.set({ ...crop, y: 1 - crop.y - crop.h });
  }

  toggleGrid(): void {
    this.grid.update((value) => !value);
  }

  setScale(value: string): void {
    const parsed = Number(value);
    if (Number.isFinite(parsed)) this.scalePercent.set(this.clamp(parsed, 10, 100));
  }

  setOutputWidth(value: string): void {
    const target = Number(value);
    const cropWidth = this.cropPixels().width;
    if (!Number.isFinite(target) || target <= 0 || cropWidth === 0) return;
    this.scalePercent.set(this.clamp((target / cropWidth) * 100, 10, 100));
  }

  selectAll(): void {
    this.rect.set({ x: 0, y: 0, w: 1, h: 1 });
    if (this.aspectRatio()) this.setAspect(this.aspectKey());
  }

  resetAll(): void {
    this.rect.set({ x: 0, y: 0, w: 1, h: 1 });
    this.rotation.set(0);
    this.flipH.set(false);
    this.flipV.set(false);
    this.aspectKey.set('free');
    this.scalePercent.set(100);
    this.error.set('');
  }

  startDrag(event: PointerEvent, mode: DragMode): void {
    if (!this.ready() || event.button !== 0) return;
    event.preventDefault();
    event.stopPropagation();

    const point = this.pointToImage(event);
    if (!point) return;

    if (mode === 'new') {
      this.rect.set({ x: point.x, y: point.y, w: 0, h: 0 });
    }
    this.drag = { mode, originX: point.x, originY: point.y, startRect: this.rect() };
    window.addEventListener('pointermove', this.onDragMove);
    window.addEventListener('pointerup', this.onDragEnd);
    window.addEventListener('pointercancel', this.onDragEnd);
  }

  async apply(): Promise<void> {
    const canvas = this.canvasEl()?.nativeElement;
    if (!canvas || !this.ready()) return;

    const crop = this.normalizedRect();
    const { width, height } = this.outputSize();

    const target = document.createElement('canvas');
    target.width = width;
    target.height = height;
    const ctx = target.getContext('2d');
    if (!ctx) {
      this.error.set(this.locale.t('teacher.questionImage.cropFailed'));
      return;
    }

    const type = this.source().type === 'image/png' ? 'image/png' : 'image/jpeg';
    ctx.imageSmoothingQuality = 'high';
    if (type === 'image/jpeg') {
      ctx.fillStyle = '#ffffff';
      ctx.fillRect(0, 0, width, height);
    }
    ctx.drawImage(
      canvas,
      Math.round(crop.x * canvas.width),
      Math.round(crop.y * canvas.height),
      Math.max(1, Math.round(crop.w * canvas.width)),
      Math.max(1, Math.round(crop.h * canvas.height)),
      0,
      0,
      width,
      height
    );

    this.working.set(true);
    const blob = await new Promise<Blob | null>((resolve) => target.toBlob(resolve, type, 0.9));
    this.working.set(false);

    if (!blob) {
      this.error.set(this.locale.t('teacher.questionImage.cropFailed'));
      return;
    }

    const extension = type === 'image/png' ? 'png' : 'jpg';
    this.applied.emit(new File([blob], `question-image-${Date.now()}.${extension}`, { type }));
  }

  private redraw(): void {
    const image = this.sourceImage();
    const canvas = this.canvasEl()?.nativeElement;
    if (!image || !canvas) return;

    const rotation = this.rotation();
    const swapped = rotation === 90 || rotation === 270;
    const width = swapped ? image.naturalHeight : image.naturalWidth;
    const height = swapped ? image.naturalWidth : image.naturalHeight;
    canvas.width = width;
    canvas.height = height;
    this.workSize.set({ width, height });

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    ctx.save();
    ctx.clearRect(0, 0, width, height);
    ctx.translate(width / 2, height / 2);
    ctx.rotate((rotation * Math.PI) / 180);
    ctx.scale(this.flipH() ? -1 : 1, this.flipV() ? -1 : 1);
    ctx.drawImage(image, -image.naturalWidth / 2, -image.naturalHeight / 2);
    ctx.restore();
  }

  private readonly onDragMove = (event: PointerEvent): void => {
    const state = this.drag;
    const point = this.pointToImage(event);
    if (!state || !point) return;

    if (state.mode === 'move') {
      const start = state.startRect;
      this.rect.set({
        x: this.clamp(start.x + (point.x - state.originX), 0, 1 - start.w),
        y: this.clamp(start.y + (point.y - state.originY), 0, 1 - start.h),
        w: start.w,
        h: start.h
      });
      return;
    }

    if (this.edgeHandles.includes(state.mode as CropHandle)) {
      this.rect.set(this.resizeEdge(state, point));
      return;
    }

    this.rect.set(this.resizeCorner(state, point));
  };

  private readonly onDragEnd = (): void => {
    if (!this.drag) return;
    this.drag = null;
    this.detachDragListeners();
    this.rect.set(this.normalizedRect());
  };

  private detachDragListeners(): void {
    window.removeEventListener('pointermove', this.onDragMove);
    window.removeEventListener('pointerup', this.onDragEnd);
    window.removeEventListener('pointercancel', this.onDragEnd);
  }

  private resizeCorner(state: DragState, point: { x: number; y: number }): CropRect {
    const anchor = this.anchorFor(state);
    const towardRight = point.x >= anchor.x;
    const towardBottom = point.y >= anchor.y;
    const limitW = towardRight ? 1 - anchor.x : anchor.x;
    const limitH = towardBottom ? 1 - anchor.y : anchor.y;

    let w = Math.min(Math.abs(point.x - anchor.x), limitW);
    let h = Math.min(Math.abs(point.y - anchor.y), limitH);

    const ratio = this.aspectRatio();
    if (ratio) {
      const norm = this.normalizedAspect(ratio);
      w = Math.max(w, h * norm);
      h = w / norm;
      if (w > limitW) {
        w = limitW;
        h = w / norm;
      }
      if (h > limitH) {
        h = limitH;
        w = h * norm;
      }
    }

    return {
      x: towardRight ? anchor.x : anchor.x - w,
      y: towardBottom ? anchor.y : anchor.y - h,
      w,
      h
    };
  }

  private resizeEdge(state: DragState, point: { x: number; y: number }): CropRect {
    const start = state.startRect;
    let next: CropRect = { ...start };

    switch (state.mode) {
      case 'e':
        next.w = this.clamp(point.x - start.x, MIN_SIZE, 1 - start.x);
        break;
      case 'w': {
        const right = start.x + start.w;
        next.x = this.clamp(point.x, 0, right - MIN_SIZE);
        next.w = right - next.x;
        break;
      }
      case 's':
        next.h = this.clamp(point.y - start.y, MIN_SIZE, 1 - start.y);
        break;
      case 'n': {
        const bottom = start.y + start.h;
        next.y = this.clamp(point.y, 0, bottom - MIN_SIZE);
        next.h = bottom - next.y;
        break;
      }
    }

    const ratio = this.aspectRatio();
    if (ratio) {
      const norm = this.normalizedAspect(ratio);
      if (state.mode === 'e' || state.mode === 'w') {
        next.h = Math.min(next.w / norm, 1 - next.y);
        next.w = next.h * norm;
      } else {
        next.w = Math.min(next.h * norm, 1 - next.x);
        next.h = next.w / norm;
      }
    }

    return next;
  }

  private anchorFor(state: DragState): { x: number; y: number } {
    const start = state.startRect;
    switch (state.mode) {
      case 'nw':
        return { x: start.x + start.w, y: start.y + start.h };
      case 'ne':
        return { x: start.x, y: start.y + start.h };
      case 'sw':
        return { x: start.x + start.w, y: start.y };
      case 'se':
        return { x: start.x, y: start.y };
      default:
        return { x: state.originX, y: state.originY };
    }
  }

  private normalizedAspect(ratio: number): number {
    const work = this.workSize();
    if (work.width === 0 || work.height === 0) return ratio;
    return (ratio * work.height) / work.width;
  }

  private normalizedRect(): CropRect {
    const crop = this.rect();
    const w = Math.max(MIN_SIZE, Math.min(crop.w, 1));
    const h = Math.max(MIN_SIZE, Math.min(crop.h, 1));
    return { x: this.clamp(crop.x, 0, 1 - w), y: this.clamp(crop.y, 0, 1 - h), w, h };
  }

  private pointToImage(event: PointerEvent): { x: number; y: number } | null {
    const canvas = this.canvasEl()?.nativeElement;
    if (!canvas) return null;
    const bounds = canvas.getBoundingClientRect();
    if (bounds.width === 0 || bounds.height === 0) return null;
    return {
      x: this.clamp((event.clientX - bounds.left) / bounds.width, 0, 1),
      y: this.clamp((event.clientY - bounds.top) / bounds.height, 0, 1)
    };
  }

  private clamp(value: number, min: number, max: number): number {
    return Math.min(max, Math.max(min, value));
  }
}
