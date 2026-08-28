import { Component, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { formatCourseLabel } from '../../grade.util';
import { Course, CourseLesson, StudentAskedQuestion } from '../../models';
import { TranslatePipe } from '../translate.pipe';
import { SearchableSelectComponent } from '../searchable-select/searchable-select.component';
import { PageFeedbackComponent } from '../page-feedback/page-feedback.component';
import { IconActionButtonComponent } from '../icon-action-button/icon-action-button.component';
import { paginate, totalPages } from '../../list-query.util';

@Component({
  selector: 'app-asked-questions-board',
  imports: [FormsModule, TranslatePipe, SearchableSelectComponent, PageFeedbackComponent, IconActionButtonComponent],
  templateUrl: './asked-questions-board.component.html',
  styleUrl: './asked-questions-board.component.css'
})
export class AskedQuestionsBoardComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  readonly canAnswer = input(false);

  readonly courses = signal<Course[]>([]);
  readonly items = signal<StudentAskedQuestion[]>([]);
  readonly page = signal(1);
  readonly pageSize = 10;
  readonly error = signal('');
  readonly message = signal('');
  readonly editingAnswerId = signal<string | null>(null);
  readonly editingQuestionId = signal<string | null>(null);
  draftAnswer = '';
  draftQuestion = '';

  filterCourseId = '';
  filterUnitId = '';
  filterLessonId = '';
  filterFromDate = '';
  filterToDate = '';
  filterQuestion = '';

  readonly courseOptions = computed(() => {
    this.locale.lang();
    return this.courses().map((course) => ({
      value: course.id,
      label: formatCourseLabel((k, p) => this.locale.t(k, p), course.title, course.grade, 'common.allGrades', course.stageId)
    }));
  });

  readonly unitOptions = computed(() => {
    const course = this.courses().find((c) => c.id === this.filterCourseId);
    return (course?.units ?? []).map((u) => ({ value: u.id, label: u.title }));
  });

  readonly lessonOptions = computed(() => {
    const course = this.courses().find((c) => c.id === this.filterCourseId);
    const units = course?.units ?? [];
    const source: CourseLesson[] = this.filterUnitId
      ? units.find((u) => u.id === this.filterUnitId)?.lessons ?? []
      : units.flatMap((u) => u.lessons ?? []);
    return source.map((l) => ({ value: l.id, label: l.title }));
  });

  readonly totalPages = computed(() => totalPages(this.items().length, this.pageSize));
  readonly pagedItems = computed(() => paginate(this.items(), this.page(), this.pageSize));

  constructor() {
    this.api.getCourses(true).subscribe({
      next: (courses) => this.courses.set(courses),
      error: (err) => this.error.set(this.locale.fromApiError(err, 'askedQuestions.loadFailed'))
    });
    this.reload();
  }

  onCourseChange(value: string): void {
    this.filterCourseId = value;
    this.filterUnitId = '';
    this.filterLessonId = '';
  }

  onUnitChange(value: string): void {
    this.filterUnitId = value;
    this.filterLessonId = '';
  }

  applyFilters(): void {
    this.reload();
  }

  clearFilters(): void {
    this.filterCourseId = '';
    this.filterUnitId = '';
    this.filterLessonId = '';
    this.filterFromDate = '';
    this.filterToDate = '';
    this.filterQuestion = '';
    this.reload();
  }

  reload(): void {
    this.error.set('');
    this.api
      .listStudentAskedQuestions({
        courseId: this.filterCourseId || undefined,
        unitId: this.filterUnitId || undefined,
        lessonId: this.filterLessonId || undefined,
        fromDate: this.filterFromDate || undefined,
        toDate: this.filterToDate || undefined,
        q: this.filterQuestion.trim() || undefined
      })
      .subscribe({
        next: (items) => {
          this.items.set(items);
          this.page.set(1);
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'askedQuestions.loadFailed'))
      });
  }

  formatWhen(iso: string): string {
    this.locale.lang();
    return new Date(iso).toLocaleString(this.locale.lang());
  }

  startEditAnswer(item: StudentAskedQuestion): void {
    this.editingQuestionId.set(null);
    this.editingAnswerId.set(item.id);
    this.draftAnswer = (item.teacherAnswer || item.aiAnswer || '').trim();
  }

  startEditQuestion(item: StudentAskedQuestion): void {
    this.editingAnswerId.set(null);
    this.editingQuestionId.set(item.id);
    this.draftQuestion = item.question;
  }

  cancelEdit(): void {
    this.editingAnswerId.set(null);
    this.editingQuestionId.set(null);
    this.draftAnswer = '';
    this.draftQuestion = '';
  }

  saveAnswer(item: StudentAskedQuestion): void {
    const text = this.draftAnswer.trim();
    if (!text) {
      this.error.set(this.locale.t('askedQuestions.answerRequired'));
      return;
    }
    this.api.answerStudentAskedQuestion(item.id, text).subscribe({
      next: (updated) => {
        this.items.set(this.items().map((row) => (row.id === updated.id ? updated : row)));
        this.message.set(this.locale.t('askedQuestions.answerSaved'));
        this.error.set('');
        this.cancelEdit();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'askedQuestions.answerFailed'))
    });
  }

  saveQuestion(item: StudentAskedQuestion): void {
    const text = this.draftQuestion.trim();
    if (!text) {
      this.error.set(this.locale.t('studentAsk.questionRequired'));
      return;
    }
    this.api.updateStudentAskedQuestion(item.id, text).subscribe({
      next: (updated) => {
        this.items.set(this.items().map((row) => (row.id === updated.id ? { ...updated, isMine: row.isMine } : row)));
        this.message.set(this.locale.t('askedQuestions.questionSaved'));
        this.error.set('');
        this.cancelEdit();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'askedQuestions.questionFailed'))
    });
  }

  deleteQuestion(item: StudentAskedQuestion): void {
    if (!confirm(this.locale.t('askedQuestions.confirmDelete', { question: item.question }))) return;
    this.api.deleteStudentAskedQuestion(item.id).subscribe({
      next: () => {
        this.items.set(this.items().filter((row) => row.id !== item.id));
        this.page.set(Math.min(this.page(), totalPages(this.items().length, this.pageSize)));
        this.message.set(this.locale.t('askedQuestions.questionDeleted'));
        this.error.set('');
        if (this.editingAnswerId() === item.id || this.editingQuestionId() === item.id) {
          this.cancelEdit();
        }
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'askedQuestions.deleteFailed'))
    });
  }

  goToPage(page: number): void {
    this.page.set(Math.min(Math.max(1, page), this.totalPages()));
  }

  scopeLabel(item: StudentAskedQuestion): string {
    const parts = [item.courseTitle, item.unitTitle, item.lessonTitle].filter((p) => !!p);
    return parts.join(' · ');
  }
}
