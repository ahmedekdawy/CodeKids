import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../auth.service';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Assignment, Classroom, ClassroomCourse, Course, CourseLesson, CourseUnit } from '../../models';
import { courseMatchesClassroomGrade, formatCourseLabel } from '../../grade.util';
import { TranslatePipe } from '../../shared/translate.pipe';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { SearchableMultiSelectComponent } from '../../shared/searchable-multi-select/searchable-multi-select.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import { QuestionDraftEditorComponent } from '../../shared/question-draft-editor/question-draft-editor.component';
import { QuestionDraft } from '../../shared/question-draft/question-draft.model';
import {
  draftFromAssignmentQuestion,
  draftFromGenerated,
  emptyQuestionDraft,
  toQuestionPayload,
  validateQuestionDraft
} from '../../shared/question-draft/question-draft.util';

@Component({
  selector: 'app-teacher-assignments',
  imports: [
    PageFeedbackComponent,
    SearchableSelectComponent,
    SearchableMultiSelectComponent,
    FormsModule,
    TranslatePipe,
    QuestionDraftEditorComponent,
    IconActionButtonComponent
  ],
  templateUrl: './teacher-assignments.component.html',
  styleUrl: './teacher-panel.css'
})
export class TeacherAssignmentsComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  private readonly auth = inject(AuthService);
  readonly courses = signal<Course[]>([]);
  readonly classrooms = signal<Classroom[]>([]);
  readonly assignments = signal<Assignment[]>([]);
  readonly error = signal('');
  readonly info = signal('');
  readonly generating = signal(false);
  readonly publishingId = signal<string | null>(null);

  assignmentTitle = '';
  assignmentDescription = '';
  assignmentClassroomId = '';
  assignmentCourseId = '';
  assignmentUnitIds: string[] = [];
  assignmentLessonIds: string[] = [];
  assignmentXp = 25;
  assignmentIsPublished = false;
  assignmentQuestionCount = 1;
  assignmentType: 'ShortAnswer' | 'MultipleChoice' = 'ShortAnswer';
  questions: QuestionDraft[] = [emptyQuestionDraft('ShortAnswer')];
  editingAssignmentId: string | null = null;
  editingDueAtUtc: string | null = null;

  constructor() {
    this.api.getCourses().subscribe({
      next: (courses) => {
        this.courses.set(courses ?? []);
        this.ensureCourseSelection();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.ai.needScope'))
    });
    this.api.getClassrooms().subscribe({
      next: (classrooms) => {
        this.classrooms.set(classrooms ?? []);
        if (!this.assignmentClassroomId && classrooms[0]) this.assignmentClassroomId = classrooms[0].id;
        this.ensureCourseSelection();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.ai.needClassroom'))
    });
    this.reloadAssignments();
  }

  courseLabel(course: Course): string {
    return formatCourseLabel((k, p) => this.locale.t(k, p), course.title, course.grade, 'common.allGrades', course.stageId);
  }

  coursesForClassroom(): Course[] {
    if (!this.assignmentClassroomId) return [];
    const room = this.classrooms().find((c) => c.id === this.assignmentClassroomId);
    if (!room) return [];

    const teacherId = this.auth.user()?.id;
    const links = (room.courses ?? []).filter(
      (link) =>
        !!link.courseId &&
        (!teacherId || !link.teacherId || link.teacherId === teacherId)
    );

    const ids = new Set(
      [
        ...links.map((link) => link.courseId),
        ...(links.length ? [] : [room.courseId])
      ]
        .filter((id): id is string => !!id)
        .map((id) => id.toLowerCase())
    );

    const all = this.courses();
    const sortCourses = (list: Course[]) =>
      [...list].sort(
        (a, b) => a.title.localeCompare(b.title) || (a.grade ?? 999) - (b.grade ?? 999)
      );

    if (!ids.size) {
      return sortCourses(
        all.filter((course) =>
          courseMatchesClassroomGrade(course.grade, room.grade, course.stageId)
        )
      );
    }

    const matched = all.filter((course) => ids.has(course.id.toLowerCase()));
    if (matched.length) return sortCourses(matched);

    // Classroom has course links, but they were missing from getCourses — still show them.
    return sortCourses(links.map((link) => this.toCourseOption(link, all)));
  }

  private toCourseOption(link: ClassroomCourse, loaded: Course[]): Course {
    const existing = loaded.find((course) => course.id.toLowerCase() === link.courseId.toLowerCase());
    if (existing) return existing;
    return {
      id: link.courseId,
      title: link.courseTitle || link.courseId,
      theme: '',
      description: '',
      ageMin: 0,
      ageMax: 0,
      grade: link.courseGrade ?? null,
      stageId: link.courseStageId ?? null,
      schoolType: link.courseSchoolType ?? 'All',
      sortOrder: 0,
      lessons: [],
      quizzes: [],
      units: []
    };
  }

  unitsForCourse(): CourseUnit[] {
    const units = [...(this.courses().find((c) => c.id === this.assignmentCourseId)?.units ?? [])];
    return units.sort((a, b) => a.sortOrder - b.sortOrder || a.title.localeCompare(b.title));
  }

  lessonsForUnits(): CourseLesson[] {
    const course = this.courses().find((c) => c.id === this.assignmentCourseId);
    if (!course || !this.assignmentUnitIds.length) return [];
    const selected = new Set(this.assignmentUnitIds);
    const lessons = (course.units ?? [])
      .filter((u) => selected.has(u.id))
      .flatMap((u) => u.lessons ?? []);
    const extra = (course.lessons ?? []).filter((l) => l.unitId && selected.has(l.unitId));
    const byId = new Map<string, CourseLesson>();
    for (const lesson of [...lessons, ...extra]) byId.set(lesson.id, lesson);
    return [...byId.values()].sort((a, b) => a.sortOrder - b.sortOrder || a.title.localeCompare(b.title));
  }

  onClassroomChange(): void {
    this.assignmentCourseId = '';
    this.onCourseChange();
    this.ensureCourseSelection();
  }

  onCourseChange(): void {
    this.assignmentUnitIds = [];
    this.assignmentLessonIds = [];
  }

  onUnitsChange(): void {
    const allowed = new Set(this.lessonsForUnits().map((l) => l.id));
    this.assignmentLessonIds = this.assignmentLessonIds.filter((id) => allowed.has(id));
  }

  private ensureCourseSelection(): void {
    const options = this.coursesForClassroom();
    if (!options.length) {
      this.assignmentCourseId = '';
      this.onCourseChange();
      return;
    }
    if (!this.assignmentCourseId || !options.some((c) => c.id === this.assignmentCourseId)) {
      this.assignmentCourseId = options[0].id;
      this.onCourseChange();
    }
  }

  private requireScope(): boolean {
    if (!this.assignmentCourseId) {
      this.error.set(this.locale.t('teacher.ai.needScope'));
      return false;
    }
    return true;
  }

  onQuestionCountChange(): void {
    const count = this.clampQuestionCount(this.assignmentQuestionCount, 1);
    this.assignmentQuestionCount = count;
    while (this.questions.length < count) {
      this.questions.push(emptyQuestionDraft(this.assignmentType));
    }
    if (this.questions.length > count) {
      this.questions = this.questions.slice(0, count);
    }
  }

  generate(): void {
    this.error.set('');
    this.info.set('');
    if (!this.assignmentClassroomId) {
      this.error.set(this.locale.t('teacher.ai.needClassroom'));
      return;
    }
    if (!this.requireScope()) return;

    this.generating.set(true);
    this.api
      .generateAssessment({
        kind: 'Assignment',
        classroomId: this.assignmentClassroomId,
        courseId: this.assignmentCourseId,
        unitIds: this.assignmentUnitIds,
        lessonIds: this.assignmentLessonIds,
        questionCount: this.clampQuestionCount(this.assignmentQuestionCount, 1),
        questionType: this.assignmentType,
        language: this.locale.lang()
      })
      .subscribe({
        next: (draft) => {
          this.generating.set(false);
          this.assignmentTitle = draft.title;
          this.assignmentDescription = draft.description;
          this.questions = draft.questions.length
            ? draft.questions.map((question) => draftFromGenerated(question))
            : [emptyQuestionDraft(this.assignmentType)];
          this.assignmentQuestionCount = this.questions.length;
          this.info.set(this.locale.t('teacher.ai.generated'));
        },
        error: (err) => {
          this.generating.set(false);
          this.error.set(this.locale.fromApiError(err, 'teacher.ai.generateFailed'));
        }
      });
  }

  createAssignment(): void {
    this.saveAssignment();
  }

  startEdit(assignment: Assignment): void {
    this.error.set('');
    this.info.set('');
    this.editingAssignmentId = assignment.id;
    this.editingDueAtUtc = assignment.dueAtUtc ?? null;
    this.assignmentTitle = assignment.title;
    this.assignmentDescription = assignment.description;
    this.assignmentClassroomId = assignment.classroomId;
    this.assignmentXp = assignment.xpReward;
    this.assignmentIsPublished = assignment.isPublished;
    this.questions = assignment.questions.length
      ? assignment.questions.map((question) => draftFromAssignmentQuestion(question))
      : [emptyQuestionDraft(this.assignmentType)];
    this.assignmentQuestionCount = this.questions.length;
    this.onClassroomChange();
  }

  cancelEdit(): void {
    this.editingAssignmentId = null;
    this.editingDueAtUtc = null;
    this.assignmentTitle = '';
    this.assignmentDescription = '';
    this.assignmentIsPublished = false;
    this.questions = [emptyQuestionDraft(this.assignmentType)];
    this.assignmentQuestionCount = 1;
    this.error.set('');
    this.info.set('');
  }

  deleteAssignment(assignment: Assignment): void {
    if (!confirm(this.locale.t('teacher.assignments.confirmDelete', { title: assignment.title }))) {
      return;
    }

    this.error.set('');
    this.info.set('');
    this.api.deleteAssignment(assignment.id).subscribe({
      next: () => {
        if (this.editingAssignmentId === assignment.id) {
          this.cancelEdit();
        }
        this.info.set(this.locale.t('teacher.assignments.deleted'));
        this.reloadAssignments();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.assignments.deleteFailed'))
    });
  }

  publishAssignment(assignment: Assignment): void {
    if (assignment.isPublished || this.publishingId()) {
      return;
    }

    this.error.set('');
    this.info.set('');
    this.publishingId.set(assignment.id);
    this.api.publishAssignment(assignment.id).subscribe({
      next: () => {
        this.publishingId.set(null);
        this.info.set(this.locale.t('teacher.assessments.publishedSuccess'));
        this.reloadAssignments();
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

  private saveAssignment(): void {
    this.error.set('');
    this.info.set('');
    if (!this.requireScope()) return;
    const questions = this.buildQuestionPayload();
    if (questions === null) {
      return;
    }
    if (!questions.length) {
      this.error.set(this.locale.t('teacher.assignments.question'));
      return;
    }

    const payload = {
      classroomId: this.assignmentClassroomId,
      title: this.assignmentTitle,
      description: this.assignmentDescription,
      dueAtUtc: this.editingDueAtUtc,
      xpReward: this.assignmentXp,
      isPublished: this.assignmentIsPublished,
      questions
    };

    const editingId = this.editingAssignmentId;
    const request = editingId
      ? this.api.updateAssignment(editingId, payload)
      : this.api.createAssignment(payload);

    request.subscribe({
      next: () => {
        this.cancelEdit();
        this.info.set(
          this.locale.t(editingId ? 'teacher.assignments.updated' : 'teacher.assignments.created')
        );
        this.reloadAssignments();
      },
      error: (err) =>
        this.error.set(
          this.locale.fromApiError(
            err,
            editingId ? 'teacher.assignments.updateFailed' : 'teacher.assignments.createFailed'
          )
        )
    });
  }

  private buildQuestionPayload() {
    const questions = [];
    for (let index = 0; index < this.questions.length; index++) {
      const errorKey = validateQuestionDraft(this.questions[index], index + 1);
      if (errorKey) {
        this.error.set(this.locale.t(errorKey));
        return null;
      }
      questions.push(toQuestionPayload(this.questions[index], index + 1));
    }
    return questions.filter((question) => question.prompt.length > 0 || question.questionType === 'Paragraph');
  }

  private reloadAssignments(): void {
    this.api.getAssignments().subscribe((assignments) => this.assignments.set(assignments));
  }

  private clampQuestionCount(value: number, fallback: number): number {
    const count = Number(value);
    if (!Number.isFinite(count) || count < 1) return fallback;
    return Math.min(12, Math.floor(count));
  }
}
