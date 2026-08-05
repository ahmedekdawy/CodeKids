import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Assignment, Classroom } from '../../models';
import { TranslatePipe } from '../../shared/translate.pipe';

@Component({
  selector: 'app-teacher-assignments',
  imports: [FormsModule, TranslatePipe],
  templateUrl: './teacher-assignments.component.html',
  styleUrl: './teacher-panel.css'
})
export class TeacherAssignmentsComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  readonly classrooms = signal<Classroom[]>([]);
  readonly assignments = signal<Assignment[]>([]);
  readonly error = signal('');
  readonly info = signal('');

  assignmentTitle = '';
  assignmentDescription = '';
  assignmentClassroomId = '';
  assignmentXp = 25;
  assignmentPrompt = '';
  assignmentType: 'ShortAnswer' | 'MultipleChoice' = 'ShortAnswer';
  assignmentCorrect = '';
  assignmentOptionA = '';
  assignmentOptionB = '';
  assignmentOptionC = '';

  constructor() {
    this.api.getClassrooms().subscribe((classrooms) => {
      this.classrooms.set(classrooms);
      if (!this.assignmentClassroomId && classrooms[0]) this.assignmentClassroomId = classrooms[0].id;
    });
    this.reloadAssignments();
  }

  createAssignment(): void {
    this.error.set('');
    this.info.set('');
    this.api
      .createAssignment({
        classroomId: this.assignmentClassroomId,
        title: this.assignmentTitle,
        description: this.assignmentDescription,
        xpReward: this.assignmentXp,
        questions: [
          {
            prompt: this.assignmentPrompt,
            questionType: this.assignmentType,
            optionA: this.assignmentType === 'MultipleChoice' ? this.assignmentOptionA : null,
            optionB: this.assignmentType === 'MultipleChoice' ? this.assignmentOptionB : null,
            optionC: this.assignmentType === 'MultipleChoice' ? this.assignmentOptionC : null,
            correctAnswer: this.assignmentCorrect,
            points: 1,
            sortOrder: 1
          }
        ]
      })
      .subscribe({
        next: () => {
          this.info.set(this.locale.t('teacher.assignments.created'));
          this.assignmentTitle = '';
          this.assignmentPrompt = '';
          this.reloadAssignments();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.assignments.createFailed'))
      });
  }

  private reloadAssignments(): void {
    this.api.getAssignments().subscribe((assignments) => this.assignments.set(assignments));
  }
}
