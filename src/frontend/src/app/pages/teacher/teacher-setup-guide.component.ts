import { Component, inject, signal } from '@angular/core';
import { catchError, forkJoin, of } from 'rxjs';
import { LearningApiService } from '../../learning-api.service';
import { SetupGuideComponent, SetupGuideStep } from '../../shared/setup-guide/setup-guide.component';

@Component({
  selector: 'app-teacher-setup-guide',
  imports: [SetupGuideComponent],
  template: `<app-setup-guide prefix="teacher.guide" [steps]="steps" [counts]="counts()" />`
})
export class TeacherSetupGuideComponent {
  private readonly api = inject(LearningApiService);

  readonly steps: SetupGuideStep[] = [
    { id: 'students', path: '/teacher/students' },
    { id: 'video', path: '/teacher/videos' },
    { id: 'material', path: '/teacher/materials' },
    { id: 'assistant', path: '/teacher/smart-study-assistant', visitOnly: true },
    { id: 'question', path: '/teacher/question-bank' },
    { id: 'exam', path: '/teacher/exams' },
    { id: 'assignment', path: '/teacher/assignments' },
    { id: 'review', path: '/teacher/review', visitOnly: true },
    { id: 'weekly', path: '/teacher/weekly-reports' }
  ];

  readonly counts = signal<Record<string, number> | null>(null);

  constructor() {
    // Each list fails on its own so one broken endpoint doesn't blank the whole guide.
    forkJoin({
      classrooms: this.api.getClassrooms().pipe(catchError(() => of([]))),
      videos: this.api.getVideoLibrary().pipe(catchError(() => of(null))),
      materials: this.api.getLearningMaterials().pipe(catchError(() => of([]))),
      questions: this.api.getBankQuestions().pipe(catchError(() => of([]))),
      exams: this.api.getExams().pipe(catchError(() => of([]))),
      assignments: this.api.getAssignments().pipe(catchError(() => of([]))),
      weekly: this.api.listWeeklyReports().pipe(catchError(() => of([])))
    }).subscribe(({ classrooms, videos, materials, questions, exams, assignments, weekly }) =>
      this.counts.set({
        students: new Set(classrooms.flatMap((c) => (c.students ?? []).map((s) => s.studentId))).size,
        video: (videos?.lessonVideos?.length ?? 0) + (videos?.courseVideos?.length ?? 0),
        material: materials.length,
        question: questions.length,
        exam: exams.length,
        assignment: assignments.length,
        weekly: weekly.length
      })
    );
  }
}
