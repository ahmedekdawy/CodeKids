import { Component, ElementRef, OnDestroy, OnInit, ViewChild, computed, inject, input, signal } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
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
  private readonly sanitizer = inject(DomSanitizer);
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

  readonly embedUrl = computed<SafeResourceUrl | null>(() => {
    const info = this.playback();
    if (!info?.isExternalLink) return null;
    const embed = toEmbedUrl(info.playbackUrl);
    return embed ? this.sanitizer.bypassSecurityTrustResourceUrl(embed) : null;
  });

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

  onVideoError(): void {
    this.error.set(this.locale.t('player.loadFailed'));
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

function toEmbedUrl(url: string): string | null {
  try {
    const u = new URL(url);
    const host = u.hostname.replace(/^www\./, '').toLowerCase();
    if (host === 'youtu.be') {
      const id = u.pathname.split('/').filter(Boolean)[0];
      return id ? `https://www.youtube.com/embed/${id}` : null;
    }
    if (host === 'youtube.com' || host === 'm.youtube.com' || host === 'youtube-nocookie.com') {
      if (u.pathname.startsWith('/embed/')) return url;
      const id = u.searchParams.get('v') || u.pathname.split('/').filter(Boolean).pop();
      return id ? `https://www.youtube.com/embed/${id}` : null;
    }
    if (host === 'vimeo.com') {
      const id = u.pathname.split('/').filter(Boolean)[0];
      return id ? `https://player.vimeo.com/video/${id}` : null;
    }
  } catch {
    return null;
  }
  return null;
}
