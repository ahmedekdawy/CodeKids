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

type ClassroomRow = Classroom & { gradeLabel: string };

@Component({
  selector: 'app-admin-create-classroom',
  imports: [SearchableSelectComponent, FormsModule, IconActionButtonComponent, TranslatePipe],
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

  classroomName = '';
  classroomDescription = '';
  readonly classroomGrade = signal<number | ''>('');
  classroomWhatsAppInvite = '';
  classroomWhatsAppPhones = '';

  editName = '';
  editDescription = '';
  readonly editGrade = signal<number | ''>('');
  editWhatsAppInvite = '';
  editWhatsAppPhones = '';
  private editCourses: ClassroomCourseAssignment[] = [];

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

  createClassroom(): void {
    this.clearStatus();
    this.api
      .createClassroom({
        name: this.classroomName,
        description: this.classroomDescription,
        grade: this.classroomGrade() === '' ? null : Number(this.classroomGrade()),
        courses: [],
        whatsAppGroupInviteUrl: this.classroomWhatsAppInvite,
        whatsAppNotifyPhones: this.classroomWhatsAppPhones
      })
      .subscribe({
        next: () => {
          this.message.set(this.locale.t('admin.classrooms.created'));
          this.classroomName = '';
          this.classroomDescription = '';
          this.classroomGrade.set('');
          this.classroomWhatsAppInvite = '';
          this.classroomWhatsAppPhones = '';
          this.reload();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.classrooms.createFailed'))
      });
  }

  startEdit(room: Classroom): void {
    this.editingId.set(room.id);
    this.editName = room.name;
    this.editDescription = room.description;
    this.editGrade.set(room.grade ?? '');
    this.editWhatsAppInvite = room.whatsAppGroupInviteUrl || '';
    this.editWhatsAppPhones = room.whatsAppNotifyPhones || '';
    const courses = room.courses ?? [];
    this.editCourses = courses.length
      ? courses.map((c) => ({ courseId: c.courseId, teacherId: c.teacherId }))
      : room.courseId
        ? [{ courseId: room.courseId, teacherId: room.teachers?.[0]?.teacherId || '' }]
        : [];
  }

  cancelEdit(): void {
    this.editingId.set(null);
  }

  saveEdit(classroomId: string): void {
    this.clearStatus();
    this.api
      .updateClassroom(classroomId, {
        name: this.editName,
        description: this.editDescription,
        grade: this.editGrade() === '' ? null : Number(this.editGrade()),
        courses: this.editCourses,
        whatsAppGroupInviteUrl: this.editWhatsAppInvite,
        whatsAppNotifyPhones: this.editWhatsAppPhones
      })
      .subscribe({
        next: () => {
          this.message.set(this.locale.t('admin.classrooms.updated'));
          this.editingId.set(null);
          this.reload();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.classrooms.updateFailed'))
      });
  }

  deleteClassroom(room: Classroom): void {
    if (!confirm(this.locale.t('admin.classrooms.confirmDelete', { name: room.name }))) return;
    this.clearStatus();
    this.api.deleteClassroom(room.id).subscribe({
      next: () => {
        this.message.set(this.locale.t('admin.classrooms.deleted'));
        this.reload();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.classrooms.deleteFailed'))
    });
  }

  private clearStatus(): void {
    this.message.set('');
    this.error.set('');
  }
}
