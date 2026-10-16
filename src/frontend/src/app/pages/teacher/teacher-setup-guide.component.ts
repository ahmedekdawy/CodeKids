import { Component, inject, signal } from '@angular/core';
import { catchError, forkJoin, of } from 'rxjs';
import { AuthService } from '../../auth.service';
import { LearningApiService } from '../../learning-api.service';
import { Classroom } from '../../models';
import { SetupGuideComponent, SetupGuideStep } from '../../shared/setup-guide/setup-guide.component';

/** What a teacher does once an admin has set the school up for them. */
const TEACHING_STEPS: SetupGuideStep[] = [
  { id: 'video', path: '/teacher/videos' },
  { id: 'material', path: '/teacher/materials' },
  { id: 'assistant', path: '/teacher/smart-study-assistant', visitOnly: true },
  { id: 'question', path: '/teacher/question-bank' },
  { id: 'exam', path: '/teacher/exams/new' },
  { id: 'assignment', path: '/teacher/assignments/new' },
  { id: 'review', path: '/teacher/review', visitOnly: true },
  { id: 'weekly', path: '/teacher/weekly-reports' }
];

/** A teacher in a school waits for the admin to give them students, so their guide starts by checking. */
const TEACHER_STEPS: SetupGuideStep[] = [{ id: 'students', path: '/teacher/students' }, ...TEACHING_STEPS];

/**
 * The owner of a single-teacher tenant has no admin to wait for: they set the school up themselves
 * (courses, classroom, students) and then teach. Setup steps reuse the admin guide's wording.
 */
const OWNER_STEPS: SetupGuideStep[] = [
  { id: 'course', path: '/admin/courses', textPrefix: 'admin.guide' },
  { id: 'classroom', path: '/admin/create-classroom', textPrefix: 'admin.guide' },
  { id: 'assign', path: '/admin/assign-classroom' },
  { id: 'student', path: '/admin/students', textPrefix: 'admin.guide' },
  { id: 'enroll', path: '/admin/enroll-student', textPrefix: 'admin.guide' },
  ...TEACHING_STEPS.map((step) => ({ ...step, textPrefix: 'teacher.guide' }))
];

/**
 * Getting-started checklist on the teacher overview. A regular teacher gets the teaching steps;
 * the owner of a single-teacher tenant gets setup and teaching in one list.
 */
@Component({
  selector: 'app-teacher-setup-guide',
  imports: [SetupGuideComponent],
  template: `<app-setup-guide [prefix]="prefix" [steps]="steps" [counts]="counts()" />`
})
export class TeacherSetupGuideComponent {
  private readonly api = inject(LearningApiService);
  private readonly isOwner = inject(AuthService).user()?.isTenantOwner === true;

  readonly prefix = this.isOwner ? 'owner.guide' : 'teacher.guide';
  readonly steps = this.isOwner ? OWNER_STEPS : TEACHER_STEPS;

  readonly counts = signal<Record<string, number> | null>(null);

  constructor() {
    // Each list fails on its own so one broken endpoint doesn't blank the whole guide.
    forkJoin({
      classrooms: this.api.getClassrooms().pipe(catchError(() => of([] as Classroom[]))),
      videos: this.api.getVideoLibrary().pipe(catchError(() => of(null))),
      materials: this.api.getLearningMaterials().pipe(catchError(() => of([]))),
      questions: this.api.getBankQuestions().pipe(catchError(() => of([]))),
      exams: this.api.getExams().pipe(catchError(() => of([]))),
      assignments: this.api.getAssignments().pipe(catchError(() => of([]))),
      weekly: this.api.listWeeklyReports().pipe(catchError(() => of([]))),
      // Only the owner may list these; a regular teacher's guide has no step that needs them.
      courses: this.isOwner ? this.api.getCourses(false).pipe(catchError(() => of([]))) : of([]),
      allStudents: this.isOwner ? this.api.getUsers('Student').pipe(catchError(() => of([]))) : of([])
    }).subscribe(({ classrooms, videos, materials, questions, exams, assignments, weekly, courses, allStudents }) =>
      this.counts.set({
        students: new Set(classrooms.flatMap((c) => (c.students ?? []).map((s) => s.studentId))).size,
        video: (videos?.lessonVideos?.length ?? 0) + (videos?.courseVideos?.length ?? 0),
        material: materials.length,
        question: questions.length,
        exam: exams.length,
        assignment: assignments.length,
        weekly: weekly.length,
        course: courses.length,
        classroom: classrooms.length,
        assign: classrooms.filter((c) => (c.teachers ?? []).length > 0 || (c.courses ?? []).some((x) => !!x.teacherId)).length,
        student: allStudents.length,
        enroll: classrooms.reduce((sum, c) => sum + (c.students ?? []).length, 0)
      })
    );
  }
}
