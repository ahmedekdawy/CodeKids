import { Injectable, effect, inject } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { Subject, catchError, debounceTime, filter, forkJoin, merge, of } from 'rxjs';
import { AuthService } from './auth.service';
import { ChatRealtimeService } from './chat-realtime.service';
import { LearningApiService } from './learning-api.service';
import { NotificationRealtimeService } from './notification-realtime.service';
import { unreadChanged } from './unread-changed';

type BadgeNavigator = Navigator & {
  setAppBadge?: (count?: number) => Promise<void>;
  clearAppBadge?: () => Promise<void>;
};

const CHAT_ROLES = new Set(['Student', 'Teacher', 'SuperAdmin']);

/**
 * Shows the unread count (notifications plus chat messages) on the installed app's icon. The server
 * holds the counts, so this re-reads them whenever they may have changed rather than tracking them here.
 * While the app is closed, app-sw.js sets the badge from the count each push carries.
 */
@Injectable({ providedIn: 'root' })
export class AppBadgeService {
  private readonly api = inject(LearningApiService);
  private readonly auth = inject(AuthService);
  private readonly badge = navigator as BadgeNavigator;
  private readonly refresh$ = new Subject<void>();

  constructor() {
    if (!this.badge.setAppBadge) return;

    this.refresh$.pipe(debounceTime(800)).subscribe(() => this.load());

    merge(
      unreadChanged,
      inject(NotificationRealtimeService).notifications$,
      inject(ChatRealtimeService).messages$,
      inject(Router).events.pipe(filter((event) => event instanceof NavigationEnd))
    ).subscribe(() => this.refresh$.next());

    document.addEventListener('visibilitychange', () => {
      if (document.visibilityState === 'visible') this.refresh$.next();
    });

    effect(() => {
      if (this.auth.token() && this.auth.user()) {
        this.refresh$.next();
      } else {
        this.show(0);
      }
    });
  }

  private load(): void {
    const role = this.auth.user()?.role;
    if (!this.auth.token() || !role) return;

    forkJoin({
      notifications: this.api.getNotificationUnreadSummary().pipe(catchError(() => of({ unreadCount: 0 }))),
      chat: CHAT_ROLES.has(role)
        ? this.api.getChatUnreadSummary().pipe(catchError(() => of({ totalUnread: 0 })))
        : of({ totalUnread: 0 })
    }).subscribe(({ notifications, chat }) => this.show(notifications.unreadCount + chat.totalUnread));
  }

  private show(count: number): void {
    const done = count > 0 ? this.badge.setAppBadge?.(count) : this.badge.clearAppBadge?.();
    void done?.catch(() => undefined);
  }
}
