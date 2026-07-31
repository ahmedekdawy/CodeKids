import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LearningApiService } from '../../learning-api.service';
import { Classroom, Course } from '../../models';

@Component({
  selector: 'app-teacher-quizzes',
  imports: [FormsModule],
  templateUrl: './teacher-quizzes.component.html',
  styleUrl: './teacher-panel.css'
})
export class TeacherQuizzesComponent {
  private readonly api = inject(LearningApiService);
  readonly courses = signal<Course[]>([]);
  readonly classrooms = signal<Classroom[]>([]);
  readonly error = signal('');
  readonly info = signal('');

  quizTitle = '';
  quizDescription = '';
  quizXp = 30;
  quizCourseId = '';
  quizClassroomId = '';
  quizPrompt = '';
  quizA = '';
  quizB = '';
  quizC = '';
  quizCorrect = 'A';

  constructor() {
    this.api.getCourses().subscribe((courses) => {
      this.courses.set(courses);
      if (!this.quizCourseId && courses[0]) this.quizCourseId = courses[0].id;
    });
    this.api.getClassrooms().subscribe((classrooms) => {
      this.classrooms.set(classrooms);
      if (!this.quizClassroomId && classrooms[0]) this.quizClassroomId = classrooms[0].id;
    });
  }

  createQuiz(): void {
    this.error.set('');
    this.info.set('');
    this.api
      .createQuiz({
        courseId: this.quizCourseId,
        classroomId: this.quizClassroomId || null,
        title: this.quizTitle,
        description: this.quizDescription,
        xpReward: this.quizXp,
        questions: [
          {
            prompt: this.quizPrompt,
            optionA: this.quizA,
            optionB: this.quizB,
            optionC: this.quizC,
            correctOption: this.quizCorrect,
            sortOrder: 1
          }
        ]
      })
      .subscribe({
        next: () => {
          this.info.set('Quiz created.');
          this.quizTitle = '';
          this.quizPrompt = '';
        },
        error: (err) => this.error.set(err?.error?.message || 'Could not create quiz.')
      });
  }
}
