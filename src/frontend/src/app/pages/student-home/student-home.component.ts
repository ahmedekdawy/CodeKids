import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../auth.service';
import { LearningApiService } from '../../learning-api.service';
import { SiteBrandService } from '../../site-brand.service';
import { Assignment, Avatar, Badge, Classroom, Course, CourseQuiz, CourseTerm, CourseVideoSummary, Exam, StudentGradeCertificate, StudentSummary } from '../../models';
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
import { LocaleService } from '../../i18n/locale.service';
import { formatGradeLabel } from '../../grade.util';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { StudentAskPanelComponent, StudentAskCourseChoice } from '../../shared/student-ask-panel/student-ask-panel.component';
import { NotificationBellComponent } from '../../shared/notification-bell/notification-bell.component';
import { ApiBusyIndicatorComponent } from '../../shared/api-busy-indicator/api-busy-indicator.component';
import { UserPhotoComponent } from '../../shared/user-photo/user-photo.component';
import { PROFILE_PHOTO_MAX_BYTES, PROFILE_PHOTO_TYPES } from '../../shared/user-photo/profile-photo.rules';

@Component({
  selector: 'app-student-home',
  imports: [
    SearchableSelectComponent,
    FormsModule,
    RouterLink,
    TranslatePipe,
    CertificatePreviewComponent,
    LanguageSwitcherComponent,
    ThemeSwitcherComponent,
    SiteBrandComponent,
    StudentAskPanelComponent,
    NotificationBellComponent,
    ApiBusyIndicatorComponent,
    UserPhotoComponent
  ],
  templateUrl: './student-home.component.html',
  styleUrl: './student-home.component.css'
})
export class StudentHomeComponent {
  readonly auth = inject(AuthService);
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  private readonly brand = inject(SiteBrandService);
  private readonly http = inject(HttpClient);

  readonly courses = signal<Course[]>([]);
  readonly summary = signal<StudentSummary | null>(null);
  readonly badges = signal<Badge[]>([]);
  readonly avatars = signal<Avatar[]>([]);
  readonly classrooms = signal<Classroom[]>([]);
  readonly assignments = signal<Assignment[]>([]);
  readonly exams = signal<Exam[]>([]);
  readonly certificates = signal<StudentGradeCertificate[]>([]);
  readonly openCertificateId = signal<string | null>(null);
  readonly exportingCertificates = signal(false);

  readonly photoBusy = signal(false);
  readonly photoError = signal('');

  readonly hasPhoto = computed(() => !!this.auth.user()?.profilePhotoUrl);
  readonly selectedAvatar = computed(() => this.avatars().find((a) => a.isSelected) ?? null);
  readonly earnedBadges = computed(() => this.badges().filter((b) => b.isEarned));
  readonly publishedAssignments = computed(() => this.assignments().filter((a) => a.isPublished === true));
  readonly publishedExams = computed(() => this.exams().filter((e) => e.isPublished === true));
  readonly leftoverAssignments = computed(() =>
    this.publishedAssignments().filter((assignment) => !this.courses().some((course) => this.assignmentBelongsToCourse(assignment, course)))
  );
  readonly leftoverExams = computed(() =>
    this.publishedExams().filter((exam) => !this.courses().some((course) => this.examBelongsToCourse(exam, course)))
  );
  readonly askCourses = computed<StudentAskCourseChoice[]>(() =>
    this.courses()
      .filter((course) => course.studentAskEnabled)
      .map((course) => ({ id: course.id, title: course.title }))
  );

  constructor() {
    this.api.getCourses().subscribe((courses) => this.courses.set(courses));
    this.api.getStudentSummary().subscribe((summary) => this.summary.set(summary));
    this.api.getBadges().subscribe((badges) => this.badges.set(badges));
    this.api.getAvatars().subscribe((avatars) => this.avatars.set(avatars));
    this.api.getClassrooms().subscribe((classrooms) => this.classrooms.set(classrooms));
    this.api.getAssignments().subscribe((assignments) => this.assignments.set(assignments));
    this.api.getExams().subscribe((exams) => this.exams.set(exams));
    if (this.auth.user()?.id) {
      this.api.getStudentGradeCertificates(this.auth.user()!.id).subscribe({
        next: (certificates) => this.certificates.set(certificates ?? []),
        error: () => this.certificates.set([])
      });
    }
  }

  toggleCertificate(certificateId: string): void {
    this.openCertificateId.set(this.openCertificateId() === certificateId ? null : certificateId);
  }

  formatDate(value: string | null | undefined): string {
    if (!value) return this.locale.t('common.emDash');
    const date = value.length <= 10 ? new Date(`${value}T00:00:00`) : new Date(value);
    return date.toLocaleDateString(this.locale.lang());
  }

  /** Prints the selected certificate, or all approved certificates when none is selected. */
  async printCertificates(): Promise<void> {
    const models = await this.buildCertificateModels();
    if (!models.length) return;
    const printed = await printCertificateModels(models, this.locale.lang() === 'ar', this.printTitle(models));
    if (!printed) {
      this.photoError.set(this.locale.t('certificates.popupBlocked'));
    }
  }

  /** Downloads one PNG per certificate; selected one first, or all approved certificates. */
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
    } catch {
      // Download failures are surfaced by the browser itself.
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
      this.auth.user()?.displayName || '',
      this.gradeLabel(items[0]?.grade),
      brand,
      defaultCertificateLabels((key) => this.locale.t(key))
    );
  }

  private printTitle(models: CertificatePrintModel[]): string {
    return models.length === 1 ? models[0].title : this.locale.t('certificates.title');
  }

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

  onPhotoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    this.photoError.set('');
    if (!PROFILE_PHOTO_TYPES.includes(file.type)) {
      this.photoError.set(this.locale.t('student.photo.invalidType'));
      return;
    }
    if (file.size > PROFILE_PHOTO_MAX_BYTES) {
      this.photoError.set(this.locale.t('student.photo.tooLarge'));
      return;
    }

    this.photoBusy.set(true);
    this.auth.uploadProfilePhoto(file).subscribe({
      next: () => this.photoBusy.set(false),
      error: (err) => {
        this.photoBusy.set(false);
        this.photoError.set(this.locale.fromApiError(err, 'student.photo.uploadFailed'));
      }
    });
  }

  removePhoto(): void {
    this.photoError.set('');
    this.photoBusy.set(true);
    this.auth.removeProfilePhoto().subscribe({
      next: () => this.photoBusy.set(false),
      error: (err) => {
        this.photoBusy.set(false);
        this.photoError.set(this.locale.fromApiError(err, 'student.photo.removeFailed'));
      }
    });
  }

  selectAvatar(avatar: Avatar): void {
    if (!avatar.isUnlocked || avatar.isSelected) return;
    this.api.selectAvatar(avatar.id).subscribe(() => {
      this.api.getAvatars().subscribe((avatars) => this.avatars.set(avatars));
    });
  }

  selectAvatarById(id: string | number | null): void {
    const avatar = this.avatars().find((a) => a.id === String(id ?? ''));
    if (avatar) this.selectAvatar(avatar);
  }

  avatarOptionLabel(option: Avatar): string {
    const base = `${option.emoji} ${option.name}`;
    return option.isUnlocked
      ? base
      : `${base} (${this.locale.t('student.needsXp', { xp: option.unlockXp })})`;
  }

  courseAssignments(course: Course): Assignment[] {
    return this.publishedAssignments().filter((assignment) => this.assignmentBelongsToCourse(assignment, course));
  }

  courseExams(course: Course): Exam[] {
    return this.publishedExams().filter((exam) => this.examBelongsToCourse(exam, course));
  }

  termLabel(term: CourseTerm | string | null | undefined): string {
    if (!term) return this.locale.t('student.allTerms');
    if (term === 'FirstTerm') return this.locale.t('student.firstTerm');
    if (term === 'SecondTerm') return this.locale.t('student.secondTerm');
    return this.locale.t('student.fullYear');
  }

  gradeLabel(grade: number | null | undefined): string {
    return formatGradeLabel((k, p) => this.locale.t(k, p), grade, 'student.allGrades');
  }

  courseLessonCount(course: Course): number {
    if (course.lessons?.length) return course.lessons.length;
    return (course.units ?? []).reduce((count, unit) => count + (unit.lessons?.length ?? 0), 0);
  }

  courseVideos(course: Course): CourseVideoSummary[] {
    return course.videos ?? [];
  }

  badgeTitle(badge: Badge): string {
    return this.translateBadgeField(badge.code, badge.name);
  }

  badgeDescription(badge: Badge): string {
    return this.translateBadgeField(`${badge.code}.desc`, badge.description);
  }

  publishedQuizzes(course: Course): CourseQuiz[] {
    return (course.quizzes ?? []).filter((quiz) => quiz.isPublished === true);
  }

  private translateBadgeField(suffix: string, fallback: string): string {
    const key = `badge.${suffix}`;
    const translated = this.locale.t(key);
    return translated === key ? fallback : translated;
  }

  private assignmentBelongsToCourse(assignment: Assignment, course: Course): boolean {
    if (assignment.courseId) return assignment.courseId === course.id;
    return this.classroomLinksOnlyCourse(assignment.classroomId, course.id);
  }

  private examBelongsToCourse(exam: Exam, course: Course): boolean {
    if (exam.courseId) return exam.courseId === course.id;
    return this.classroomLinksOnlyCourse(exam.classroomId, course.id);
  }

  /** Match a classroom task to a subject only when the room clearly points at that one course. */
  private classroomLinksOnlyCourse(classroomId: string, courseId: string): boolean {
    const room = this.classrooms().find((classroom) => classroom.id === classroomId);
    if (!room) return false;
    if (room.courseId) return room.courseId === courseId;
    const linked = room.courses ?? [];
    if (linked.length === 1) return linked[0].courseId === courseId;
    return false;
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
