import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../auth.service';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { SiteBrandService } from '../../site-brand.service';
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
  ParentDashboard,
  StudentGradeCertificate
} from '../../models';
import {
  CertificatePrintModel,
  defaultCertificateLabels,
  exportCertificateModelImages,
  printCertificateModels,
  safeCertificateFileName,
  studentCertificatePrintModels
} from '../../shared/certificate-print/certificate-print.util';
import { CertificatePreviewComponent } from '../../shared/certificate-preview/certificate-preview.component';
import { LanguageSwitcherComponent } from '../../shared/language-switcher/language-switcher.component';
import { ThemeSwitcherComponent } from '../../shared/theme-switcher/theme-switcher.component';
import { SiteBrandComponent } from '../../shared/site-brand/site-brand.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { NotificationBellComponent } from '../../shared/notification-bell/notification-bell.component';
import { ApiBusyIndicatorComponent } from '../../shared/api-busy-indicator/api-busy-indicator.component';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import { UserPhotoComponent } from '../../shared/user-photo/user-photo.component';

@Component({
  selector: 'app-parent-dashboard',
  imports: [FormsModule, RouterLink, TranslatePipe, SiteBrandComponent, LanguageSwitcherComponent, ThemeSwitcherComponent, NotificationBellComponent, ApiBusyIndicatorComponent, IconActionButtonComponent, UserPhotoComponent, CertificatePreviewComponent],
  templateUrl: './parent-dashboard.component.html',
  styleUrl: './parent-dashboard.component.css'
})
export class ParentDashboardComponent {
  readonly auth = inject(AuthService);
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  private readonly brand = inject(SiteBrandService);
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly dashboard = signal<ParentDashboard | null>(null);
  readonly meetings = signal<LiveSession[]>([]);
  readonly classrooms = signal<Classroom[]>([]);
  readonly selectedChildId = signal<string | null>(null);
  readonly overview = signal<ParentChildOverview | null>(null);
  readonly selectedCourseId = signal<string | null>(null);
  readonly loadingChild = signal(false);
  readonly savingChild = signal(false);
  readonly impersonatingId = signal<string | null>(null);
  readonly message = signal('');
  readonly error = signal('');
  readonly certificates = signal<StudentGradeCertificate[]>([]);
  readonly certificatesLoading = signal(false);
  readonly openCertificateId = signal<string | null>(null);
  readonly exportingCertificates = signal(false);

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
    this.loadCertificates(child.studentId);
  }

  /** Approved certificates of the selected child; only these are visible to parents. */
  loadCertificates(childId: string): void {
    this.certificates.set([]);
    this.openCertificateId.set(null);
    this.certificatesLoading.set(true);
    this.api.getChildGradeCertificates(childId).subscribe({
      next: (certificates) => {
        this.certificates.set(certificates ?? []);
        this.certificatesLoading.set(false);
      },
      error: (err) => {
        this.certificatesLoading.set(false);
        this.error.set(this.locale.fromApiError(err, 'certificates.loadFailed'));
      }
    });
  }

  toggleCertificate(certificateId: string): void {
    this.openCertificateId.set(this.openCertificateId() === certificateId ? null : certificateId);
  }

  /** Prints the selected certificate, or all of the child's certificates when none is selected. */
  async printCertificates(): Promise<void> {
    const models = await this.buildCertificateModels();
    if (!models.length) return;
    const printed = await printCertificateModels(models, this.locale.lang() === 'ar', this.printTitle(models));
    if (!printed) {
      this.error.set(this.locale.t('certificates.popupBlocked'));
    }
  }

  /** Downloads one PNG per certificate; selected one first, or all of the child's certificates. */
  async exportCertificateImages(): Promise<void> {
    if (this.exportingCertificates()) return;
    this.exportingCertificates.set(true);
    try {
      const models = await this.buildCertificateModels();
      const items = models.map((model) => ({
        model,
        fileName: safeCertificateFileName(`${model.title} - ${model.studentName}`)
      }));
      await exportCertificateModelImages(items, this.locale.lang() === 'ar');
      this.message.set(this.locale.t('certificates.exported'));
    } catch {
      this.error.set(this.locale.t('certificates.exportFailed'));
    } finally {
      this.exportingCertificates.set(false);
    }
  }

  private async buildCertificateModels(): Promise<CertificatePrintModel[]> {
    const openId = this.openCertificateId();
    const items = openId
      ? this.certificates().filter((certificate) => certificate.id === openId)
      : this.certificates();
    if (!items.length) return [];
    const brand = await this.loadPrintBrand();
    return studentCertificatePrintModels(
      items,
      this.selectedChild()?.displayName || this.overview()?.displayName || '',
      this.gradeLabel(items[0]?.grade),
      brand,
      defaultCertificateLabels((key) => this.locale.t(key))
    );
  }

  private printTitle(models: CertificatePrintModel[]): string {
    return models.length === 1 ? models[0].title : this.locale.t('certificates.title');
  }

  /** Platform name and logo, with the logo inlined as a data URL for printing/exporting. */
  private async loadPrintBrand(): Promise<{ name: string; logo: string | null }> {
    const logoUrl = this.brand.logoUrl();
    let logo: string | null = null;
    if (logoUrl) {
      const absolute = new URL(logoUrl, window.location.origin).href;
      try {
        logo = await blobToDataUrl(await firstValueFrom(this.http.get(absolute, { responseType: 'blob' })));
      } catch {
        logo = absolute;
      }
    }
    return { name: this.brand.siteName(), logo };
  }

  selectCourse(course: ParentChildCourse): void {
    this.selectedCourseId.set(course.courseId);
  }

  loginAs(childId: string): void {
    this.error.set('');
    this.message.set('');
    this.impersonatingId.set(childId);
    this.auth.impersonateChildAsParent(childId).subscribe({
      next: () => {
        this.impersonatingId.set(null);
        void this.router.navigateByUrl(this.auth.roleHome());
      },
      error: (err) => {
        this.impersonatingId.set(null);
        this.error.set(this.locale.fromApiError(err, 'parent.loginAsFailed'));
      }
    });
  }

  backToChildren(): void {
    this.selectedChildId.set(null);
    this.selectedCourseId.set(null);
    this.overview.set(null);
    this.message.set('');
    this.error.set('');
    this.clearChildPassword();
  }

  saveChildAccount(): void {
    const childId = this.selectedChildId();
    if (!childId) return;
    this.saveChild(childId);
  }

  private saveChild(userId: string): void {
    const form = {
      email: this.childEmail,
      mobilePhone: this.childMobile,
      password: this.childPassword,
      confirmPassword: this.childPasswordConfirm
    };
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

    this.savingChild.set(true);
    this.api
      .updateParentManagedAccount(userId, {
        email: form.email.trim() || null,
        mobilePhone: form.mobilePhone.trim() || null,
        password: form.password.trim() || null
      })
      .subscribe({
        next: (account) => {
          this.savingChild.set(false);
          this.clearChildPassword();
          this.message.set(this.locale.t('parent.childAccountSaved'));
          this.reloadDashboard(userId);
        },
        error: (err) => {
          this.savingChild.set(false);
          this.error.set(this.locale.fromApiError(err, 'parent.accountSaveFailed'));
        }
      });
  }

  private reloadDashboard(keepChildId?: string): void {
    this.api.getParentDashboard().subscribe({
      next: (dashboard) => {
        this.dashboard.set(dashboard);
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

  badgeLabel(name: string): string {
    const byName: Record<string, string> = {
      'Weekly Star': 'badge.WEEKLY_STAR',
      'Assignment Ace': 'badge.ASSIGNMENT_ACE',
      'Exam Star': 'badge.EXAM_STAR',
      'Quiz Ace': 'badge.QUIZ_ACE'
    };
    const key = byName[name];
    return key ? this.locale.t(key) : name;
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

function blobToDataUrl(blob: Blob): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result ?? ''));
    reader.onerror = () => reject(reader.error);
    reader.readAsDataURL(blob);
  });
}
