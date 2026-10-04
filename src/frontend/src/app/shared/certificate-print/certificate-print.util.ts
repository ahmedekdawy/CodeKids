/**
 * Shared markup, styling, and small helpers for the printable grade certificate.
 * Used by the admin certificates page and the student/parent certificate viewer.
 */

import { downloadElementAsPng } from '../../export-image.util';
import { StudentGradeCertificate } from '../../models';

export interface CertificatePrintSubject {
  title: string;
  maxDegree: number;
  degree: number | null;
}

/** Fully-resolved data of one student's certificate, ready to render or print. */
export interface CertificatePrintModel {
  title: string;
  studentName: string;
  gradeLine: string;
  brand: { name: string; logo: string | null };
  labels: {
    subject: string;
    studentName: string;
    grade: string;
    maxDegree: string;
    degree: string;
    total: string;
  };
  counted: CertificatePrintSubject[];
  extra: CertificatePrintSubject[];
  maxTotal: number;
  total: number | null;
}

/** Total of the degrees that count toward the certificate total; null until one counted subject has a degree. */
export function certificateTotal(subjects: CertificatePrintSubject[]): number | null {
  const degrees = subjects
    .filter((subject) => subject.degree != null)
    .map((subject) => subject.degree as number);
  return degrees.length ? degrees.reduce((sum, degree) => sum + degree, 0) : null;
}

/** Sum of the maximum degrees of the subjects that count toward the total. */
export function certificateMaxTotal(subjects: CertificatePrintSubject[]): number {
  return subjects.reduce((sum, subject) => sum + subject.maxDegree, 0);
}

export function formatDegree(value: number): string {
  return String(Math.round(value * 100) / 100);
}

function cell(value: number | null): string {
  return value == null ? '' : formatDegree(value);
}

export function escapeHtml(value: string): string {
  return (value ?? '').replace(/[&<>"']/g, (ch) => `&#${ch.charCodeAt(0)};`);
}

/** One student's certificate markup, shared by printing, image export, and the on-page viewer. */
export function certificateHtml(model: CertificatePrintModel): string {
  const logoHtml = model.brand.logo
    ? `<img class="logo" src="${escapeHtml(model.brand.logo)}" alt="">`
    : '';

  const head = [
    `<th>${escapeHtml(model.labels.subject)}</th>`,
    ...model.counted.map((subject) => `<th>${escapeHtml(subject.title)}</th>`),
    `<th>${escapeHtml(model.labels.total)}</th>`,
    ...model.extra.map((subject) => `<th>${escapeHtml(subject.title)}</th>`)
  ].join('');
  const max = [
    `<td>${escapeHtml(model.labels.maxDegree)}</td>`,
    ...model.counted.map((subject) => `<td>${formatDegree(subject.maxDegree)}</td>`),
    `<td>${formatDegree(model.maxTotal)}</td>`,
    ...model.extra.map((subject) => `<td>${formatDegree(subject.maxDegree)}</td>`)
  ].join('');
  const degrees = [
    `<td>${escapeHtml(model.labels.degree)}</td>`,
    ...model.counted.map((subject) => `<td>${cell(subject.degree)}</td>`),
    `<td>${cell(model.total)}</td>`,
    ...model.extra.map((subject) => `<td>${cell(subject.degree)}</td>`)
  ].join('');

  return `
<section class="gc-cert">
  <div class="frame">
    <div class="frame-inner">
      <div class="top">
        <div class="brand">${logoHtml}<span>${escapeHtml(model.brand.name)}</span></div>
        <h1>${escapeHtml(model.title)}</h1>
      </div>
      <div class="who">
        <p><b>${escapeHtml(model.labels.studentName)} :</b> ${escapeHtml(model.studentName)}</p>
        <p><b>${escapeHtml(model.labels.grade)} :</b> ${escapeHtml(model.gradeLine)}</p>
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

/** Resolves approved certificates of one student into printable models, newest approval first. */
export function studentCertificatePrintModels(
  certificates: StudentGradeCertificate[],
  studentName: string,
  gradeLabelText: string,
  brand: { name: string; logo: string | null },
  labels: CertificatePrintModel['labels']
): CertificatePrintModel[] {
  return certificates.map((cert) => {
    const degrees = new Map(cert.marks.map((mark) => [mark.subjectId, mark.degree]));
    const ordered = [...cert.subjects].sort((a, b) => a.sortOrder - b.sortOrder);
    const toSubject = (subject: typeof ordered[number]): CertificatePrintSubject => ({
      title: subject.courseTitle,
      maxDegree: subject.maxDegree,
      degree: degrees.get(subject.id) ?? null
    });
    const counted = ordered.filter((subject) => subject.includedInTotal).map(toSubject);
    const extra = ordered.filter((subject) => !subject.includedInTotal).map(toSubject);
    return {
      title: cert.title,
      studentName,
      gradeLine: `${gradeLabelText} — ${cert.classroomName}`,
      brand,
      labels,
      counted,
      extra,
      maxTotal: certificateMaxTotal(counted),
      total: certificateTotal(counted)
    };
  });
}

/** Translated column labels for the certificate table. */
export function defaultCertificateLabels(t: (key: string) => string): CertificatePrintModel['labels'] {
  return {
    subject: t('certificates.print.subject'),
    studentName: t('certificates.print.studentName'),
    grade: t('certificates.print.grade'),
    maxDegree: t('certificates.print.maxDegree'),
    degree: t('certificates.print.degree'),
    total: t('certificates.total')
  };
}

/** Prints one certificate per A4-landscape page. Returns false when the pop-up was blocked. */
export async function printCertificateModels(
  models: CertificatePrintModel[],
  rtl: boolean,
  documentTitle: string
): Promise<boolean> {
  if (!models.length) return true;
  const win = window.open('', '_blank');
  if (!win) return false;

  const pages = models.map(certificateHtml).join('');
  win.document.write(`<!DOCTYPE html>
<html dir="${rtl ? 'rtl' : 'ltr'}" lang="${rtl ? 'ar' : 'en'}">
<head>
  <meta charset="utf-8">
  <title>${escapeHtml(documentTitle)}</title>
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
  return true;
}

/** Downloads one PNG image per certificate, drawn off-screen at A4-landscape width. */
export async function exportCertificateModelImages(
  items: { model: CertificatePrintModel; fileName: string }[],
  rtl: boolean
): Promise<void> {
  if (!items.length) return;
  const host = document.createElement('div');
  host.dir = rtl ? 'rtl' : 'ltr';
  host.style.cssText = 'position:fixed;left:-20000px;top:0;width:1123px;background:#ffffff;';
  host.innerHTML = `<style>${CERTIFICATE_CSS}</style>`;
  document.body.appendChild(host);

  try {
    for (const item of items) {
      const holder = document.createElement('div');
      holder.innerHTML = certificateHtml(item.model);
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
        item.fileName,
        { backgroundColor: '#ffffff' }
      );
      holder.remove();
    }
  } finally {
    host.remove();
  }
}

export function safeCertificateFileName(value: string): string {
  return value.replace(/[\\/:*?"<>|]+/g, ' ').replace(/\s+/g, ' ').trim() || 'certificate';
}

/**
 * Injects the certificate stylesheet into the document head once, so markup inserted via
 * innerHTML (the on-page preview) is styled exactly like the print window and the export image.
 */
export function ensureCertificateStyles(): void {
  if (document.getElementById('gc-cert-styles')) return;
  const style = document.createElement('style');
  style.id = 'gc-cert-styles';
  style.textContent = CERTIFICATE_CSS;
  document.head.appendChild(style);
}

/** Certificate look, scoped to .gc-cert so it works in print windows, exports, and inside the app page. */
export const CERTIFICATE_CSS = `
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
