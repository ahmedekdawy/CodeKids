import { Component, computed, inject, signal } from '@angular/core';
import { AuthService } from '../../auth.service';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { formatGradeLabel } from '../../grade.util';
import {
  ChildEvaluationSummary,
  ChildProgress,
  LiveSession,
  ParentAssessmentItem,
  ParentChildCourse,
  ParentChildOverview,
  ParentDashboard
} from '../../models';
import { LanguageSwitcherComponent } from '../../shared/language-switcher/language-switcher.component';
import { SiteBrandComponent } from '../../shared/site-brand/site-brand.component';
import { TranslatePipe } from '../../shared/translate.pipe';

@Component({
  selector: 'app-parent-dashboard',
  imports: [TranslatePipe, SiteBrandComponent, LanguageSwitcherComponent],
  templateUrl: './parent-dashboard.component.html',
  styleUrl: './parent-dashboard.component.css'
})
export class ParentDashboardComponent {
  readonly auth = inject(AuthService);
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  readonly dashboard = signal<ParentDashboard | null>(null);
  readonly meetings = signal<LiveSession[]>([]);
  readonly selectedChildId = signal<string | null>(null);
  readonly overview = signal<ParentChildOverview | null>(null);
  readonly selectedCourseId = signal<string | null>(null);
  readonly loadingChild = signal(false);
  readonly error = signal('');

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

  constructor() {
    this.api.getParentDashboard().subscribe((dashboard) => this.dashboard.set(dashboard));
    this.api.getMeetings().subscribe((meetings) => this.meetings.set(meetings));
  }

  selectChild(child: ChildProgress): void {
    if (this.selectedChildId() === child.studentId && this.overview()) {
      this.selectedCourseId.set(null);
      return;
    }

    this.error.set('');
    this.selectedChildId.set(child.studentId);
    this.selectedCourseId.set(null);
    this.overview.set(null);
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
    this.error.set('');
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
