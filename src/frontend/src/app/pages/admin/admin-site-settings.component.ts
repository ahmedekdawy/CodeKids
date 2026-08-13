import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { SiteSettings } from '../../models';
import { SiteBrandService } from '../../site-brand.service';
import { TranslatePipe } from '../../shared/translate.pipe';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';

@Component({
  selector: 'app-admin-site-settings',
  imports: [PageFeedbackComponent, FormsModule, TranslatePipe],
  templateUrl: './admin-site-settings.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminSiteSettingsComponent {
  private readonly api = inject(LearningApiService);
  private readonly brand = inject(SiteBrandService);
  private readonly locale = inject(LocaleService);

  readonly settings = signal<SiteSettings | null>(null);
  readonly message = signal('');
  readonly error = signal('');
  readonly saving = signal(false);
  readonly uploading = signal<'logo' | 'banner' | null>(null);

  siteName = 'CodeKids';
  timetableWeekLocal = '';
  useCurrentTimetableWeek = true;

  constructor() {
    this.reload();
  }

  reload(): void {
    this.api.getSiteSettings().subscribe({
      next: (settings) => {
        this.settings.set(settings);
        this.siteName = settings.siteName;
        this.applyTimetableWeek(settings);
        this.brand.apply(settings);
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.site.loadFailed'))
    });
  }

  logoUrl(): string | null {
    const settings = this.settings();
    const url = this.api.siteAssetUrl(settings?.logoUrl);
    return url ? `${url}?v=${settings?.updatedAtUtc || ''}` : null;
  }

  bannerUrl(): string | null {
    const settings = this.settings();
    const url = this.api.siteAssetUrl(settings?.bannerUrl);
    return url ? `${url}?v=${settings?.updatedAtUtc || ''}` : null;
  }

  onUseCurrentWeekChange(useCurrent: boolean): void {
    this.useCurrentTimetableWeek = useCurrent;
    if (!useCurrent && !this.timetableWeekLocal) {
      this.timetableWeekLocal = toDateInputValue(new Date());
    }
  }

  save(): void {
    this.message.set('');
    this.error.set('');

    if (!this.useCurrentTimetableWeek && !this.timetableWeekLocal) {
      this.error.set(this.locale.t('admin.site.timetableWeekRequired'));
      return;
    }

    this.saving.set(true);
    this.api.updateSiteSettings(this.buildSavePayload()).subscribe({
      next: (settings) => {
        this.saving.set(false);
        this.settings.set(settings);
        this.applyTimetableWeek(settings);
        this.brand.apply(settings);
        this.message.set(this.locale.t('admin.site.saved'));
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(this.locale.fromApiError(err, 'admin.site.saveFailed'));
      }
    });
  }

  onFile(kind: 'logo' | 'banner', event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    this.message.set('');
    this.error.set('');
    this.uploading.set(kind);
    this.api.uploadSiteImage(kind, file).subscribe({
      next: (settings) => {
        this.uploading.set(null);
        this.settings.set(settings);
        this.brand.apply(settings);
        this.message.set(this.locale.t(kind === 'logo' ? 'admin.site.logoUploaded' : 'admin.site.bannerUploaded'));
      },
      error: (err) => {
        this.uploading.set(null);
        this.error.set(this.locale.fromApiError(err, 'admin.site.uploadFailed'));
      }
    });
  }

  clearImage(kind: 'logo' | 'banner'): void {
    this.message.set('');
    this.error.set('');
    this.api
      .updateSiteSettings({
        ...this.buildSavePayload(),
        clearLogo: kind === 'logo',
        clearBanner: kind === 'banner'
      })
      .subscribe({
        next: (settings) => {
          this.settings.set(settings);
          this.applyTimetableWeek(settings);
          this.brand.apply(settings);
          this.message.set(this.locale.t('admin.site.saved'));
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.site.saveFailed'))
      });
  }

  private buildSavePayload(): {
    siteName: string;
    timetableWeekStartUtc?: string;
    clearTimetableWeek?: boolean;
  } {
    if (this.useCurrentTimetableWeek) {
      return {
        siteName: this.siteName.trim(),
        clearTimetableWeek: true
      };
    }

    const weekStart = startOfWeekSunday(new Date(`${this.timetableWeekLocal}T00:00:00`));
    return {
      siteName: this.siteName.trim(),
      timetableWeekStartUtc: weekStart.toISOString()
    };
  }

  private applyTimetableWeek(settings: SiteSettings): void {
    if (settings.timetableWeekStartUtc) {
      const configured = new Date(settings.timetableWeekStartUtc);
      if (!Number.isNaN(configured.getTime())) {
        this.useCurrentTimetableWeek = false;
        this.timetableWeekLocal = toDateInputValue(startOfWeekSunday(configured));
        return;
      }
    }
    this.useCurrentTimetableWeek = true;
    this.timetableWeekLocal = '';
  }
}

function startOfWeekSunday(date: Date): Date {
  const d = new Date(date);
  d.setHours(0, 0, 0, 0);
  d.setDate(d.getDate() - d.getDay());
  return d;
}

function toDateInputValue(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}
