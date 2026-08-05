import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Classroom, ClassroomStudent } from '../../models';
import { TranslatePipe } from '../../shared/translate.pipe';

@Component({
  selector: 'app-teacher-whatsapp',
  imports: [FormsModule, TranslatePipe],
  templateUrl: './teacher-whatsapp.component.html',
  styleUrls: ['./teacher-panel.css', './teacher-whatsapp.component.css']
})
export class TeacherWhatsAppComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  readonly classrooms = signal<Classroom[]>([]);
  readonly selectedIds = signal<Set<string>>(new Set());
  readonly sending = signal(false);
  readonly error = signal('');
  readonly info = signal('');
  readonly lastGroupShareUrl = signal<string | null>(null);

  classroomId = '';
  message = '';
  includeGroupInviteLink = true;
  dailyReportsEnabled = true;

  readonly selectedClassroom = computed(() =>
    this.classrooms().find((c) => c.id === this.classroomId) ?? null
  );

  readonly students = computed(() => this.selectedClassroom()?.students ?? []);

  constructor() {
    this.api.getClassrooms().subscribe((classrooms) => {
      this.classrooms.set(classrooms);
      if (!this.classroomId && classrooms[0]) {
        this.classroomId = classrooms[0].id;
        this.selectAllWithMobile();
      }
    });
  }

  onClassroomChange(): void {
    this.selectedIds.set(new Set());
    this.selectAllWithMobile();
    this.lastGroupShareUrl.set(null);
    const room = this.selectedClassroom();
    this.dailyReportsEnabled = room?.dailyWhatsAppReportsEnabled !== false;
  }

  saveDailyReportsSetting(): void {
    if (!this.classroomId) return;
    this.api
      .updateClassroomWhatsApp(this.classroomId, {
        dailyWhatsAppReportsEnabled: this.dailyReportsEnabled
      })
      .subscribe({
        next: (room) => {
          this.classrooms.update((list) => list.map((c) => (c.id === room.id ? room : c)));
          this.info.set(this.locale.t('teacher.whatsapp.dailySaved'));
        },
        error: (err) => this.error.set(this.locale.fromApiError(err,'teacher.whatsapp.saveSettingFailed'))
      });
  }

  runDailyDigest(): void {
    this.error.set('');
    this.info.set('');
    this.api.runDailyWhatsAppReports(true).subscribe({
      next: (result) =>
        this.info.set(
          this.locale.t('teacher.whatsapp.dailyDigestResult', {
            sent: result.sentCount,
            failed: result.failedCount,
            skipped: result.skippedCount,
            students: result.studentMessagesAttempted,
            parents: result.parentMessagesAttempted
          })
        ),
      error: (err) => this.error.set(this.locale.fromApiError(err,'teacher.whatsapp.runDigestFailed'))
    });
  }

  selectAllWithMobile(): void {
    const next = new Set<string>();
    for (const student of this.students()) {
      if (student.mobilePhone?.trim()) next.add(student.studentId);
    }
    this.selectedIds.set(next);
  }

  clearSelection(): void {
    this.selectedIds.set(new Set());
  }

  toggleStudent(studentId: string): void {
    const next = new Set(this.selectedIds());
    if (next.has(studentId)) next.delete(studentId);
    else next.add(studentId);
    this.selectedIds.set(next);
  }

  isSelected(studentId: string): boolean {
    return this.selectedIds().has(studentId);
  }

  hasMobile(student: ClassroomStudent): boolean {
    return !!student.mobilePhone?.trim();
  }

  send(): void {
    this.error.set('');
    this.info.set('');
    this.lastGroupShareUrl.set(null);
    if (!this.classroomId) {
      this.error.set(this.locale.t('teacher.whatsapp.selectClassroom'));
      return;
    }
    if (!this.message.trim()) {
      this.error.set(this.locale.t('teacher.whatsapp.enterMessage'));
      return;
    }
    if (this.selectedIds().size === 0) {
      this.error.set(this.locale.t('teacher.whatsapp.selectStudent'));
      return;
    }

    this.sending.set(true);
    this.api
      .sendClassroomWhatsApp(this.classroomId, {
        message: this.message.trim(),
        studentIds: [...this.selectedIds()],
        includeGroupInviteLink: this.includeGroupInviteLink
      })
      .subscribe({
        next: (result) => {
          this.sending.set(false);
          this.info.set(
            this.locale.t('teacher.whatsapp.sendResult', {
              sent: result.sentCount,
              failed: result.failedCount,
              status: result.status
            })
          );
          this.lastGroupShareUrl.set(result.groupShareUrl || null);
        },
        error: (err) => {
          this.sending.set(false);
          this.error.set(this.locale.fromApiError(err,'teacher.whatsapp.sendFailed'));
        }
      });
  }
}
