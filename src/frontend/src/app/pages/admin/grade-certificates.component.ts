import { Component, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../auth.service';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { SiteBrandService } from '../../site-brand.service';
import {
  Classroom,
  GradeCertificateListItem,
  GradeCertificateSheet,
  GradeCertificateStudent,
  GradeCertificateSubject
} from '../../models';
import { formatGradeLabel } from '../../grade.util';
import {
  CertificatePrintModel,
  certificateMaxTotal,
  certificateTotal,
  exportCertificateModelImages,
  printCertificateModels,
  safeCertificateFileName
} from '../../shared/certificate-print/certificate-print.util';
import { TranslatePipe } from '../../shared/translate.pipe';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';

/** One subject row of the admin create/edit form. */
interface SubjectDraft {
  courseId: string;
  courseTitle: string;
  selected: boolean;
  maxDegree: number;
  includedInTotal: boolean;
}

/**
 * Student marks certificates. Admins define a certificate per classroom and see every subject;
 * teachers open the same page and only enter degrees for the subjects they teach.
 */
@Component({
  selector: 'app-grade-certificates',
  imports: [PageFeedbackComponent, SearchableSelectComponent, FormsModule, TranslatePipe, IconActionButtonComponent],
  templateUrl: './grade-certificates.component.html',
  styleUrls: ['./admin-panel.css', './grade-certificates.component.css']
})
export class GradeCertificatesComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  private readonly auth = inject(AuthService);
  private readonly brand = inject(SiteBrandService);
  private readonly http = inject(HttpClient);

  readonly isAdmin = computed(() => this.auth.user()?.role === 'SuperAdmin');

  readonly certificates = signal<GradeCertificateListItem[]>([]);
  readonly classrooms = signal<Classroom[]>([]);
  readonly sheet = signal<GradeCertificateSheet | null>(null);
  readonly message = signal('');
  readonly error = signal('');
  readonly saving = signal(false);
  readonly exporting = signal(false);
  readonly approving = signal(false);

  // Admin create/edit form.
  editingId: string | null = null;
  formTitle = '';
  formClassroomId = '';
  subjectDrafts: SubjectDraft[] = [];

  /** Degrees being edited, keyed by student id then subject id. */
  marks: Record<string, Record<string, number | null>> = {};

  readonly classroomOptions = computed(() =>
    this.classrooms().map((room) => ({ value: room.id, label: room.name }))
  );

  constructor() {
    this.reload();
    if (this.isAdmin()) {
      this.api.getClassrooms().subscribe({
        next: (classrooms) => this.classrooms.set(classrooms ?? []),
        error: (err) => this.error.set(this.locale.fromApiError(err, 'certificates.loadFailed'))
      });
    }
  }

  gradeLabel(grade?: number | null): string {
    if (grade == null) return this.locale.t('common.emDash');
    return formatGradeLabel((k, p) => this.locale.t(k, p), grade);
  }

  reload(): void {
    this.api.listGradeCertificates().subscribe({
      next: (certificates) => this.certificates.set(certificates ?? []),
      error: (err) => this.error.set(this.locale.fromApiError(err, 'certificates.loadFailed'))
    });
  }

  // ---- Admin: define the certificate -------------------------------------------------

  onFormClassroomChange(classroomId: string): void {
    this.formClassroomId = classroomId;
    this.subjectDrafts = this.draftsForClassroom(classroomId, []);
  }

  startEdit(certificate: GradeCertificateListItem): void {
    this.clearFeedback();
    this.api.getGradeCertificate(certificate.id).subscribe({
      next: (sheet) => {
        this.editingId = sheet.id;
        this.formTitle = sheet.title;
        this.formClassroomId = sheet.classroomId;
        this.subjectDrafts = this.draftsForClassroom(sheet.classroomId, sheet.subjects);
        document.getElementById('certificate-form')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'certificates.loadFailed'))
    });
  }

  cancelEdit(): void {
    this.editingId = null;
    this.formTitle = '';
    this.formClassroomId = '';
    this.subjectDrafts = [];
  }

  saveCertificate(): void {
    this.clearFeedback();
    const subjects = this.subjectDrafts
      .filter((draft) => draft.selected)
      .map((draft) => ({
        courseId: draft.courseId,
        maxDegree: Number(draft.maxDegree),
        includedInTotal: draft.includedInTotal
      }));
    if (!this.formTitle.trim() || !this.formClassroomId || !subjects.length) {
      this.error.set(this.locale.t('certificates.required'));
      return;
    }
    if (subjects.some((subject) => !(subject.maxDegree > 0))) {
      this.error.set(this.locale.t('certificates.maxRequired'));
      return;
    }

    const payload = { classroomId: this.formClassroomId, title: this.formTitle.trim(), subjects };
    const request = this.editingId
      ? this.api.updateGradeCertificate(this.editingId, payload)
      : this.api.createGradeCertificate(payload);
    this.saving.set(true);
    request.subscribe({
      next: (sheet) => {
        this.saving.set(false);
        this.cancelEdit();
        this.message.set(this.locale.t('certificates.saved'));
        this.reload();
        this.showSheet(sheet);
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(this.locale.fromApiError(err, 'certificates.saveFailed'));
      }
    });
  }

  deleteCertificate(certificate: GradeCertificateListItem): void {
    if (!confirm(this.locale.t('certificates.confirmDelete', { title: certificate.title }))) return;
    this.clearFeedback();
    this.api.deleteGradeCertificate(certificate.id).subscribe({
      next: () => {
        if (this.sheet()?.id === certificate.id) this.sheet.set(null);
        if (this.editingId === certificate.id) this.cancelEdit();
        this.message.set(this.locale.t('certificates.deleted'));
        this.reload();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'certificates.deleteFailed'))
    });
  }

  // ---- Admin: approval ----------------------------------------------------------------

  /** Approves one certificate; afterwards the student and their parent can see it. */
  approveCertificate(certificate: GradeCertificateListItem): void {
    if (this.approving()) return;
    this.clearFeedback();
    this.approving.set(true);
    this.api.approveGradeCertificate(certificate.id).subscribe({
      next: () => {
        this.approving.set(false);
        this.message.set(this.locale.t('certificates.approved'));
        this.reload();
      },
      error: (err) => {
        this.approving.set(false);
        this.error.set(this.locale.fromApiError(err, 'certificates.approveFailed'));
      }
    });
  }

  /** Approves every not-yet-approved certificate in one go. */
  approveAllCertificates(): void {
    if (this.approving()) return;
    this.clearFeedback();
    this.approving.set(true);
    this.api.approveAllGradeCertificates().subscribe({
      next: (result) => {
        this.approving.set(false);
        this.message.set(
          this.locale.t('certificates.approvedAll', { count: result?.approvedCount ?? 0 })
        );
        this.reload();
      },
      error: (err) => {
        this.approving.set(false);
        this.error.set(this.locale.fromApiError(err, 'certificates.approveFailed'));
      }
    });
  }

  /** Takes the approval back so the certificate disappears from the student and parent views. */
  revokeApproval(certificate: GradeCertificateListItem): void {
    if (this.approving()) return;
    this.clearFeedback();
    this.approving.set(true);
    this.api.revokeGradeCertificateApproval(certificate.id).subscribe({
      next: () => {
        this.approving.set(false);
        this.message.set(this.locale.t('certificates.approvalRevoked'));
        this.reload();
      },
      error: (err) => {
        this.approving.set(false);
        this.error.set(this.locale.fromApiError(err, 'certificates.approveFailed'));
      }
    });
  }

  approvedCount(): number {
    return this.certificates().filter((certificate) => certificate.isApproved).length;
  }

  pendingCount(): number {
    return this.certificates().filter((certificate) => !certificate.isApproved).length;
  }

  /** Classroom subjects as form rows; subjects already on the certificate keep their settings. */
  private draftsForClassroom(classroomId: string, existing: GradeCertificateSubject[]): SubjectDraft[] {
    const room = this.classrooms().find((item) => item.id === classroomId);
    const byCourse = new Map(existing.map((subject) => [subject.courseId, subject]));
    const drafts = new Map<string, SubjectDraft>();
    for (const subject of existing) {
      drafts.set(subject.courseId, {
        courseId: subject.courseId,
        courseTitle: subject.courseTitle,
        selected: true,
        maxDegree: subject.maxDegree,
        includedInTotal: subject.includedInTotal
      });
    }
    for (const course of room?.courses ?? []) {
      if (!course.courseId || drafts.has(course.courseId)) continue;
      drafts.set(course.courseId, {
        courseId: course.courseId,
        courseTitle: course.courseTitle,
        // A brand new certificate starts with every classroom subject ticked.
        selected: byCourse.size === 0,
        maxDegree: 20,
        includedInTotal: true
      });
    }
    return [...drafts.values()];
  }

  // ---- Degrees sheet -----------------------------------------------------------------

  openSheet(certificate: GradeCertificateListItem): void {
    this.clearFeedback();
    this.api.getGradeCertificate(certificate.id).subscribe({
      next: (sheet) => {
        this.showSheet(sheet);
        document.getElementById('certificate-sheet')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'certificates.loadFailed'))
    });
  }

  closeSheet(): void {
    this.sheet.set(null);
    this.marks = {};
  }

  saveMarks(): void {
    const sheet = this.sheet();
    if (!sheet) return;
    this.clearFeedback();

    const entries = [];
    for (const student of sheet.students) {
      for (const subject of sheet.subjects) {
        const degree = this.degreeOf(student.studentId, subject.id);
        if (degree != null && (degree < 0 || degree > subject.maxDegree)) {
          this.error.set(
            this.locale.t('certificates.degreeRange', {
              student: student.studentName,
              subject: subject.courseTitle,
              max: subject.maxDegree
            })
          );
          return;
        }
        entries.push({ subjectId: subject.id, studentId: student.studentId, degree });
      }
    }

    this.saving.set(true);
    this.api.saveGradeCertificateMarks(sheet.id, entries).subscribe({
      next: (saved) => {
        this.saving.set(false);
        this.showSheet(saved);
        this.message.set(this.locale.t('certificates.marksSaved'));
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(this.locale.fromApiError(err, 'certificates.marksSaveFailed'));
      }
    });
  }

  /** Subjects that count toward the total come first, as on the printed certificate. */
  totalSubjects(sheet: GradeCertificateSheet): GradeCertificateSubject[] {
    return sheet.subjects.filter((subject) => subject.includedInTotal);
  }

  extraSubjects(sheet: GradeCertificateSheet): GradeCertificateSubject[] {
    return sheet.subjects.filter((subject) => !subject.includedInTotal);
  }

  maxTotal(sheet: GradeCertificateSheet): number {
    return this.totalSubjects(sheet).reduce((sum, subject) => sum + subject.maxDegree, 0);
  }

  /** Null until at least one counted subject has a degree. */
  studentTotal(sheet: GradeCertificateSheet, studentId: string): number | null {
    const degrees = this.totalSubjects(sheet)
      .map((subject) => this.degreeOf(studentId, subject.id))
      .filter((degree): degree is number => degree != null);
    return degrees.length ? degrees.reduce((sum, degree) => sum + degree, 0) : null;
  }

  private degreeOf(studentId: string, subjectId: string): number | null {
    const value = this.marks[studentId]?.[subjectId];
    if (value == null || (value as unknown) === '') return null;
    const degree = Number(value);
    return Number.isFinite(degree) ? degree : null;
  }

  private showSheet(sheet: GradeCertificateSheet): void {
    const marks: Record<string, Record<string, number | null>> = {};
    for (const student of sheet.students) {
      const row: Record<string, number | null> = {};
      for (const subject of sheet.subjects) row[subject.id] = null;
      for (const mark of student.marks) row[mark.subjectId] = mark.degree;
      marks[student.studentId] = row;
    }
    this.marks = marks;
    this.sheet.set(sheet);
  }

  private clearFeedback(): void {
    this.message.set('');
    this.error.set('');
  }

  // ---- Printing and image export ----------------------------------------------------

  /** Prints one certificate per page: for one student, or for the whole classroom. */
  async printCertificates(student?: GradeCertificateStudent): Promise<void> {
    const sheet = this.sheet();
    if (!sheet) return;
    const students = student ? [student] : sheet.students;
    if (!students.length) return;

    const brand = await this.loadPrintBrand();
    const models = students.map((item) => this.printModel(sheet, item, brand));
    const printed = await printCertificateModels(models, this.locale.lang() === 'ar', sheet.title);
    if (!printed) {
      this.error.set(this.locale.t('certificates.popupBlocked'));
    }
  }

  /** Downloads the certificate as a PNG image: for one student, or one image per student. */
  async exportCertificateImages(student?: GradeCertificateStudent): Promise<void> {
    const sheet = this.sheet();
    if (!sheet || this.exporting()) return;
    const students = student ? [student] : sheet.students;
    if (!students.length) return;

    this.clearFeedback();
    this.exporting.set(true);

    try {
      const brand = await this.loadPrintBrand();
      const items = students.map((item) => ({
        model: this.printModel(sheet, item, brand),
        fileName: safeCertificateFileName(`${sheet.title} - ${item.studentName}`)
      }));
      await exportCertificateModelImages(items, this.locale.lang() === 'ar');
      this.message.set(this.locale.t('certificates.exported'));
    } catch {
      this.error.set(this.locale.t('certificates.exportFailed'));
    } finally {
      this.exporting.set(false);
    }
  }

  /** Resolves one student's certificate into the shared printable model. */
  private printModel(
    sheet: GradeCertificateSheet,
    student: GradeCertificateStudent,
    brand: { name: string; logo: string | null }
  ): CertificatePrintModel {
    const counted = this.totalSubjects(sheet).map((subject) => ({
      title: subject.courseTitle,
      maxDegree: subject.maxDegree,
      degree: this.degreeOf(student.studentId, subject.id)
    }));
    const extra = this.extraSubjects(sheet).map((subject) => ({
      title: subject.courseTitle,
      maxDegree: subject.maxDegree,
      degree: this.degreeOf(student.studentId, subject.id)
    }));
    const labels = {
      subject: this.locale.t('certificates.print.subject'),
      studentName: this.locale.t('certificates.print.studentName'),
      grade: this.locale.t('certificates.print.grade'),
      maxDegree: this.locale.t('certificates.print.maxDegree'),
      degree: this.locale.t('certificates.print.degree'),
      total: this.locale.t('certificates.total')
    };
    return {
      title: sheet.title,
      studentName: student.studentName,
      gradeLine: `${this.gradeLabel(sheet.grade)} — ${sheet.classroomName}`,
      brand,
      labels,
      counted,
      extra,
      maxTotal: certificateMaxTotal(counted),
      total: certificateTotal(counted)
    };
  }/**
 * Platform name and logo from the site settings, read fresh so the certificate never shows the
 * built-in default. The logo is inlined as a data URL so it is ready when the print dialog opens.
 */
  private async loadPrintBrand(): Promise<{ name: string; logo: string | null }> {
    try {
      this.brand.apply(await firstValueFrom(this.api.getSiteSettings()));
    } catch {
      // Keep whatever the app already loaded.
    }

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
}

function blobToDataUrl(blob: Blob): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result ?? ''));
    reader.onerror = () => reject(reader.error);
    reader.readAsDataURL(blob);
  });
}
