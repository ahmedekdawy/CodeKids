import { Component, ElementRef, OnDestroy, OnInit, ViewChild, inject, input, signal } from '@angular/core';
import { LearningApiService } from '../../learning-api.service';
import { LocaleService } from '../../i18n/locale.service';
import { PlaybackInfo, WatchSession } from '../../models';
import { TranslatePipe } from '../translate.pipe';

@Component({
  selector: 'app-protected-video-player',
  imports: [TranslatePipe],
  templateUrl: './protected-video-player.component.html',
  styleUrl: './protected-video-player.component.css'
})
export class ProtectedVideoPlayerComponent implements OnInit, OnDestroy {
  private readonly api = inject(LearningApiService);
  readonly locale = inject(LocaleService);
  readonly mediaAssetId = input.required<string>();
  readonly lessonId = input<string | null>(null);
  readonly title = input('Lesson video');
  readonly autoLoad = input(false);
  readonly autoplay = input(false);
  readonly compact = input(false);

  @ViewChild('videoEl') videoEl?: ElementRef<HTMLVideoElement>;

  readonly playback = signal<PlaybackInfo | null>(null);
  readonly session = signal<WatchSession | null>(null);
  readonly error = signal('');
  readonly loading = signal(false);

  private heartbeatTimer: ReturnType<typeof setInterval> | null = null;
  private lastSeekFrom = 0;
  private autoplayAttempted = false;

  ngOnInit(): void {
    if (this.autoLoad()) {
      this.load();
    }
  }

  ngOnDestroy(): void {
    this.stopHeartbeat();
  }

  load(): void {
    if (this.playback()) return;
    this.loading.set(true);
    this.error.set('');
    this.api.getPlayback(this.mediaAssetId()).subscribe({
      next: (info) => {
        this.playback.set(info);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(this.locale.fromApiError(err, 'player.loadFailed'));
      }
    });
  }

  onCanPlay(): void {
    if (!this.autoplay() || this.autoplayAttempted) return;
    this.autoplayAttempted = true;
    const video = this.videoEl?.nativeElement;
    if (!video) return;
    video.play().catch(() => undefined);
  }

  onPlay(): void {
    this.emit('play');
    this.startHeartbeat();
  }

  onPause(): void {
    this.emit('pause');
    this.stopHeartbeat();
  }

  onEnded(): void {
    this.emit('ended');
    this.stopHeartbeat();
  }

  onRateChange(): void {
    const video = this.videoEl?.nativeElement;
    if (!video) return;
    this.emit('ratechange', { playbackRate: video.playbackRate });
  }

  onSeeking(): void {
    const video = this.videoEl?.nativeElement;
    if (!video) return;
    this.lastSeekFrom = Math.floor(video.currentTime);
  }

  onSeeked(): void {
    const video = this.videoEl?.nativeElement;
    if (!video) return;
    const to = Math.floor(video.currentTime);
    this.emit('seek', { fromSeconds: this.lastSeekFrom, toSeconds: to });
  }

  private startHeartbeat(): void {
    this.stopHeartbeat();
    this.heartbeatTimer = setInterval(() => this.emit('heartbeat'), 5000);
  }

  private stopHeartbeat(): void {
    if (this.heartbeatTimer) {
      clearInterval(this.heartbeatTimer);
      this.heartbeatTimer = null;
    }
  }

  private emit(
    eventType: string,
    extra?: { playbackRate?: number; fromSeconds?: number; toSeconds?: number }
  ): void {
    const video = this.videoEl?.nativeElement;
    if (!video) return;
    this.api
      .recordWatchEvents({
        mediaAssetId: this.mediaAssetId(),
        lessonId: this.lessonId(),
        sessionId: this.session()?.id,
        events: [
          {
            eventType,
            positionSeconds: Math.floor(video.currentTime || 0),
            playbackRate: extra?.playbackRate ?? video.playbackRate,
            fromSeconds: extra?.fromSeconds,
            toSeconds: extra?.toSeconds,
            clientAtUtc: new Date().toISOString()
          }
        ]
      })
      .subscribe({
        next: (session) => this.session.set(session),
        error: () => undefined
      });
  }
}
