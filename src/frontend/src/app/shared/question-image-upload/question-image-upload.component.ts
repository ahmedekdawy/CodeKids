import { Component, DestroyRef, ElementRef, HostListener, effect, inject, input, output, signal, viewChild } from '@angular/core';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { TranslatePipe } from '../translate.pipe';
import { ImageCropEditorComponent } from '../image-crop-editor/image-crop-editor.component';
import { QuestionImageDisplayComponent } from '../question-image-display/question-image-display.component';

@Component({
  selector: 'app-question-image-upload',
  imports: [TranslatePipe, QuestionImageDisplayComponent, ImageCropEditorComponent],
  templateUrl: './question-image-upload.component.html',
  styleUrl: './question-image-upload.component.css'
})
export class QuestionImageUploadComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private stream: MediaStream | null = null;

  readonly mediaAssetId = input<string | null>(null);
  readonly imageUrl = input<string | null>(null);
  readonly mediaAssetIdChange = output<string | null>();
  readonly imageUrlChange = output<string | null>();

  readonly uploading = signal(false);
  readonly capturing = signal(false);
  readonly pasteArmed = signal(false);
  readonly editingFile = signal<File | null>(null);
  readonly error = signal('');

  private readonly videoEl = viewChild<ElementRef<HTMLVideoElement>>('cameraVideo');
  private readonly cameraFileEl = viewChild<ElementRef<HTMLInputElement>>('cameraFile');

  constructor() {
    this.destroyRef.onDestroy(() => this.stopCamera());
    effect(() => {
      const video = this.videoEl()?.nativeElement;
      if (!this.capturing() || !video || !this.stream) return;
      video.srcObject = this.stream;
      void video.play();
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    this.handleIncomingFile(file);
  }

  armPaste(): void {
    this.error.set('');
    this.pasteArmed.set(true);
  }

  @HostListener('document:paste', ['$event'])
  onDocumentPaste(event: ClipboardEvent): void {
    if (!this.pasteArmed() || this.uploading() || this.editingFile()) return;

    const items = Array.from(event.clipboardData?.items ?? []);
    const imageItem = items.find((item) => item.kind === 'file' && item.type.startsWith('image/'));
    const file = imageItem?.getAsFile();
    if (!file) {
      this.error.set(this.locale.t('teacher.questionImage.pasteEmpty'));
      return;
    }

    event.preventDefault();
    this.pasteArmed.set(false);
    this.handleIncomingFile(file);
  }

  async openCamera(): Promise<void> {
    this.error.set('');
    if (!navigator.mediaDevices?.getUserMedia) {
      this.cameraFileEl()?.nativeElement.click();
      return;
    }

    try {
      this.stream = await navigator.mediaDevices.getUserMedia({
        audio: false,
        video: { facingMode: { ideal: 'environment' }, width: { ideal: 1920 }, height: { ideal: 1080 } }
      });
      this.capturing.set(true);
    } catch {
      this.stopCamera();
      this.cameraFileEl()?.nativeElement.click();
    }
  }

  async snapPhoto(): Promise<void> {
    const video = this.videoEl()?.nativeElement;
    if (!video || video.videoWidth === 0) {
      this.error.set(this.locale.t('teacher.questionImage.cameraDenied'));
      return;
    }

    const canvas = document.createElement('canvas');
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    const ctx = canvas.getContext('2d');
    if (!ctx) {
      this.error.set(this.locale.t('teacher.questionImage.cameraDenied'));
      return;
    }

    ctx.drawImage(video, 0, 0);
    this.stopCamera();

    const blob = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, 'image/jpeg', 0.85));
    if (!blob) {
      this.error.set(this.locale.t('teacher.questionImage.cameraDenied'));
      return;
    }

    this.handleIncomingFile(new File([blob], `question-capture-${Date.now()}.jpg`, { type: 'image/jpeg' }));
  }

  cancelCamera(): void {
    this.stopCamera();
  }

  onCropApplied(file: File): void {
    this.editingFile.set(null);
    this.uploadFile(file);
  }

  cancelCrop(): void {
    this.editingFile.set(null);
  }

  clear(): void {
    this.mediaAssetIdChange.emit(null);
    this.imageUrlChange.emit(null);
    this.error.set('');
  }

  private handleIncomingFile(file: File): void {
    this.error.set('');
    if (!file.type.startsWith('image/') || file.type === 'image/gif') {
      this.uploadFile(file);
      return;
    }
    this.editingFile.set(file);
    setTimeout(() => this.host.nativeElement.scrollIntoView({ block: 'nearest', behavior: 'smooth' }));
  }

  private stopCamera(): void {
    this.stream?.getTracks().forEach((track) => track.stop());
    this.stream = null;
    const video = this.videoEl()?.nativeElement;
    if (video) video.srcObject = null;
    this.capturing.set(false);
  }

  private uploadFile(file: File): void {
    this.error.set('');
    this.uploading.set(true);
    this.api.uploadQuestionImage(file).subscribe({
      next: (result) => {
        this.uploading.set(false);
        this.mediaAssetIdChange.emit(result.id);
        this.imageUrlChange.emit(result.url);
      },
      error: (err) => {
        this.uploading.set(false);
        this.error.set(this.locale.fromApiError(err, 'teacher.questionImage.uploadFailed'));
      }
    });
  }
}
