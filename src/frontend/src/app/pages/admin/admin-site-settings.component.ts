import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { SiteSettings } from '../../models';
import { SiteBrandService } from '../../site-brand.service';
import { TranslatePipe } from '../../shared/translate.pipe';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';
import {
  DEFAULT_SESSION_COUNT,
  DEFAULT_PM_START_MINUTES,
  MAX_SESSION_COUNT,
  MIN_SESSION_COUNT,
  MAX_PM_START_MINUTES,
  MIN_PM_START_MINUTES,
  minutesToTimeInput,
  normalizePmStartMinutes,
  normalizeSessionCount,
  timeInputToMinutes
} from '../../fixed-timetable.util';

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
  amSessionCount = DEFAULT_SESSION_COUNT;
  pmSessionCount = DEFAULT_SESSION_COUNT;
  pmStartTime = minutesToTimeInput(DEFAULT_PM_START_MINUTES);
  readonly minSessionCount = MIN_SESSION_COUNT;
  readonly maxSessionCount = MAX_SESSION_COUNT;
  readonly minPmStartTime = minutesToTimeInput(MIN_PM_START_MINUTES);
  readonly maxPmStartTime = minutesToTimeInput(MAX_PM_START_MINUTES);

  constructor() {
    this.reload();
  }

  reload(): void {
    this.api.getSiteSettings().subscribe({
      next: (settings) => {
        this.settings.set(settings);
        this.siteName = settings.siteName;
        this.applyTimetableWeek(settings);
        this.applySessionCounts(settings);
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

    if (!this.sessionCountsValid()) {
      this.error.set(
        this.locale.t('admin.site.sessionCountInvalid', {
          min: MIN_SESSION_COUNT,
          max: MAX_SESSION_COUNT
        })
      );
      return;
    }

    if (!this.pmStartValid()) {
      this.error.set(
        this.locale.t('admin.site.pmStartInvalid', {
          min: this.minPmStartTime,
          max: this.maxPmStartTime
        })
      );
      return;
    }

    this.saving.set(true);
    this.api.updateSiteSettings(this.buildSavePayload()).subscribe({
      next: (settings) => {
        this.saving.set(false);
        this.settings.set(settings);
        this.applyTimetableWeek(settings);
        this.applySessionCounts(settings);
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
          this.applySessionCounts(settings);
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
    amSessionCount: number;
    pmSessionCount: number;
    pmStartMinutes: number;
  } {
    const counts = {
      amSessionCount: normalizeSessionCount(this.amSessionCount),
      pmSessionCount: normalizeSessionCount(this.pmSessionCount),
      pmStartMinutes: timeInputToMinutes(this.pmStartTime)
    };
    if (this.useCurrentTimetableWeek) {
      return {
        siteName: this.siteName.trim(),
        clearTimetableWeek: true,
        ...counts
      };
    }

    const weekStart = startOfWeekSunday(new Date(`${this.timetableWeekLocal}T00:00:00`));
    return {
      siteName: this.siteName.trim(),
      timetableWeekStartUtc: weekStart.toISOString(),
      ...counts
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

  private applySessionCounts(settings: SiteSettings): void {
    this.amSessionCount = normalizeSessionCount(settings.amSessionCount);
    this.pmSessionCount = normalizeSessionCount(settings.pmSessionCount);
    this.pmStartTime = minutesToTimeInput(normalizePmStartMinutes(settings.pmStartMinutes));
  }

  private sessionCountsValid(): boolean {
    const am = Number(this.amSessionCount);
    const pm = Number(this.pmSessionCount);
    return (
      Number.isInteger(am) &&
      Number.isInteger(pm) &&
      am >= MIN_SESSION_COUNT &&
      am <= MAX_SESSION_COUNT &&
      pm >= MIN_SESSION_COUNT &&
      pm <= MAX_SESSION_COUNT
    );
  }

  private pmStartValid(): boolean {
    const match = /^(\d{1,2}):(\d{2})$/.exec((this.pmStartTime ?? '').trim());
    if (!match) return false;
    const minutes = Number(match[1]) * 60 + Number(match[2]);
    return minutes >= MIN_PM_START_MINUTES && minutes <= MAX_PM_START_MINUTES;
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
