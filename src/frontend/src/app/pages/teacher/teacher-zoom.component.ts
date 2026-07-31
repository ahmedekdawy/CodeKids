import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { LearningApiService } from '../../learning-api.service';
import { Classroom, LiveSession, ZoomConnectionStatus } from '../../models';

@Component({
  selector: 'app-teacher-zoom',
  imports: [FormsModule],
  templateUrl: './teacher-zoom.component.html',
  styleUrl: './teacher-panel.css'
})
export class TeacherZoomComponent {
  private readonly api = inject(LearningApiService);
  private readonly route = inject(ActivatedRoute);
  readonly meetings = signal<LiveSession[]>([]);
  readonly classrooms = signal<Classroom[]>([]);
  readonly zoomStatus = signal<ZoomConnectionStatus | null>(null);
  readonly creating = signal(false);
  readonly connecting = signal(false);
  readonly error = signal('');
  readonly info = signal('');

  title = '';
  description = '';
  startsAtLocal = '';
  durationMinutes = 45;
  classroomId = '';
  notifyWhatsApp = true;

  constructor() {
    const defaultStart = new Date(Date.now() + 60 * 60 * 1000);
    defaultStart.setMinutes(0, 0, 0);
    this.startsAtLocal = this.toLocalInputValue(defaultStart);

    this.route.queryParamMap.subscribe((params) => {
      const zoom = params.get('zoom');
      if (zoom === 'connected') {
        this.info.set('Personal Zoom account connected.');
      } else if (zoom === 'error') {
        this.error.set(params.get('message') || 'Could not connect Zoom.');
      }
    });

    this.api.getClassrooms().subscribe((classrooms) => {
      this.classrooms.set(classrooms);
      if (!this.classroomId && classrooms[0]) this.classroomId = classrooms[0].id;
    });
    this.reloadMeetings();
    this.reloadZoomStatus();
  }

  connectZoom(): void {
    this.error.set('');
    this.connecting.set(true);
    this.api.getZoomConnectUrl().subscribe({
      next: (result) => {
        this.connecting.set(false);
        if (!result.userOAuthConfigured || !result.authorizeUrl) {
          this.error.set(
            'Personal Zoom OAuth is not configured. Set Zoom:UserOAuthClientId and Zoom:UserOAuthClientSecret in the API.'
          );
          return;
        }
        window.location.href = result.authorizeUrl;
      },
      error: (err) => {
        this.connecting.set(false);
        this.error.set(err?.error?.message || 'Could not start Zoom connect.');
      }
    });
  }

  disconnectZoom(): void {
    if (!confirm('Disconnect your personal Zoom account?')) return;
    this.api.disconnectZoom().subscribe({
      next: () => {
        this.info.set('Personal Zoom disconnected. Meetings will use app Zoom (or mock) if configured.');
        this.reloadZoomStatus();
      },
      error: (err) => this.error.set(err?.error?.message || 'Could not disconnect Zoom.')
    });
  }

  createMeeting(): void {
    this.error.set('');
    this.info.set('');
    if (!this.title.trim() || !this.startsAtLocal || !this.classroomId) {
      this.error.set('Title, classroom, and start time are required.');
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
          this.info.set(meeting.whatsAppStatus || 'Meeting created.');
          this.reloadMeetings();
        },
        error: (err) => {
          this.creating.set(false);
          this.error.set(err?.error?.message || 'Could not create Zoom meeting.');
        }
      });
  }

  formatWhen(iso: string): string {
    return new Date(iso).toLocaleString();
  }

  private reloadMeetings(): void {
    this.api.getMeetings().subscribe((meetings) => this.meetings.set(meetings));
  }

  private reloadZoomStatus(): void {
    this.api.getZoomStatus().subscribe({
      next: (status) => this.zoomStatus.set(status),
      error: () => this.zoomStatus.set(null)
    });
  }

  private toLocalInputValue(date: Date): string {
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }
}
