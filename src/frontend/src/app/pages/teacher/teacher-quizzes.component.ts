import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { Classroom, Course } from '../../models';
import { formatCourseLabel } from '../../grade.util';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';

interface OptionDraft {
  text: string;
}

@Component({
  selector: 'app-teacher-quizzes',
  imports: [SearchableSelectComponent, FormsModule, IconActionButtonComponent, TranslatePipe],
  templateUrl: './teacher-quizzes.component.html',
  styleUrls: ['./teacher-panel.css', './teacher-quizzes.component.css']
})
export class TeacherQuizzesComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
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
  options: OptionDraft[] = [{ text: '' }, { text: '' }];
  quizCorrect = '';

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

  courseLabel(course: Course): string {
    return formatCourseLabel((k, p) => this.locale.t(k, p), course.title, course.grade);
  }

  optionLabel(index: number): string {
    return String.fromCharCode(65 + index);
  }

  filledOptions(): { key: string; text: string }[] {
    return this.options
      .map((o, i) => ({ key: this.optionLabel(i), text: (o.text || '').trim() }))
      .filter((o) => o.text.length > 0);
  }

  addOption(): void {
    if (this.options.length >= 26) return;
    this.options.push({ text: '' });
  }

  removeOption(index: number): void {
    if (this.options.length <= 2) return;
    this.options.splice(index, 1);
    this.onOptionTextChange();
  }

  onOptionTextChange(): void {
    const keys = new Set(this.filledOptions().map((o) => o.key));
    if (this.quizCorrect && !keys.has(this.quizCorrect)) this.quizCorrect = '';
  }

  createQuiz(): void {
    this.error.set('');
    this.info.set('');
    const filled = this.filledOptions();
    if (filled.length < 2) {
      this.error.set(this.locale.t('teacher.quizzes.minOptions'));
      return;
    }
    if (!this.quizCorrect) {
      this.error.set(this.locale.t('teacher.quizzes.selectCorrect'));
      return;
    }

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
            options: filled.map((o) => o.text),
            correctOption: this.quizCorrect,
            sortOrder: 1
          }
        ]
      })
      .subscribe({
        next: () => {
          this.info.set(this.locale.t('teacher.quizzes.created'));
          this.quizTitle = '';
          this.quizPrompt = '';
          this.options = [{ text: '' }, { text: '' }];
          this.quizCorrect = '';
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'teacher.quizzes.createFailed'))
      });
  }
}
