import { Component, EventEmitter, Input, OnChanges, Output, inject, signal } from '@angular/core';
import { LearningApiService } from '../../learning-api.service';
import { AssessmentStudentLink } from '../../models';
import { LocaleService } from '../../i18n/locale.service';
import { TranslatePipe } from '../../shared/translate.pipe';
import { assessmentWhatsAppShareUrl } from './assessment-whatsapp-share';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';

@Component({
  selector: 'app-assessment-student-links-dialog',
  standalone: true,
  imports: [TranslatePipe, IconActionButtonComponent],
  templateUrl: './assessment-student-links-dialog.component.html',
  styleUrl: './assessment-student-links-dialog.component.css'
})
export class AssessmentStudentLinksDialogComponent implements OnChanges {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  @Input({ required: true }) kind!: 'exam' | 'quiz' | 'assignment';
  @Input({ required: true }) resourceId!: string;
  @Input({ required: true }) resourceTitle!: string;
  @Input() gradeLabel: string | null = null;
  @Input() courseLabel: string | null = null;
  @Input() kindLabel = '';

  @Output() readonly closed = new EventEmitter<void>();

  readonly loading = signal(false);
  readonly error = signal('');
  readonly info = signal('');
  readonly links = signal<AssessmentStudentLink[]>([]);

  ngOnChanges(): void {
    if (this.resourceId && this.kind) {
      this.load();
    }
  }

  close(): void {
    this.closed.emit();
  }

  /** Public play path for logged-in students (same for the whole class). */
  generalPath(): string {
    const segment =
      this.kind === 'exam' ? 'exams' : this.kind === 'quiz' ? 'quizzes' : 'assignments';
    return `/${segment}/${this.resourceId}`;
  }

  copyGeneralLink(): void {
    const url = `${window.location.origin}${this.generalPath()}`;
    void navigator.clipboard?.writeText(url).then(
      () => {
        this.error.set('');
        this.info.set(this.locale.t('teacher.assessments.generalLinkCopied'));
      },
      () => this.error.set(this.locale.t('teacher.assessments.copyGeneralLinkFailed'))
    );
  }

  shareGeneralWhatsApp(): void {
    window.open(this.whatsAppUrlForPath(this.generalPath()), '_blank', 'noopener');
  }

  copyLink(link: AssessmentStudentLink): void {
    const url = `${window.location.origin}${link.path}`;
    void navigator.clipboard?.writeText(url).then(
      () => {
        this.error.set('');
        this.info.set(
          this.locale.t('teacher.assessments.studentLinkCopiedNamed', { name: link.displayName })
        );
      },
      () => this.error.set(this.locale.t('teacher.assessments.copyStudentLinkFailed'))
    );
  }

  shareWhatsApp(link: AssessmentStudentLink): void {
    window.open(this.whatsAppUrlForPath(link.path), '_blank', 'noopener');
  }

  private whatsAppUrlForPath(path: string): string {
    return assessmentWhatsAppShareUrl({
      kindLabel: this.kindLabel || this.resourceTitle,
      name: this.resourceTitle,
      gradeLabel: this.gradeLabel,
      courseLabel: this.courseLabel,
      studentPath: path,
      gradeCaption: this.locale.t('teacher.assessments.whatsAppGrade'),
      courseCaption: this.locale.t('teacher.assessments.whatsAppCourse')
    });
  }

  private load(): void {
    this.loading.set(true);
    this.error.set('');
    this.info.set('');
    this.api.getAssessmentStudentLinks(this.kind, this.resourceId).subscribe({
      next: (result) => {
        this.links.set(result.links ?? []);
        this.loading.set(false);
        if (!(result.links?.length > 0)) {
          this.info.set(this.locale.t('teacher.assessments.noStudentLinks'));
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.links.set([]);
        this.error.set(this.locale.fromApiError(err, 'teacher.assessments.loadStudentLinksFailed'));
      }
    });
  }
}
