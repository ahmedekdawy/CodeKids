import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { BankQuestion, Classroom, Exam, ExamAttempt } from '../../models';
import { TranslatePipe } from '../../shared/translate.pipe';

@Component({
  selector: 'app-teacher-exams',
  imports: [FormsModule, TranslatePipe],
  templateUrl: './teacher-exams.component.html',
  styleUrl: './teacher-panel.css'
})
export class TeacherExamsComponent {
  private readonly locale = inject(LocaleService);
  private readonly api = inject(LearningApiService);
  readonly classrooms = signal<Classroom[]>([]);
  readonly bank = signal<BankQuestion[]>([]);
  readonly exams = signal<Exam[]>([]);
  readonly attempts = signal<ExamAttempt[]>([]);
  readonly selectedIds = signal<Set<string>>(new Set());
  readonly error = signal('');
  readonly info = signal('');

  title = '';
  description = '';
  classroomId = '';
  courseId = '';
  xpReward = 40;
  reviewExamId = '';

  constructor() {
    this.api.getClassrooms().subscribe((classrooms) => {
      this.classrooms.set(classrooms);
      if (!this.classroomId && classrooms[0]) {
        this.classroomId = classrooms[0].id;
        this.courseId = classrooms[0].courseId || '';
      }
      this.reloadBank();
    });
    this.reloadExams();
  }

  onClassroomChange(): void {
    const room = this.classrooms().find((c) => c.id === this.classroomId);
    this.courseId = room?.courseId || '';
    this.reloadBank();
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

  createExam(): void {
    this.error.set('');
    this.info.set('');
    const questionIds = [...this.selectedIds()];
    if (!this.title.trim() || !this.classroomId || questionIds.length === 0) {
      this.error.set(this.locale.t('teacher.exams.required'));
      return;
    }

    this.api
      .createExam({
        classroomId: this.classroomId,
        courseId: this.courseId || null,
        title: this.title.trim(),
        description: this.description.trim() || undefined,
        xpReward: this.xpReward,
        questionIds
      })
      .subscribe({
        next: () => {
          this.info.set(this.locale.t('teacher.exams.created'));
          this.title = '';
          this.description = '';
          this.selectedIds.set(new Set());
          this.reloadExams();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.exams.createFailed'))
      });
  }

  reviewExam(exam: Exam): void {
    this.reviewExamId = exam.id;
    this.api.getExamAttempts(exam.id).subscribe({
      next: (attempts) => this.attempts.set(attempts),
      error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.exams.loadAttemptsFailed'))
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
}
