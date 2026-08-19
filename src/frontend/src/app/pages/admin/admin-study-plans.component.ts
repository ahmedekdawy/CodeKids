import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Course, ManagedUser, WeeklyStudyPlan } from '../../models';
import { TranslatePipe } from '../../shared/translate.pipe';
import { formatCourseLabel } from '../../grade.util';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';
import { StudyPlanSheetComponent } from '../../shared/study-plan-sheet/study-plan-sheet.component';
import { normalizeStudyPlans } from '../../shared/study-plan-sheet/study-plan.util';

@Component({
  selector: 'app-admin-study-plans',
  imports: [
    PageFeedbackComponent,
    SearchableSelectComponent,
    FormsModule,
    TranslatePipe,
    StudyPlanSheetComponent
  ],
  templateUrl: './admin-study-plans.component.html',
  styleUrls: ['./admin-panel.css', './admin-study-plans.component.css']
})
export class AdminStudyPlansComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  readonly teachers = signal<ManagedUser[]>([]);
  readonly courses = signal<Course[]>([]);
  readonly plans = signal<WeeklyStudyPlan[]>([]);
  readonly selectedPlanId = signal<string | null>(null);
  readonly message = signal('');
  readonly error = signal('');

  readonly filterTeacherId = signal('');
  readonly filterCourseId = signal('');
  readonly filterFromDate = signal('');
  readonly filterToDate = signal('');

  readonly teacherOptions = computed(() =>
    this.teachers()
      .slice()
      .sort((a, b) => a.displayName.localeCompare(b.displayName))
      .map((teacher) => ({ value: teacher.id, label: teacher.displayName }))
  );

  readonly courseOptions = computed(() =>
    this.courses()
      .slice()
      .sort((a, b) => {
        const ga = a.grade ?? 999;
        const gb = b.grade ?? 999;
        if (ga !== gb) return ga - gb;
        return a.title.localeCompare(b.title);
      })
      .map((course) => ({ value: course.id, label: this.courseLabel(course) }))
  );

  readonly selectedPlan = computed(() => {
    const id = this.selectedPlanId();
    return this.plans().find((plan) => plan.id === id) ?? this.plans()[0] ?? null;
  });

  constructor() {
    this.api.getUsers('Teacher').subscribe({
      next: (users) => this.teachers.set(users ?? []),
      error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.studyPlans.loadFailed'))
    });
    this.api.getCourses().subscribe({
      next: (courses) => this.courses.set(courses ?? []),
      error: () => undefined
    });
    this.loadPlans();
  }

  courseLabel(course: Course): string {
    return formatCourseLabel((k, p) => this.locale.t(k, p), course.title, course.grade);
  }

  planCourseLabel(plan: WeeklyStudyPlan): string {
    return formatCourseLabel((k, p) => this.locale.t(k, p), plan.courseName, plan.courseGrade);
  }

  loadPlans(): void {
    this.message.set('');
    this.error.set('');
    this.api
      .listStudyPlans({
        teacherId: this.filterTeacherId() || undefined,
        courseId: this.filterCourseId() || undefined,
        fromDate: this.filterFromDate() || undefined,
        toDate: this.filterToDate() || undefined
      })
      .subscribe({
        next: (rows) => {
          const plans = normalizeStudyPlans(rows);
          this.plans.set(plans);
          const selected = this.selectedPlanId();
          if (!selected || !plans.some((plan) => plan.id === selected)) {
            this.selectedPlanId.set(plans[0]?.id ?? null);
          }
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.studyPlans.loadFailed'))
      });
  }

  resetFilters(): void {
    this.filterTeacherId.set('');
    this.filterCourseId.set('');
    this.filterFromDate.set('');
    this.filterToDate.set('');
    this.loadPlans();
  }

  selectPlan(plan: WeeklyStudyPlan): void {
    this.selectedPlanId.set(plan.id);
  }
}
