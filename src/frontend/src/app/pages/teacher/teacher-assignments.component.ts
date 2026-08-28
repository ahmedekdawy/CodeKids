import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Assignment, Classroom, Course, CourseLesson, CourseUnit } from '../../models';
import { formatCourseLabel } from '../../grade.util';
import { TranslatePipe } from '../../shared/translate.pipe';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { SearchableMultiSelectComponent } from '../../shared/searchable-multi-select/searchable-multi-select.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';
import { QuestionImageUploadComponent } from '../../shared/question-image-upload/question-image-upload.component';

interface AssignmentQuestionDraft {
  prompt: string;
  questionType: 'ShortAnswer' | 'MultipleChoice';
  optionA: string;
  optionB: string;
  optionC: string;
  correct: string;
  promptImageMediaAssetId?: string | null;
  promptImageUrl?: string | null;
}

function emptyAssignmentQuestion(
  type: 'ShortAnswer' | 'MultipleChoice' = 'ShortAnswer'
): AssignmentQuestionDraft {
  return { prompt: '', questionType: type, optionA: '', optionB: '', optionC: '', correct: '' };
}

@Component({
  selector: 'app-teacher-assignments',
  imports: [
    PageFeedbackComponent,
    SearchableSelectComponent,
    SearchableMultiSelectComponent,
    FormsModule,
    TranslatePipe,
    QuestionImageUploadComponent
  ],
  templateUrl: './teacher-assignments.component.html',
  styleUrl: './teacher-panel.css'
})
export class TeacherAssignmentsComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  readonly courses = signal<Course[]>([]);
  readonly classrooms = signal<Classroom[]>([]);
  readonly assignments = signal<Assignment[]>([]);
  readonly error = signal('');
  readonly info = signal('');
  readonly generating = signal(false);

  assignmentTitle = '';
  assignmentDescription = '';
  assignmentClassroomId = '';
  assignmentCourseId = '';
  assignmentUnitIds: string[] = [];
  assignmentLessonIds: string[] = [];
  assignmentXp = 25;
  assignmentQuestionCount = 1;
  assignmentType: 'ShortAnswer' | 'MultipleChoice' = 'ShortAnswer';
  questions: AssignmentQuestionDraft[] = [emptyAssignmentQuestion()];

  constructor() {
    this.api.getCourses().subscribe((courses) => {
      this.courses.set(courses);
      this.ensureCourseSelection();
    });
    this.api.getClassrooms().subscribe((classrooms) => {
      this.classrooms.set(classrooms);
      if (!this.assignmentClassroomId && classrooms[0]) this.assignmentClassroomId = classrooms[0].id;
      this.ensureCourseSelection();
    });
    this.reloadAssignments();
  }

  courseLabel(course: Course): string {
    return formatCourseLabel((k, p) => this.locale.t(k, p), course.title, course.grade, 'common.allGrades', course.stageId);
  }

  coursesForClassroom(): Course[] {
    const room = this.classrooms().find((c) => c.id === this.assignmentClassroomId);
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
      this.questions.push(emptyAssignmentQuestion(this.assignmentType));
    }
    if (this.questions.length > count) {
      this.questions = this.questions.slice(0, count);
    }
  }

  onTypeChange(): void {
    for (const question of this.questions) {
      question.questionType = this.assignmentType;
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
            ? draft.questions.map((question) => {
                const type =
                  question.questionType === 'MultipleChoice' ? 'MultipleChoice' : 'ShortAnswer';
                return {
                  prompt: question.prompt,
                  questionType: type as 'ShortAnswer' | 'MultipleChoice',
                  optionA: question.options[0] || '',
                  optionB: question.options[1] || '',
                  optionC: question.options[2] || '',
                  correct:
                    type === 'MultipleChoice'
                      ? question.correctOption || question.correctAnswer
                      : question.correctAnswer || question.correctOption
                };
              })
            : [emptyAssignmentQuestion(this.assignmentType)];
          this.assignmentQuestionCount = this.questions.length;
          this.assignmentType = this.questions[0]?.questionType || this.assignmentType;
          this.info.set(this.locale.t('teacher.ai.generated'));
        },
        error: (err) => {
          this.generating.set(false);
          this.error.set(this.locale.fromApiError(err, 'teacher.ai.generateFailed'));
        }
      });
  }

  createAssignment(): void {
    this.error.set('');
    this.info.set('');
    if (!this.requireScope()) return;
    const questions = this.questions
      .map((question, index) => ({
        prompt: (question.prompt || '').trim(),
        questionType: question.questionType,
        optionA: question.questionType === 'MultipleChoice' ? question.optionA : null,
        optionB: question.questionType === 'MultipleChoice' ? question.optionB : null,
        optionC: question.questionType === 'MultipleChoice' ? question.optionC : null,
        correctAnswer: question.correct,
        points: 1,
        sortOrder: index + 1,
        promptImageMediaAssetId: question.promptImageMediaAssetId || null
      }))
      .filter((question) => question.prompt.length > 0);
    if (!questions.length) {
      this.error.set(this.locale.t('teacher.assignments.question'));
      return;
    }

    this.api
      .createAssignment({
        classroomId: this.assignmentClassroomId,
        title: this.assignmentTitle,
        description: this.assignmentDescription,
        xpReward: this.assignmentXp,
        questions
      })
      .subscribe({
        next: () => {
          this.info.set(this.locale.t('teacher.assignments.created'));
          this.assignmentTitle = '';
          this.questions = [emptyAssignmentQuestion(this.assignmentType)];
          this.assignmentQuestionCount = 1;
          this.reloadAssignments();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.assignments.createFailed'))
      });
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
