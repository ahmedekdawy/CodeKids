import { Injectable, effect, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';
import { AuthService } from './auth.service';
import { ChatRealtimeService } from './chat-realtime.service';
import { LocaleService } from './i18n/locale.service';
import { LearningApiService } from './learning-api.service';
import { ToastService } from './shared/toast/toast.service';

@Injectable({ providedIn: 'root' })
export class ChatNotifyService {
  private readonly auth = inject(AuthService);
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  private readonly realtime = inject(ChatRealtimeService);
  private readonly toasts = inject(ToastService);
  private readonly router = inject(Router);
  private currentUrl = this.router.url;
  private readonly seen = new Set<string>();
  private loginNotifiedFor: string | null = null;

  constructor() {
    effect(() => {
      const token = this.auth.token();
      const user = this.auth.user();
      if (!token || !user?.id) {
        this.loginNotifiedFor = null;
        return;
      }
      if (user.role !== 'Student' && user.role !== 'Teacher' && user.role !== 'SuperAdmin') {
        return;
      }
      if (this.loginNotifiedFor === user.id) return;
      this.loginNotifiedFor = user.id;
      this.api.getChatUnreadSummary().subscribe({
        next: (summary) => {
          if (summary.totalUnread <= 0) return;
          const href = summary.roomId ? this.chatHref(summary.roomId) : this.chatBaseHref();
          if (!href) return;
          const text = summary.roomId
            ? this.locale.t('chat.unreadLoginWithRoom', {
                count: summary.totalUnread,
                room: summary.roomTitle
              })
            : this.locale.t('chat.unreadLogin', { count: summary.totalUnread });
          this.toasts.chat(this.locale.t('chat.unreadTitle'), text, href);
        }
      });
    });

    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed()
      )
      .subscribe((event) => {
        this.currentUrl = event.urlAfterRedirects;
      });

    this.realtime.messages$.pipe(takeUntilDestroyed()).subscribe((message) => {
      if (!message?.id || this.seen.has(message.id)) return;
      this.seen.add(message.id);
      if (this.seen.size > 200) {
        const first = this.seen.values().next().value;
        if (first) this.seen.delete(first);
      }

      if (message.isDeleted) return;
      if (message.senderId === this.auth.user()?.id) return;
      if (this.isViewingRoom(message.roomId)) return;

      const href = this.chatHref(message.roomId);
      if (!href) return;
      this.toasts.chat(message.senderName, message.body, href);
    });
  }

  private isViewingRoom(roomId: string): boolean {
    if (!this.currentUrl.includes('/chat')) return false;
    try {
      const url = new URL(this.currentUrl, 'http://local.invalid');
      return url.searchParams.get('room') === roomId;
    } catch {
      return this.currentUrl.includes(`room=${roomId}`);
    }
  }

  private chatBaseHref(): string | null {
    const role = this.auth.user()?.role;
    if (role === 'Teacher' || role === 'SuperAdmin') return '/teacher/chat';
    if (role === 'Student') return '/student/chat';
    return null;
  }

  private chatHref(roomId: string): string | null {
    const base = this.chatBaseHref();
    return base ? `${base}?room=${roomId}` : null;
  }
}
