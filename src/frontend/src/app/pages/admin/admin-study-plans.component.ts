import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Course, ManagedUser, WeeklyStudyPlan } from '../../models';
import { TranslatePipe } from '../../shared/translate.pipe';
import { formatCourseLabel } from '../../grade.util';
import { totalPages } from '../../list-query.util';
import { SortDir, nextSort } from '../../sort.util';
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
  readonly totalCount = signal(0);
  readonly selectedPlanId = signal<string | null>(null);
  readonly message = signal('');
  readonly error = signal('');
  readonly sortKey = signal('fromDate');
  readonly sortDir = signal<SortDir>('desc');
  readonly page = signal(1);
  readonly pageSize = signal(10);
  readonly pageSizeOptions = [10, 25, 50];

  readonly filterTeacherId = signal('');
  readonly filterCourseId = signal('');
  readonly filterFromDate = signal('');
  readonly filterToDate = signal('');

  readonly totalPages = computed(() => totalPages(this.totalCount(), this.pageSize()));

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
    this.api.getCourses(false).subscribe({
      next: (courses) => this.courses.set(courses ?? []),
      error: () => undefined
    });
    this.loadPlans();
  }

  courseLabel(course: Course): string {
    return formatCourseLabel((k, p) => this.locale.t(k, p), course.title, course.grade, 'common.allGrades', course.stageId);
  }

  planCourseLabel(plan: WeeklyStudyPlan): string {
    return formatCourseLabel((k, p) => this.locale.t(k, p), plan.courseName, plan.courseGrade, 'common.allGrades', plan.courseStageId);
  }

  loadPlans(): void {
    this.message.set('');
    this.error.set('');
    this.api
      .getAdminStudyPlans({
        teacherId: this.filterTeacherId() || undefined,
        courseId: this.filterCourseId() || undefined,
        fromDate: this.filterFromDate() || undefined,
        toDate: this.filterToDate() || undefined,
        sortKey: this.sortKey(),
        sortDir: this.sortDir(),
        page: this.page(),
        pageSize: this.pageSize()
      })
      .subscribe({
        next: (result) => {
          this.totalCount.set(result.totalCount);
          if (this.page() > totalPages(result.totalCount, this.pageSize())) {
            this.page.set(Math.max(1, totalPages(result.totalCount, this.pageSize())));
            if (this.page() !== result.page) {
              this.loadPlans();
              return;
            }
          }
          const plans = normalizeStudyPlans(result.items);
          this.plans.set(plans);
          const selected = this.selectedPlanId();
          if (!selected || !plans.some((plan) => plan.id === selected)) {
            this.selectedPlanId.set(plans[0]?.id ?? null);
          }
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.studyPlans.loadFailed'))
      });
  }

  setSort(key: string): void {
    this.sortDir.set(nextSort(this.sortKey(), key, this.sortDir()));
    this.sortKey.set(key);
    this.page.set(1);
    this.loadPlans();
  }

  sortMark(key: string): string {
    if (this.sortKey() !== key) return '';
    return this.sortDir() === 'asc' ? '↑' : '↓';
  }

  setFilterTeacher(teacherId: string): void {
    this.filterTeacherId.set(teacherId);
    this.page.set(1);
    this.loadPlans();
  }

  setFilterCourse(courseId: string): void {
    this.filterCourseId.set(courseId);
    this.page.set(1);
    this.loadPlans();
  }

  setFilterFromDate(value: string): void {
    this.filterFromDate.set(value);
    this.page.set(1);
    this.loadPlans();
  }

  setFilterToDate(value: string): void {
    this.filterToDate.set(value);
    this.page.set(1);
    this.loadPlans();
  }

  setPageSize(value: string | number): void {
    this.pageSize.set(Number(value) || 10);
    this.page.set(1);
    this.loadPlans();
  }

  goToPage(nextPage: number): void {
    this.page.set(Math.min(Math.max(1, nextPage), this.totalPages()));
    this.loadPlans();
  }

  resetFilters(): void {
    this.filterTeacherId.set('');
    this.filterCourseId.set('');
    this.filterFromDate.set('');
    this.filterToDate.set('');
    this.page.set(1);
    this.loadPlans();
  }

  selectPlan(plan: WeeklyStudyPlan): void {
    this.selectedPlanId.set(plan.id);
  }
}
