import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { BankQuestion, Classroom, Course, CourseLesson, CourseUnit, Exam, ExamAttempt } from '../../models';
import { formatCourseLabel } from '../../grade.util';
import { TranslatePipe } from '../../shared/translate.pipe';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { SearchableMultiSelectComponent } from '../../shared/searchable-multi-select/searchable-multi-select.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';
import { QuestionImageDisplayComponent } from '../../shared/question-image-display/question-image-display.component';
import { QuestionImageUploadComponent } from '../../shared/question-image-upload/question-image-upload.component';
import { SafeHtmlPipe } from '../../shared/safe-html.pipe';

interface AttemptDraft {
  feedback: string;
  feedbackImageMediaAssetId: string | null;
  feedbackImageUrl: string | null;
}

@Component({
  selector: 'app-teacher-exams',
  imports: [
    PageFeedbackComponent,
    SearchableSelectComponent,
    SearchableMultiSelectComponent,
    FormsModule,
    TranslatePipe,
    QuestionImageDisplayComponent,
    QuestionImageUploadComponent,
    SafeHtmlPipe
  ],
  templateUrl: './teacher-exams.component.html',
  styleUrl: './teacher-panel.css'
})
export class TeacherExamsComponent {
  private readonly locale = inject(LocaleService);
  private readonly api = inject(LearningApiService);
  readonly courses = signal<Course[]>([]);
  readonly classrooms = signal<Classroom[]>([]);
  readonly bank = signal<BankQuestion[]>([]);
  readonly exams = signal<Exam[]>([]);
  readonly attempts = signal<ExamAttempt[]>([]);
  readonly selectedIds = signal<Set<string>>(new Set());
  readonly error = signal('');
  readonly info = signal('');
  readonly generating = signal(false);
  readonly publishingId = signal<string | null>(null);

  title = '';
  description = '';
  classroomId = '';
  courseId = '';
  unitIds: string[] = [];
  lessonIds: string[] = [];
  xpReward = 40;
  /** 0 keeps the exam untimed. */
  durationMinutes = 0;
  isPublished = false;
  questionCount = 6;
  reviewExamId = '';
  private readonly attemptDrafts = signal<Record<string, AttemptDraft>>({});

  constructor() {
    this.api.getCourses().subscribe((courses) => {
      this.courses.set(courses);
      this.ensureCourseSelection();
      this.reloadBank();
    });
    this.api.getClassrooms().subscribe((classrooms) => {
      this.classrooms.set(classrooms);
      if (!this.classroomId && classrooms[0]) this.classroomId = classrooms[0].id;
      this.ensureCourseSelection();
      this.reloadBank();
    });
    this.reloadExams();
  }

  courseLabel(course: Course): string {
    return formatCourseLabel((k, p) => this.locale.t(k, p), course.title, course.grade, 'common.allGrades', course.stageId);
  }

  coursesForClassroom(): Course[] {
    const room = this.classrooms().find((c) => c.id === this.classroomId);
    const ids = new Set(
      [
        ...(room?.courses?.map((c) => c.courseId) ?? []),
        room?.courseId
      ].filter((id): id is string => !!id)
    );
    const all = this.courses();
    if (!ids.size) return all;
    const matched = all.filter((c) => ids.has(c.id));
    return matched.length ? matched : all;
  }

  unitsForCourse(): CourseUnit[] {
    const units = [...(this.courses().find((c) => c.id === this.courseId)?.units ?? [])];
    return units.sort((a, b) => a.sortOrder - b.sortOrder || a.title.localeCompare(b.title));
  }

  lessonsForUnits(): CourseLesson[] {
    const course = this.courses().find((c) => c.id === this.courseId);
    if (!course || !this.unitIds.length) return [];
    const selected = new Set(this.unitIds);
    const lessons = (course.units ?? [])
      .filter((u) => selected.has(u.id))
      .flatMap((u) => u.lessons ?? []);
    const extra = (course.lessons ?? []).filter((l) => l.unitId && selected.has(l.unitId));
    const byId = new Map<string, CourseLesson>();
    for (const lesson of [...lessons, ...extra]) byId.set(lesson.id, lesson);
    return [...byId.values()].sort((a, b) => a.sortOrder - b.sortOrder || a.title.localeCompare(b.title));
  }

  visibleBank(): BankQuestion[] {
    const questions = this.bank();
    if (this.lessonIds.length) {
      const ids = new Set(this.lessonIds);
      return questions.filter((q) => !q.lessonId || ids.has(q.lessonId));
    }
    if (this.unitIds.length) {
      const ids = new Set(this.lessonsForUnits().map((l) => l.id));
      return questions.filter((q) => !q.lessonId || ids.has(q.lessonId));
    }
    return questions;
  }

  onClassroomChange(): void {
    this.ensureCourseSelection();
    this.reloadBank();
  }

  onCourseChange(): void {
    this.unitIds = [];
    this.lessonIds = [];
    this.reloadBank();
  }

  onUnitsChange(): void {
    const allowed = new Set(this.lessonsForUnits().map((l) => l.id));
    this.lessonIds = this.lessonIds.filter((id) => allowed.has(id));
  }

  private ensureCourseSelection(): void {
    const options = this.coursesForClassroom();
    if (!options.length) {
      this.courseId = '';
      this.unitIds = [];
      this.lessonIds = [];
      return;
    }
    if (!this.courseId || !options.some((c) => c.id === this.courseId)) {
      this.courseId = options[0].id;
      this.unitIds = [];
      this.lessonIds = [];
    }
  }

  private requireScope(): boolean {
    if (!this.courseId) {
      this.error.set(this.locale.t('teacher.ai.needScope'));
      return false;
    }
    return true;
  }

  toggleQuestion(id: string): void {
    const next = new Set(this.selectedIds());
    if (next.has(id)) next.delete(id);
    else next.add(id);
    this.selectedIds.set(next);
  }

  isSelected(id: string): boolean {
    return this.selectedIds().has(id);
  }

  generate(): void {
    this.error.set('');
    this.info.set('');
    if (!this.classroomId) {
      this.error.set(this.locale.t('teacher.ai.needClassroom'));
      return;
    }
    if (!this.requireScope()) return;

    this.generating.set(true);
    this.api
      .generateAssessment({
        kind: 'Exam',
        classroomId: this.classroomId,
        courseId: this.courseId,
        unitIds: this.unitIds,
        lessonIds: this.lessonIds,
        questionCount: this.clampQuestionCount(this.questionCount, 6),
        language: this.locale.lang()
      })
      .subscribe({
        next: (draft) => {
          this.generating.set(false);
          this.title = draft.title;
          this.description = draft.description;
          this.selectedIds.set(new Set(draft.questionIds || []));
          this.questionCount = draft.questionIds?.length || this.questionCount;
          this.reloadBank();
          this.info.set(this.locale.t('teacher.exams.aiGenerated'));
        },
        error: (err) => {
          this.generating.set(false);
          this.error.set(this.locale.fromApiError(err, 'teacher.ai.generateFailed'));
        }
      });
  }

  createExam(): void {
    this.error.set('');
    this.info.set('');
    const questionIds = [...this.selectedIds()];
    if (!this.title.trim() || !this.classroomId || !this.courseId || questionIds.length === 0) {
      this.error.set(this.locale.t('teacher.exams.required'));
      return;
    }

    this.api
      .createExam({
        classroomId: this.classroomId,
        courseId: this.courseId,
        title: this.title.trim(),
        description: this.description.trim() || undefined,
        xpReward: this.xpReward,
        durationMinutes: this.durationMinutes > 0 ? this.durationMinutes : null,
        isPublished: this.isPublished,
        questionIds
      })
      .subscribe({
        next: () => {
          this.info.set(this.locale.t('teacher.exams.created'));
          this.title = '';
          this.description = '';
          this.isPublished = false;
          this.selectedIds.set(new Set());
          this.reloadExams();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.exams.createFailed'))
      });
  }

  publishExam(exam: Exam): void {
    if (exam.isPublished || this.publishingId()) {
      return;
    }

    this.error.set('');
    this.info.set('');
    this.publishingId.set(exam.id);
    this.api.publishExam(exam.id).subscribe({
      next: () => {
        this.publishingId.set(null);
        this.info.set(this.locale.t('teacher.assessments.publishedSuccess'));
        this.reloadExams();
      },
      error: (err) => {
        this.publishingId.set(null);
        this.error.set(this.locale.fromApiError(err, 'teacher.assessments.publishFailed'));
      }
    });
  }

  isPublishing(id: string): boolean {
    return this.publishingId() === id;
  }

  reviewExam(exam: Exam): void {
    this.reviewExamId = exam.id;
    this.api.getExamAttempts(exam.id).subscribe({
      next: (attempts) => {
        this.attempts.set(attempts);
        const drafts: Record<string, AttemptDraft> = {};
        for (const attempt of attempts) {
          drafts[attempt.id] = {
            feedback: attempt.teacherFeedback || '',
            feedbackImageMediaAssetId: null,
            feedbackImageUrl: attempt.feedbackImageUrl || null
          };
        }
        this.attemptDrafts.set(drafts);
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.exams.loadAttemptsFailed'))
    });
  }

  attemptDraftFor(attemptId: string): AttemptDraft {
    return this.attemptDrafts()[attemptId] || { feedback: '', feedbackImageMediaAssetId: null, feedbackImageUrl: null };
  }

  setAttemptFeedback(attemptId: string, feedback: string): void {
    this.attemptDrafts.update((current) => ({
      ...current,
      [attemptId]: { ...this.attemptDraftFor(attemptId), feedback }
    }));
  }

  setAttemptFeedbackImage(attemptId: string, mediaAssetId: string | null, imageUrl: string | null): void {
    this.attemptDrafts.update((current) => ({
      ...current,
      [attemptId]: {
        ...this.attemptDraftFor(attemptId),
        feedbackImageMediaAssetId: mediaAssetId,
        feedbackImageUrl: imageUrl
      }
    }));
  }

  markExamAnswer(attemptId: string, questionId: string, correct: boolean): void {
    this.attempts.update((list) =>
      list.map((attempt) => {
        if (attempt.id !== attemptId) return attempt;
        return {
          ...attempt,
          answers: attempt.answers.map((a) =>
            a.questionId === questionId
              ? { ...a, isCorrect: correct, pointsAwarded: correct ? a.points : 0 }
              : a
          )
        };
      })
    );
  }

  setExamPoints(attemptId: string, questionId: string, points: number): void {
    this.attempts.update((list) =>
      list.map((attempt) => {
        if (attempt.id !== attemptId) return attempt;
        return {
          ...attempt,
          answers: attempt.answers.map((a) => {
            if (a.questionId !== questionId) return a;
            const awarded = Math.max(0, Math.min(a.points, points));
            return { ...a, pointsAwarded: awarded, isCorrect: awarded >= a.points };
          })
        };
      })
    );
  }

  gradeAttempt(attempt: ExamAttempt): void {
    const draft = this.attemptDraftFor(attempt.id);
    this.api
      .gradeExamAttempt({
        attemptId: attempt.id,
        teacherFeedback: draft.feedback,
        feedbackImageMediaAssetId: draft.feedbackImageMediaAssetId,
        answers: attempt.answers.map((a) => ({
          questionId: a.questionId,
          isCorrect: a.isCorrect ?? false,
          pointsAwarded: a.pointsAwarded ?? (a.isCorrect ? a.points : 0)
        }))
      })
      .subscribe({
        next: () => {
          this.info.set(this.locale.t('teacher.review.graded'));
          const exam = this.exams().find((e) => e.id === this.reviewExamId);
          if (exam) this.reviewExam(exam);
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.review.gradeFailed'))
      });
  }

  private reloadBank(): void {
    this.api.getBankQuestions(this.courseId || undefined).subscribe((questions) => this.bank.set(questions));
  }

  private reloadExams(): void {
    this.api.getExams().subscribe((exams) => this.exams.set(exams));
  }

  questionTypeLabel(type: string): string {
    const key = `qtype.${type.charAt(0).toLowerCase()}${type.slice(1)}`;
    return this.locale.t(key);
  }

  private clampQuestionCount(value: number, fallback: number): number {
    const count = Number(value);
    if (!Number.isFinite(count) || count < 1) return fallback;
    return Math.min(12, Math.floor(count));
  }
}
