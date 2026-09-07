import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Classroom, ClassroomCourseAssignment } from '../../models';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { SortDir, nextSort, sortBy } from '../../sort.util';
import { GRADE_CODES, formatGradeLabel } from '../../grade.util';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';
import { ClassroomZoomLinksEditorComponent } from '../../shared/classroom-zoom-links/classroom-zoom-links-editor.component';
import {
  ClassroomZoomLinkDraft,
  cloneZoomLinks,
  normalizeZoomLinks
} from '../../shared/classroom-zoom-links/classroom-zoom-links.util';

type ClassroomRow = Classroom & { gradeLabel: string };

@Component({
  selector: 'app-admin-create-classroom',
  imports: [
    PageFeedbackComponent,
    SearchableSelectComponent,
    ClassroomZoomLinksEditorComponent,
    FormsModule,
    IconActionButtonComponent,
    TranslatePipe
  ],
  templateUrl: './admin-create-classroom.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminCreateClassroomComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  readonly classrooms = signal<Classroom[]>([]);
  readonly message = signal('');
  readonly error = signal('');
  readonly sortKey = signal('name');
  readonly sortDir = signal<SortDir>('asc');
  readonly editingId = signal<string | null>(null);

  readonly grades = GRADE_CODES;

  formName = '';
  formDescription = '';
  readonly formGrade = signal<number | ''>('');
  formWhatsAppInvite = '';
  formZoomLinks: ClassroomZoomLinkDraft[] = [];
  formWhatsAppPhones = '';
  private formCourses: ClassroomCourseAssignment[] = [];

  readonly sortedClassrooms = computed(() => {
    this.locale.lang();
    const rows: ClassroomRow[] = this.classrooms().map((room) => ({
      ...room,
      gradeLabel: this.gradeLabel(room.grade)
    }));
    return sortBy(rows, this.sortKey(), this.sortDir());
  });

  constructor() {
    this.reload();
  }

  reload(): void {
    this.api.getClassrooms().subscribe((classrooms) => this.classrooms.set(classrooms));
  }

  setSort(key: string): void {
    this.sortDir.set(nextSort(this.sortKey(), key, this.sortDir()));
    this.sortKey.set(key);
  }

  sortMark(key: string): string {
    if (this.sortKey() !== key) return '';
    return this.sortDir() === 'asc' ? '↑' : '↓';
  }

  gradeLabel(grade: number | null | undefined): string {
    if (grade == null) return this.locale.t('common.emDash');
    return formatGradeLabel((k, p) => this.locale.t(k, p), grade);
  }

  saveForm(): void {
    this.clearStatus();
    const payload = {
      name: this.formName,
      description: this.formDescription,
      grade: this.formGrade() === '' ? null : Number(this.formGrade()),
      whatsAppGroupInviteUrl: this.formWhatsAppInvite,
      zoomLinks: normalizeZoomLinks(this.formZoomLinks),
      whatsAppNotifyPhones: this.formWhatsAppPhones
    };

    const classroomId = this.editingId();
    if (classroomId) {
      this.api
        .updateClassroom(classroomId, { ...payload, courses: this.formCourses })
        .subscribe({
          next: () => {
            this.message.set(this.locale.t('admin.classrooms.updated'));
            this.resetForm();
            this.reload();
          },
          error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.classrooms.updateFailed'))
        });
      return;
    }

    this.api
      .createClassroom({ ...payload, courses: [] })
      .subscribe({
        next: () => {
          this.message.set(this.locale.t('admin.classrooms.created'));
          this.resetForm();
          this.reload();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.classrooms.createFailed'))
      });
  }

  startEdit(room: Classroom): void {
    this.editingId.set(room.id);
    this.formName = room.name;
    this.formDescription = room.description;
    this.formGrade.set(room.grade ?? '');
    this.formWhatsAppInvite = room.whatsAppGroupInviteUrl || '';
    this.formZoomLinks = cloneZoomLinks(room.zoomLinks);
    this.formWhatsAppPhones = room.whatsAppNotifyPhones || '';
    const courses = room.courses ?? [];
    this.formCourses = courses.length
      ? courses.map((c) => ({ courseId: c.courseId, teacherId: c.teacherId }))
      : room.courseId
        ? [{ courseId: room.courseId, teacherId: room.teachers?.[0]?.teacherId || '' }]
        : [];
  }

  cancelEdit(): void {
    this.resetForm();
  }

  deleteClassroom(room: Classroom): void {
    if (!confirm(this.locale.t('admin.classrooms.confirmDelete', { name: room.name }))) return;
    this.clearStatus();
    this.api.deleteClassroom(room.id).subscribe({
      next: () => {
        if (this.editingId() === room.id) {
          this.resetForm();
        }
        this.message.set(this.locale.t('admin.classrooms.deleted'));
        this.reload();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.classrooms.deleteFailed'))
    });
  }

  private resetForm(): void {
    this.editingId.set(null);
    this.formName = '';
    this.formDescription = '';
    this.formGrade.set('');
    this.formWhatsAppInvite = '';
    this.formZoomLinks = [];
    this.formWhatsAppPhones = '';
    this.formCourses = [];
  }

  private clearStatus(): void {
    this.message.set('');
    this.error.set('');
  }
}
