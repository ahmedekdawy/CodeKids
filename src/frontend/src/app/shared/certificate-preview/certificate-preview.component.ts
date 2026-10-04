import {
  AfterViewInit,
  Component,
  ElementRef,
  OnChanges,
  OnDestroy,
  ViewChild,
  inject,
  input,
  signal
} from '@angular/core';
import { LocaleService } from '../../i18n/locale.service';
import { SiteBrandService } from '../../site-brand.service';
import { StudentGradeCertificate } from '../../models';
import {
  certificateHtml,
  defaultCertificateLabels,
  ensureCertificateStyles,
  studentCertificatePrintModels
} from '../certificate-print/certificate-print.util';
import { SafeHtmlPipe } from '../safe-html.pipe';

/** A4 landscape at 96dpi — the width the export image is drawn at. */
const CERTIFICATE_WIDTH = 1123;

/**
 * On-page preview of an approved certificate for the student and parent views.
 * Reuses the exact markup and styling of the printed/exported certificate, scaled
 * down proportionally to fit the available width so it matches the saved image.
 */
@Component({
  selector: 'app-certificate-preview',
  standalone: true,
  imports: [SafeHtmlPipe],
  template: `
    <div class="gc-preview-frame" #frame [style.height.px]="frameHeight()">
      <div
        class="gc-preview-scale"
        #scaled
        [style.transform]="'scale(' + scale() + ')'"
        [innerHTML]="html() | safeHtml"
      ></div>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
      }

      .gc-preview-frame {
        overflow: hidden;
        width: 100%;
      }

      .gc-preview-scale {
        width: ${CERTIFICATE_WIDTH}px;
        transform-origin: top left;
      }
    `
  ]
})
export class CertificatePreviewComponent implements OnChanges, AfterViewInit, OnDestroy {
  readonly certificate = input.required<StudentGradeCertificate>();
  readonly studentName = input('');
  readonly gradeLabelText = input('');

  readonly html = signal('');
  readonly scale = signal(1);
  readonly frameHeight = signal(0);

  @ViewChild('frame') private frame?: ElementRef<HTMLDivElement>;
  @ViewChild('scaled') private scaled?: ElementRef<HTMLDivElement>;

  private readonly brand = inject(SiteBrandService);
  private readonly locale = inject(LocaleService);
  private observer: ResizeObserver | null = null;

  ngOnChanges(): void {
    ensureCertificateStyles();
    this.rebuild();
  }

  ngAfterViewInit(): void {
    this.layout();
    if (typeof ResizeObserver !== 'undefined') {
      this.observer = new ResizeObserver(() => this.layout());
      if (this.frame) this.observer.observe(this.frame.nativeElement);
      if (this.scaled) this.observer.observe(this.scaled.nativeElement);
    }
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }

  private rebuild(): void {
    const certificate = this.certificate();
    if (!certificate) {
      this.html.set('');
      return;
    }

    const [model] = studentCertificatePrintModels(
      [certificate],
      this.studentName(),
      this.gradeLabelText(),
      { name: this.brand.siteName(), logo: this.brand.logoUrl() },
      defaultCertificateLabels((key) => this.locale.t(key))
    );
    this.html.set(certificateHtml(model));

    // Re-measure after the new markup has been painted; the resize observer covers late changes.
    queueMicrotask(() => this.layout());
  }

  private layout(): void {
    const frame = this.frame?.nativeElement;
    const scaled = this.scaled?.nativeElement;
    if (!frame || !scaled) return;

    const available = frame.clientWidth;
    const scale = available > 0 ? Math.min(1, available / CERTIFICATE_WIDTH) : 1;
    this.scale.set(scale);
    this.frameHeight.set(Math.ceil(scaled.scrollHeight * scale));
  }
}
