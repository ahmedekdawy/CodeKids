import { Injectable, effect, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';
import { AuthService } from './auth.service';
import { LocaleService } from './i18n/locale.service';
import { LearningApiService } from './learning-api.service';
import { AppNotification } from './models';
import { NotificationRealtimeService } from './notification-realtime.service';
import { ToastService } from './shared/toast/toast.service';

@Injectable({ providedIn: 'root' })
export class NotificationNotifyService {
  private readonly auth = inject(AuthService);
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  private readonly realtime = inject(NotificationRealtimeService);
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
      if (user.role !== 'Student' && user.role !== 'Parent') {
        return;
      }
      if (this.loginNotifiedFor === user.id) return;
      this.loginNotifiedFor = user.id;
      this.api.getNotificationUnreadSummary().subscribe({
        next: (summary) => {
          if (summary.unreadCount <= 0) return;
          this.toasts.notification(
            this.locale.t('notifications.unreadTitle'),
            this.locale.t('notifications.unreadLogin', { count: summary.unreadCount }),
            this.notificationsHref()
          );
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

    this.realtime.notifications$.pipe(takeUntilDestroyed()).subscribe((notification) => {
      if (!notification?.id || this.seen.has(notification.id)) return;
      this.seen.add(notification.id);
      if (this.seen.size > 200) {
        const first = this.seen.values().next().value;
        if (first) this.seen.delete(first);
      }

      if (this.isViewingTarget(notification.targetUrl)) return;

      this.toasts.notification(
        this.displayTitle(notification),
        this.displayBody(notification),
        notification.targetUrl
      );
    });
  }

  displayTitle(notification: AppNotification): string {
    return notification.title || this.locale.t('notifications.defaultTitle');
  }

  displayBody(notification: AppNotification): string {
    if (notification.relatedStudentId) {
      return notification.body;
    }

    const title = notification.title;
    switch (notification.kind) {
      case 'AssignmentCreated':
        return this.locale.t('notifications.assignmentCreated', { title });
      case 'ExamCreated':
        return this.locale.t('notifications.examCreated', { title });
      case 'QuizCreated':
        return this.locale.t('notifications.quizCreated', { title });
      case 'AssignmentGraded':
        return this.locale.t('notifications.assignmentGraded', { title });
      case 'ExamGraded':
        return this.locale.t('notifications.examGraded', { title });
      default:
        return notification.body;
    }
  }

  private isViewingTarget(targetUrl: string): boolean {
    if (!targetUrl) return false;
    const path = targetUrl.split('?')[0];
    return this.currentUrl.startsWith(path);
  }

  private notificationsHref(): string {
    const role = this.auth.user()?.role;
    if (role === 'Parent') return '/parent';
    return '/student';
  }
}
