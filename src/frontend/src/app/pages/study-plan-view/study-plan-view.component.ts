import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../auth.service';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { WeeklyStudyPlan } from '../../models';
import { LanguageSwitcherComponent } from '../../shared/language-switcher/language-switcher.component';
import { SiteBrandComponent } from '../../shared/site-brand/site-brand.component';
import { StudyPlanViewerComponent } from '../../shared/study-plan-sheet/study-plan-viewer.component';
import { normalizeStudyPlans } from '../../shared/study-plan-sheet/study-plan.util';
import { TranslatePipe } from '../../shared/translate.pipe';

@Component({
  selector: 'app-study-plan-view',
  imports: [
    RouterLink,
    TranslatePipe,
    SiteBrandComponent,
    LanguageSwitcherComponent,
    StudyPlanViewerComponent
  ],
  templateUrl: './study-plan-view.component.html',
  styleUrl: './study-plan-view.component.css'
})
export class StudyPlanViewComponent {
  readonly auth = inject(AuthService);
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  private readonly route = inject(ActivatedRoute);

  readonly plans = signal<WeeklyStudyPlan[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly childName = signal('');

  readonly isParent = computed(() => this.auth.user()?.role === 'Parent');
  readonly homeLink = computed(() => (this.isParent() ? '/parent' : '/student'));
  readonly titleKey = computed(() => (this.isParent() ? 'parent.studyPlansTitle' : 'student.studyPlans'));
  readonly hintKey = computed(() => (this.isParent() ? 'parent.studyPlansHint' : 'student.studyPlansHint'));
  readonly emptyKey = computed(() => (this.isParent() ? 'parent.noStudyPlans' : 'student.noStudyPlans'));
  readonly backKey = computed(() => (this.isParent() ? 'parent.backToChildren' : 'common.backMissions'));

  constructor() {
    const studentId = this.route.snapshot.paramMap.get('studentId') || undefined;
    const courseId = this.route.snapshot.queryParamMap.get('courseId') || undefined;

    if (this.isParent() && studentId) {
      this.api.getParentDashboard().subscribe({
        next: (dashboard) => {
          const child = dashboard.children.find((c) => c.studentId === studentId);
          this.childName.set(child?.displayName ?? '');
        },
        error: () => undefined
      });
    }

    this.api
      .listStudyPlans({
        studentId,
        courseId
      })
      .subscribe({
        next: (plans) => {
          this.plans.set(normalizeStudyPlans(plans));
          this.loading.set(false);
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(this.locale.fromApiError(err, 'teacher.studyPlans.loadFailed'));
        }
      });
  }
}
