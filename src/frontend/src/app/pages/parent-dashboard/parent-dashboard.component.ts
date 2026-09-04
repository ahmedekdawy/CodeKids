import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../auth.service';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { formatGradeLabel } from '../../grade.util';
import { classroomHasZoomLinks } from '../../shared/classroom-zoom-links/classroom-zoom-links.util';
import {
  ChildEvaluationSummary,
  ChildProgress,
  Classroom,
  LiveSession,
  ParentAssessmentItem,
  ParentChildCourse,
  ParentChildOverview,
  ParentDashboard
} from '../../models';
import { LanguageSwitcherComponent } from '../../shared/language-switcher/language-switcher.component';
import { ThemeSwitcherComponent } from '../../shared/theme-switcher/theme-switcher.component';
import { SiteBrandComponent } from '../../shared/site-brand/site-brand.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { NotificationBellComponent } from '../../shared/notification-bell/notification-bell.component';
import { ApiBusyIndicatorComponent } from '../../shared/api-busy-indicator/api-busy-indicator.component';
import { UserPhotoComponent } from '../../shared/user-photo/user-photo.component';

@Component({
  selector: 'app-parent-dashboard',
  imports: [FormsModule, RouterLink, TranslatePipe, SiteBrandComponent, LanguageSwitcherComponent, ThemeSwitcherComponent, NotificationBellComponent, ApiBusyIndicatorComponent, UserPhotoComponent],
  templateUrl: './parent-dashboard.component.html',
  styleUrl: './parent-dashboard.component.css'
})
export class ParentDashboardComponent {
  readonly auth = inject(AuthService);
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  private readonly route = inject(ActivatedRoute);

  readonly dashboard = signal<ParentDashboard | null>(null);
  readonly meetings = signal<LiveSession[]>([]);
  readonly classrooms = signal<Classroom[]>([]);
  readonly selectedChildId = signal<string | null>(null);
  readonly overview = signal<ParentChildOverview | null>(null);
  readonly selectedCourseId = signal<string | null>(null);
  readonly loadingChild = signal(false);
  readonly savingParent = signal(false);
  readonly savingChild = signal(false);
  readonly message = signal('');
  readonly error = signal('');

  parentEmail = '';
  parentMobile = '';
  parentPassword = '';
  parentPasswordConfirm = '';
  childEmail = '';
  childMobile = '';
  childPassword = '';
  childPasswordConfirm = '';

  readonly selectedChild = computed(() => {
    const id = this.selectedChildId();
    return this.dashboard()?.children.find((c) => c.studentId === id) ?? null;
  });

  readonly selectedCourse = computed(() => {
    const id = this.selectedCourseId();
    return this.overview()?.courses.find((c) => c.courseId === id) ?? null;
  });

  readonly latestEvaluation = computed(
    () => this.selectedChild()?.latestEvaluation ?? this.overview()?.evaluations[0] ?? null
  );

  readonly classroomsWithZoom = computed(() => this.classrooms().filter((room) => classroomHasZoomLinks(room)));

  constructor() {
    this.reloadDashboard();
    this.api.getMeetings().subscribe((meetings) => this.meetings.set(meetings));
    this.api.getClassrooms().subscribe((classrooms) => this.classrooms.set(classrooms));
    this.route.queryParamMap.subscribe((params) => {
      const childId = params.get('child');
      if (!childId) return;
      const child = this.dashboard()?.children.find((c) => c.studentId === childId);
      if (child) {
        this.selectChild(child);
      }
    });
  }

  selectChild(child: ChildProgress): void {
    if (this.selectedChildId() === child.studentId && this.overview()) {
      this.selectedCourseId.set(null);
      return;
    }

    this.error.set('');
    this.message.set('');
    this.selectedChildId.set(child.studentId);
    this.selectedCourseId.set(null);
    this.overview.set(null);
    this.fillChildForm(child);
    this.loadingChild.set(true);
    this.api.getParentChildOverview(child.studentId).subscribe({
      next: (overview) => {
        this.overview.set(overview);
        this.loadingChild.set(false);
      },
      error: (err) => {
        this.loadingChild.set(false);
        this.error.set(this.locale.fromApiError(err, 'parent.loadChildFailed'));
      }
    });
  }

  selectCourse(course: ParentChildCourse): void {
    this.selectedCourseId.set(course.courseId);
  }

  backToChildren(): void {
    this.selectedChildId.set(null);
    this.selectedCourseId.set(null);
    this.overview.set(null);
    this.message.set('');
    this.error.set('');
    this.clearParentPassword();
    this.clearChildPassword();
  }

  saveParentAccount(): void {
    const dashboard = this.dashboard();
    if (!dashboard) return;
    this.saveAccount(dashboard.parentId, {
      email: this.parentEmail,
      mobilePhone: this.parentMobile,
      password: this.parentPassword,
      confirmPassword: this.parentPasswordConfirm
    }, 'self');
  }

  saveChildAccount(): void {
    const childId = this.selectedChildId();
    if (!childId) return;
    this.saveAccount(childId, {
      email: this.childEmail,
      mobilePhone: this.childMobile,
      password: this.childPassword,
      confirmPassword: this.childPasswordConfirm
    }, 'child');
  }

  private saveAccount(
    userId: string,
    form: { email: string; mobilePhone: string; password: string; confirmPassword: string },
    kind: 'self' | 'child'
  ): void {
    this.error.set('');
    this.message.set('');
    if (!form.email.trim() && !form.mobilePhone.trim()) {
      this.error.set(this.locale.t('admin.users.emailOrMobileRequired'));
      return;
    }
    if (form.password || form.confirmPassword) {
      if (form.password !== form.confirmPassword) {
        this.error.set(this.locale.t('auth.reset.mismatch'));
        return;
      }
      if (form.password.trim().length < 6) {
        this.error.set(this.locale.t('api.errors.auth.passwordTooShort'));
        return;
      }
    }

    const saving = kind === 'self' ? this.savingParent : this.savingChild;
    saving.set(true);
    this.api
      .updateParentManagedAccount(userId, {
        email: form.email.trim() || null,
        mobilePhone: form.mobilePhone.trim() || null,
        password: form.password.trim() || null
      })
      .subscribe({
        next: (account) => {
          saving.set(false);
          if (kind === 'self') {
            this.clearParentPassword();
            this.auth.patchUser({ email: account.email, mobilePhone: account.mobilePhone });
            this.message.set(this.locale.t('parent.accountSaved'));
          } else {
            this.clearChildPassword();
            this.message.set(this.locale.t('parent.childAccountSaved'));
          }
          this.reloadDashboard(kind === 'child' ? userId : undefined);
        },
        error: (err) => {
          saving.set(false);
          this.error.set(this.locale.fromApiError(err, 'parent.accountSaveFailed'));
        }
      });
  }

  private reloadDashboard(keepChildId?: string): void {
    this.api.getParentDashboard().subscribe({
      next: (dashboard) => {
        this.dashboard.set(dashboard);
        this.parentEmail = dashboard.parentEmail ?? '';
        this.parentMobile = dashboard.parentMobilePhone ?? '';
        const childId = keepChildId ?? this.selectedChildId() ?? this.route.snapshot.queryParamMap.get('child');
        if (childId) {
          const child = dashboard.children.find((c) => c.studentId === childId);
          if (child) {
            this.fillChildForm(child);
            if (!this.overview() || this.selectedChildId() !== childId) {
              this.selectChild(child);
            }
          }
        }
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'parent.loadChildFailed'))
    });
  }

  private fillChildForm(child: ChildProgress): void {
    this.childEmail = child.email ?? '';
    this.childMobile = child.mobilePhone ?? '';
    this.clearChildPassword();
  }

  private clearParentPassword(): void {
    this.parentPassword = '';
    this.parentPasswordConfirm = '';
  }

  private clearChildPassword(): void {
    this.childPassword = '';
    this.childPasswordConfirm = '';
  }

  backToCourses(): void {
    this.selectedCourseId.set(null);
  }

  gradeLabel(grade: number | null | undefined): string {
    if (grade == null) return this.locale.t('common.emDash');
    return formatGradeLabel((k, p) => this.locale.t(k, p), grade);
  }

  formatWhen(iso: string): string {
    return new Date(iso).toLocaleString(this.locale.lang());
  }

  formatDate(value: string | null | undefined): string {
    if (!value) return this.locale.t('common.emDash');
    const date = value.length <= 10 ? new Date(`${value}T00:00:00`) : new Date(value);
    return date.toLocaleDateString(this.locale.lang());
  }

  percent(value: number | null | undefined): string {
    return value == null ? this.locale.t('common.emDash') : `${value}%`;
  }

  interactionLabel(value: string | null | undefined): string {
    if (!value) return this.locale.t('common.emDash');
    const key = `teacher.weeklyReports.interaction.${value}`;
    const translated = this.locale.t(key);
    return translated === key ? value : translated;
  }

  cameraLabel(value: boolean | null | undefined): string {
    if (value === true) return this.locale.t('parent.cameraYes');
    if (value === false) return this.locale.t('parent.cameraNo');
    return this.locale.t('common.emDash');
  }

  evaluationLine(evaluation: ChildEvaluationSummary | null | undefined): string {
    if (!evaluation) return this.locale.t('parent.noEvaluation');
    const parts = [
      this.locale.t('parent.weekOf', { date: this.formatDate(evaluation.weekStartDate) }),
      `${this.locale.t('teacher.weeklyReports.performance')} ${this.percent(evaluation.performancePercent)}`,
      `${this.locale.t('teacher.weeklyReports.attendance')} ${this.percent(evaluation.attendancePercent)}`,
      `${this.locale.t('teacher.weeklyReports.homework')} ${this.percent(evaluation.homeworkPercent)}`
    ];
    return parts.join(' · ');
  }

  statusLabel(status: string): string {
    const key = `parent.status.${status}`;
    const translated = this.locale.t(key);
    return translated === key ? status : translated;
  }

  scoreLabel(item: ParentAssessmentItem): string {
    if (item.score == null) return this.locale.t('parent.resultNotStarted');
    return this.locale.t('parent.resultScore', {
      score: item.score,
      max: item.maxScore ?? this.locale.t('common.emDash')
    });
  }

  quizScoreLabel(score: number | null | undefined, total: number): string {
    if (score == null) return this.locale.t('parent.resultNotStarted');
    return this.locale.t('parent.quizScore', { score, total });
  }

  termLabel(term: string | null | undefined): string {
    if (!term) return this.locale.t('student.allTerms');
    if (term === 'FirstTerm') return this.locale.t('student.firstTerm');
    if (term === 'SecondTerm') return this.locale.t('student.secondTerm');
    return this.locale.t('student.fullYear');
  }
}
