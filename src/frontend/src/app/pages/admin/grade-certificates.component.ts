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
import { downloadElementAsPng } from '../../export-image.util';
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

    const win = window.open('', '_blank');
    if (!win) {
      this.error.set(this.locale.t('certificates.popupBlocked'));
      return;
    }

    // The window is opened first (inside the click) so pop-up blockers allow it.
    const brand = await this.loadPrintBrand();
    const rtl = this.locale.lang() === 'ar';
    const pages = students.map((item) => this.certificateHtml(sheet, item, brand)).join('');

    win.document.write(`<!DOCTYPE html>
<html dir="${rtl ? 'rtl' : 'ltr'}" lang="${rtl ? 'ar' : 'en'}">
<head>
  <meta charset="utf-8">
  <title>${escapeHtml(sheet.title)}</title>
  <style>
    @page { size: A4 landscape; margin: 10mm; }
    body { margin: 0; }
    .gc-cert { page-break-after: always; break-after: page; }
    .gc-cert:last-child { page-break-after: auto; break-after: auto; }
    ${CERTIFICATE_CSS}
  </style>
</head>
<body>${pages}</body>
</html>`);
    win.document.close();
    win.focus();
    setTimeout(() => win.print(), 300);
  }

  /** Downloads the certificate as a PNG image: for one student, or one image per student. */
  async exportCertificateImages(student?: GradeCertificateStudent): Promise<void> {
    const sheet = this.sheet();
    if (!sheet || this.exporting()) return;
    const students = student ? [student] : sheet.students;
    if (!students.length) return;

    this.clearFeedback();
    this.exporting.set(true);

    // Certificates are drawn off-screen at A4-landscape width, then captured one by one.
    const host = document.createElement('div');
    host.dir = this.locale.lang() === 'ar' ? 'rtl' : 'ltr';
    host.style.cssText = 'position:fixed;left:-20000px;top:0;width:1123px;background:#ffffff;';
    host.innerHTML = `<style>${CERTIFICATE_CSS}</style>`;
    document.body.appendChild(host);

    try {
      const brand = await this.loadPrintBrand();
      for (const item of students) {
        const holder = document.createElement('div');
        holder.innerHTML = this.certificateHtml(sheet, item, brand);
        host.appendChild(holder);
        const logo = holder.querySelector('img');
        if (logo && !logo.complete) {
          await new Promise<void>((resolve) => {
            logo.onload = () => resolve();
            logo.onerror = () => resolve();
          });
        }
        await downloadElementAsPng(
          holder.firstElementChild as HTMLElement,
          safeFileName(`${sheet.title} - ${item.studentName}`),
          { backgroundColor: '#ffffff' }
        );
        holder.remove();
      }
      this.message.set(this.locale.t('certificates.exported'));
    } catch {
      this.error.set(this.locale.t('certificates.exportFailed'));
    } finally {
      host.remove();
      this.exporting.set(false);
    }
  }

  /** One student's certificate markup, shared by printing and image export. */
  private certificateHtml(
    sheet: GradeCertificateSheet,
    student: GradeCertificateStudent,
    brand: { name: string; logo: string | null }
  ): string {
    const counted = this.totalSubjects(sheet);
    const extra = this.extraSubjects(sheet);
    const t = (key: string) => escapeHtml(this.locale.t(key));
    const cell = (value: number | null) => (value == null ? '' : formatDegree(value));
    const logoHtml = brand.logo ? `<img class="logo" src="${escapeHtml(brand.logo)}" alt="">` : '';

    const head = [
      `<th>${t('certificates.print.subject')}</th>`,
      ...counted.map((subject) => `<th>${escapeHtml(subject.courseTitle)}</th>`),
      `<th>${t('certificates.total')}</th>`,
      ...extra.map((subject) => `<th>${escapeHtml(subject.courseTitle)}</th>`)
    ].join('');
    const max = [
      `<td>${t('certificates.print.maxDegree')}</td>`,
      ...counted.map((subject) => `<td>${formatDegree(subject.maxDegree)}</td>`),
      `<td>${formatDegree(this.maxTotal(sheet))}</td>`,
      ...extra.map((subject) => `<td>${formatDegree(subject.maxDegree)}</td>`)
    ].join('');
    const degrees = [
      `<td>${t('certificates.print.degree')}</td>`,
      ...counted.map((subject) => `<td>${cell(this.degreeOf(student.studentId, subject.id))}</td>`),
      `<td>${cell(this.studentTotal(sheet, student.studentId))}</td>`,
      ...extra.map((subject) => `<td>${cell(this.degreeOf(student.studentId, subject.id))}</td>`)
    ].join('');

    return `
<section class="gc-cert">
  <div class="frame">
    <div class="frame-inner">
      <div class="top">
        <div class="brand">${logoHtml}<span>${escapeHtml(brand.name)}</span></div>
        <h1>${escapeHtml(sheet.title)}</h1>
      </div>
      <div class="who">
        <p><b>${t('certificates.print.studentName')} :</b> ${escapeHtml(student.studentName)}</p>
        <p><b>${t('certificates.print.grade')} :</b> ${escapeHtml(this.gradeLabel(sheet.grade))} — ${escapeHtml(sheet.classroomName)}</p>
      </div>
      <table>
        <tr class="head">${head}</tr>
        <tr class="max">${max}</tr>
        <tr class="degree">${degrees}</tr>
      </table>
    </div>
  </div>
</section>`;
  }

  /**
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

/** Certificate look, scoped to .gc-cert so it works both in the print window and inside the app page. */
const CERTIFICATE_CSS = `
    .gc-cert, .gc-cert * { box-sizing: border-box; -webkit-print-color-adjust: exact; print-color-adjust: exact; }
    .gc-cert { padding: 4mm; background: #ffffff; color: #1c1c1c; font-family: 'Cairo', 'Segoe UI', Tahoma, Arial, sans-serif; }
    .gc-cert .frame { border: 10px double #b8862e; padding: 5px; }
    .gc-cert .frame-inner { border: 3px solid #7a1f1f; padding: 12mm 12mm 14mm; min-height: 160mm; }
    .gc-cert .top { display: flex; align-items: center; gap: 12mm; }
    .gc-cert .brand { display: flex; flex-direction: column; align-items: center; gap: 2mm; font-weight: 800; font-size: 13pt; min-width: 30mm; }
    .gc-cert .logo { max-width: 34mm; max-height: 34mm; object-fit: contain; }
    .gc-cert h1 { flex: 1; margin: 0; text-align: center; font-size: 22pt; color: #7a1f1f; }
    .gc-cert .who { display: flex; flex-wrap: wrap; justify-content: space-between; gap: 6mm 20mm; margin: 12mm 0 8mm; font-size: 16pt; }
    .gc-cert .who p { margin: 0; }
    .gc-cert table { width: 100%; border-collapse: collapse; table-layout: fixed; }
    .gc-cert th, .gc-cert td { border: 1.5px solid #222; padding: 4mm 2mm; text-align: center; font-size: 15pt; font-weight: 700; color: #1c1c1c; }
    .gc-cert tr.head th { background: #8fce4a; color: #7a1f1f; }
    .gc-cert tr.max td { background: #ffff00; color: #7a1f1f; }
    .gc-cert tr.degree td { background: #e6e6e6; }
`;

function safeFileName(value: string): string {
  return value.replace(/[\\/:*?"<>|]+/g, ' ').replace(/\s+/g, ' ').trim() || 'certificate';
}

function blobToDataUrl(blob: Blob): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result ?? ''));
    reader.onerror = () => reject(reader.error);
    reader.readAsDataURL(blob);
  });
}

function escapeHtml(value: string): string {
  return (value ?? '').replace(/[&<>"']/g, (ch) => `&#${ch.charCodeAt(0)};`);
}

function formatDegree(value: number): string {
  return String(Math.round(value * 100) / 100);
}
