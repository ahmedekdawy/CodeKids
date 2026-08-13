import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Classroom, LiveSession, ZoomConnectionStatus, ZoomOAuthSettings } from '../../models';
import { TranslatePipe } from '../../shared/translate.pipe';
import { environment } from '../../../environments/environment';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';

@Component({
  selector: 'app-teacher-zoom',
  imports: [PageFeedbackComponent, SearchableSelectComponent, FormsModule, TranslatePipe],
  templateUrl: './teacher-zoom.component.html',
  styleUrls: ['./teacher-panel.css', './teacher-zoom.component.css']
})
export class TeacherZoomComponent {
  private readonly api = inject(LearningApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly locale = inject(LocaleService);
  readonly meetings = signal<LiveSession[]>([]);
  readonly classrooms = signal<Classroom[]>([]);
  readonly zoomStatus = signal<ZoomConnectionStatus | null>(null);
  readonly oauthSettings = signal<ZoomOAuthSettings | null>(null);
  readonly creating = signal(false);
  readonly connecting = signal(false);
  readonly savingOauth = signal(false);
  readonly showWizard = signal(false);
  readonly wizardStep = signal(1);
  readonly error = signal('');
  readonly info = signal('');

  title = '';
  description = '';
  startsAtLocal = '';
  durationMinutes = 45;
  classroomId = '';
  notifyWhatsApp = true;

  clientId = '';
  clientSecret = '';
  redirectUri = environment.zoomCallbackUrl;
  frontendRedirectUri = environment.zoomFrontendRedirectUrl;

  constructor() {
    const defaultStart = new Date(Date.now() + 60 * 60 * 1000);
    defaultStart.setMinutes(0, 0, 0);
    this.startsAtLocal = this.toLocalInputValue(defaultStart);

    this.route.queryParamMap.subscribe((params) => {
      const zoom = params.get('zoom');
      if (zoom === 'connected') {
        this.info.set(this.locale.t('teacher.zoom.connected'));
      } else if (zoom === 'error') {
        const msg = params.get('message');
        this.error.set(
          msg
            ? this.locale.fromApiError({ error: { message: msg } }, 'teacher.zoom.connectFailed')
            : this.locale.t('teacher.zoom.connectFailed')
        );
      }
    });

    this.api.getClassrooms().subscribe((classrooms) => {
      this.classrooms.set(classrooms);
      if (!this.classroomId && classrooms[0]) this.classroomId = classrooms[0].id;
    });
    this.reloadMeetings();
    this.reloadZoomStatus();
    this.reloadOauthSettings();
  }

  openWizard(): void {
    this.error.set('');
    this.wizardStep.set(1);
    this.showWizard.set(true);
    this.reloadOauthSettings();
  }

  closeWizard(): void {
    this.showWizard.set(false);
  }

  nextStep(): void {
    this.wizardStep.update((s) => Math.min(3, s + 1));
  }

  prevStep(): void {
    this.wizardStep.update((s) => Math.max(1, s - 1));
  }

  copyRedirect(): void {
    const value = this.redirectUri || this.oauthSettings()?.suggestedRedirectUri || '';
    if (!value) return;
    navigator.clipboard?.writeText(value).then(
      () => this.info.set(this.locale.t('teacher.zoom.uriCopied')),
      () => undefined
    );
  }

  saveOauthSettings(): void {
    this.error.set('');
    this.info.set('');
    if (!this.clientId.trim()) {
      this.error.set(this.locale.t('teacher.zoom.clientIdRequired'));
      return;
    }
    if (!this.clientSecret.trim() && !this.oauthSettings()?.hasClientSecret) {
      this.error.set(this.locale.t('teacher.zoom.secretRequired'));
      return;
    }

    this.savingOauth.set(true);
    this.api
      .saveZoomOAuthSettings({
        clientId: this.clientId.trim(),
        clientSecret: this.clientSecret.trim() || null,
        redirectUri: this.redirectUri.trim() || null,
        frontendRedirectUri: this.frontendRedirectUri.trim() || null
      })
      .subscribe({
        next: (settings) => {
          this.savingOauth.set(false);
          this.oauthSettings.set(settings);
          this.clientSecret = '';
          this.info.set(this.locale.t('teacher.zoom.oauthSaved'));
          this.wizardStep.set(3);
          this.reloadZoomStatus();
        },
        error: (err) => {
          this.savingOauth.set(false);
          this.error.set(this.locale.fromApiError(err,'teacher.zoom.saveOauthFailed'));
        }
      });
  }

  connectZoom(): void {
    this.error.set('');
    if (!this.zoomStatus()?.userOAuthConfigured && !this.oauthSettings()?.configured) {
      this.openWizard();
      return;
    }

    this.connecting.set(true);
    this.api.getZoomConnectUrl().subscribe({
      next: (result) => {
        this.connecting.set(false);
        if (!result.userOAuthConfigured || !result.authorizeUrl) {
          this.openWizard();
          this.error.set(this.locale.t('teacher.zoom.completeWizard'));
          return;
        }
        window.location.href = result.authorizeUrl;
      },
      error: (err) => {
        this.connecting.set(false);
        this.error.set(this.locale.fromApiError(err,'teacher.zoom.startConnectFailed'));
      }
    });
  }

  disconnectZoom(): void {
    if (!confirm(this.locale.t('teacher.zoom.confirmDisconnect'))) return;
    this.api.disconnectZoom().subscribe({
      next: () => {
        this.info.set(this.locale.t('teacher.zoom.disconnected'));
        this.reloadZoomStatus();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err,'teacher.zoom.disconnectFailed'))
    });
  }

  createMeeting(): void {
    this.error.set('');
    this.info.set('');
    if (!this.title.trim() || !this.startsAtLocal || !this.classroomId) {
      this.error.set(this.locale.t('teacher.zoom.requiredFields'));
      return;
    }

    this.creating.set(true);
    const classroom = this.classrooms().find((c) => c.id === this.classroomId);
    this.api
      .createMeeting({
        title: this.title.trim(),
        description: this.description.trim() || undefined,
        startsAtUtc: new Date(this.startsAtLocal).toISOString(),
        durationMinutes: this.durationMinutes,
        classroomId: this.classroomId,
        courseId: classroom?.courseId || null,
        notifyWhatsApp: this.notifyWhatsApp
      })
      .subscribe({
        next: (meeting) => {
          this.title = '';
          this.description = '';
          this.creating.set(false);
          this.info.set(meeting.whatsAppStatus || this.locale.t('teacher.zoom.meetingCreated'));
          this.reloadMeetings();
        },
        error: (err) => {
          this.creating.set(false);
          this.error.set(this.locale.fromApiError(err,'teacher.zoom.createMeetingFailed'));
        }
      });
  }

  formatWhen(iso: string): string {
    return new Date(iso).toLocaleString(this.locale.lang());
  }

  private reloadMeetings(): void {
    this.api.getMeetings().subscribe((meetings) => this.meetings.set(meetings));
  }

  private reloadZoomStatus(): void {
    this.api.getZoomStatus().subscribe({
      next: (status) => {
        this.zoomStatus.set(status);
      },
      error: () => this.zoomStatus.set(null)
    });
  }

  private reloadOauthSettings(): void {
    this.api.getZoomOAuthSettings().subscribe({
      next: (settings) => {
        this.oauthSettings.set(settings);
        this.clientId = settings.clientId || '';
        this.redirectUri = settings.redirectUri || settings.suggestedRedirectUri;
        this.frontendRedirectUri = settings.frontendRedirectUri || environment.zoomFrontendRedirectUrl;
        if (!settings.configured) {
          this.showWizard.set(true);
          this.wizardStep.set(1);
        }
      },
      error: () => this.oauthSettings.set(null)
    });
  }

  private toLocalInputValue(date: Date): string {
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }
}
