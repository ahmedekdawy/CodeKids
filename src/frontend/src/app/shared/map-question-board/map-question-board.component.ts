import {
  Component,
  DestroyRef,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  SimpleChanges,
  effect,
  inject,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { LearningApiService } from '../../learning-api.service';
import { MapMarkerDraft } from '../question-draft/question-draft.model';
import { parseMapAnswers } from '../question-draft/question-draft.util';
import { TranslatePipe } from '../translate.pipe';
import { IconActionButtonComponent } from '../icon-action-button/icon-action-button.component';

@Component({
  selector: 'app-map-question-board',
  imports: [FormsModule, TranslatePipe, IconActionButtonComponent],
  templateUrl: './map-question-board.component.html',
  styleUrl: './map-question-board.component.css'
})
export class MapQuestionBoardComponent implements OnChanges {
  private readonly http = inject(HttpClient);
  private readonly api = inject(LearningApiService);
  private readonly destroyRef = inject(DestroyRef);

  @Input() imageUrl: string | null | undefined = null;
  @Input() mode: 'edit' | 'play' = 'play';
  @Input() markers: MapMarkerDraft[] = [];
  /** Play mode: JSON string of answers keyed by marker id. */
  @Input() answerJson = '';
  @Input() namePrefix = 'map';

  @Output() readonly markersChange = new EventEmitter<MapMarkerDraft[]>();
  @Output() readonly answerJsonChange = new EventEmitter<string>();

  readonly blobUrl = signal<string | null>(null);
  /** Local editable copy so markers appear immediately after add/drag. */
  readonly localMarkers = signal<MapMarkerDraft[]>([]);
  placeKind: 'number' | 'arrow' = 'number';
  playAnswers: Record<string, string> = {};

  private activeObjectUrl: string | null = null;
  private dragId: string | null = null;
  private didDrag = false;
  private imageUrlSignal = signal<string | null | undefined>(null);
  private syncingFromParent = false;

  constructor() {
    effect(() => {
      const path = this.imageUrlSignal();
      this.clearObjectUrl();
      if (!path) {
        this.blobUrl.set(null);
        return;
      }
      const fullUrl = this.api.siteAssetUrl(path);
      if (!fullUrl) {
        this.blobUrl.set(null);
        return;
      }
      this.http
        .get(fullUrl, { responseType: 'blob' })
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (blob) => {
            this.clearObjectUrl();
            this.activeObjectUrl = URL.createObjectURL(blob);
            this.blobUrl.set(this.activeObjectUrl);
          },
          error: () => {
            this.clearObjectUrl();
            this.blobUrl.set(null);
          }
        });
    });
    this.destroyRef.onDestroy(() => this.clearObjectUrl());
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['imageUrl']) {
      this.imageUrlSignal.set(this.imageUrl);
    }
    if (changes['markers'] && !this.syncingFromParent) {
      this.localMarkers.set(cloneMarkers(this.markers || []));
    }
    if (changes['answerJson']) {
      this.playAnswers = { ...parseMapAnswers(this.answerJson) };
    }
    if (changes['answerJson'] || changes['markers']) {
      for (const marker of this.localMarkers()) {
        if (!(marker.id in this.playAnswers)) {
          this.playAnswers[marker.id] = '';
        }
      }
    }
  }

  addMarker(kind: 'number' | 'arrow', x?: number, y?: number): void {
    if (this.mode !== 'edit') return;
    this.placeKind = kind;
    const nextId = this.nextMarkerId();
    const useOffset = x == null || y == null;
    const offset = useOffset ? (this.localMarkers().length % 5) * 4 : 0;
    const marker: MapMarkerDraft = {
      id: nextId,
      x: clampPercent((x ?? 50) + offset),
      y: clampPercent((y ?? 50) + offset),
      label: kind === 'number' ? this.nextNumberLabel() : '',
      kind,
      prompt: '',
      correctAnswer: ''
    };
    this.commitMarkers([...this.localMarkers(), marker]);
  }

  onBoardClick(event: MouseEvent): void {
    if (this.mode !== 'edit' || this.didDrag) {
      this.didDrag = false;
      return;
    }
    const target = event.currentTarget as HTMLElement;
    const rect = target.getBoundingClientRect();
    if (rect.width <= 0 || rect.height <= 0) return;
    const x = ((event.clientX - rect.left) / rect.width) * 100;
    const y = ((event.clientY - rect.top) / rect.height) * 100;
    this.addMarker(this.placeKind, clampPercent(x), clampPercent(y));
  }

  onMarkerPointerDown(event: PointerEvent, markerId: string): void {
    if (this.mode !== 'edit') return;
    event.preventDefault();
    event.stopPropagation();
    this.dragId = markerId;
    this.didDrag = false;
    (event.target as HTMLElement).setPointerCapture?.(event.pointerId);
  }

  onBoardPointerMove(event: PointerEvent): void {
    if (this.mode !== 'edit' || !this.dragId) return;
    this.didDrag = true;
    const board = event.currentTarget as HTMLElement;
    const rect = board.getBoundingClientRect();
    if (rect.width <= 0 || rect.height <= 0) return;
    const x = clampPercent(((event.clientX - rect.left) / rect.width) * 100);
    const y = clampPercent(((event.clientY - rect.top) / rect.height) * 100);
    this.commitMarkers(
      this.localMarkers().map((marker) => (marker.id === this.dragId ? { ...marker, x, y } : marker)),
      false
    );
  }

  onBoardPointerUp(): void {
    if (this.dragId) {
      this.dragId = null;
      this.emitMarkers(this.localMarkers());
    }
  }

  removeMarker(id: string): void {
    this.commitMarkers(this.relabel(this.localMarkers().filter((marker) => marker.id !== id)));
  }

  setMarkerKind(id: string, kind: 'number' | 'arrow'): void {
    const updated = this.localMarkers().map((marker) =>
      marker.id === id
        ? {
            ...marker,
            kind,
            label: kind === 'number' ? marker.label || marker.id : ''
          }
        : marker
    );
    this.commitMarkers(this.relabel(updated));
  }

  onPromptChange(id: string, value: string): void {
    this.commitMarkers(
      this.localMarkers().map((marker) => (marker.id === id ? { ...marker, prompt: value } : marker))
    );
  }

  onCorrectAnswerChange(id: string, value: string): void {
    this.commitMarkers(
      this.localMarkers().map((marker) =>
        marker.id === id ? { ...marker, correctAnswer: value } : marker
      )
    );
  }

  onPlayAnswerChange(id: string, value: string): void {
    this.playAnswers = { ...this.playAnswers, [id]: value };
    this.answerJsonChange.emit(JSON.stringify(this.playAnswers));
  }

  numberLabel(marker: MapMarkerDraft): string {
    if (marker.kind === 'arrow') return '';
    return marker.label || marker.id;
  }

  answerLabel(marker: MapMarkerDraft): string {
    if (marker.kind === 'arrow') {
      const arrows = this.localMarkers().filter((m) => m.kind === 'arrow');
      const index = arrows.findIndex((m) => m.id === marker.id) + 1;
      return `↓${index}`;
    }
    return this.numberLabel(marker);
  }

  private commitMarkers(markers: MapMarkerDraft[], emit = true): void {
    this.localMarkers.set(markers);
    if (emit) this.emitMarkers(markers);
  }

  private emitMarkers(markers: MapMarkerDraft[]): void {
    this.syncingFromParent = true;
    this.markersChange.emit(cloneMarkers(markers));
    queueMicrotask(() => {
      this.syncingFromParent = false;
    });
  }

  private nextMarkerId(): string {
    let max = 0;
    for (const marker of this.localMarkers()) {
      const n = Number(marker.id);
      if (Number.isFinite(n) && n > max) max = n;
    }
    return String(max + 1);
  }

  private nextNumberLabel(): string {
    const count = this.localMarkers().filter((m) => m.kind === 'number').length;
    return String(count + 1);
  }

  /** Keep stable ids; renumber visible number labels only. */
  private relabel(markers: MapMarkerDraft[]): MapMarkerDraft[] {
    let numberIndex = 1;
    return markers.map((marker) => {
      if (marker.kind !== 'number') {
        return { ...marker, label: '' };
      }
      return { ...marker, label: String(numberIndex++) };
    });
  }

  private clearObjectUrl(): void {
    if (this.activeObjectUrl) {
      URL.revokeObjectURL(this.activeObjectUrl);
      this.activeObjectUrl = null;
    }
  }
}

function cloneMarkers(markers: MapMarkerDraft[]): MapMarkerDraft[] {
  return markers.map((marker) => ({
    id: marker.id,
    x: marker.x,
    y: marker.y,
    label: marker.label,
    kind: marker.kind === 'arrow' ? 'arrow' : 'number',
    prompt: marker.prompt || '',
    correctAnswer: marker.correctAnswer || ''
  }));
}

function clampPercent(value: number): number {
  if (Number.isNaN(value)) return 50;
  return Math.min(100, Math.max(0, value));
}
