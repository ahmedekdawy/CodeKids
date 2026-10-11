import { Injectable, inject, signal } from '@angular/core';
import { LearningApiService } from './learning-api.service';
import { SiteSettings } from './models';
import { TenantStyle, normalizeTenantStyle } from './tenant-style';
import { DEFAULT_SESSION_COUNT, DEFAULT_PM_START_MINUTES, normalizeSessionCount, normalizePmStartMinutes } from './fixed-timetable.util';

@Injectable({ providedIn: 'root' })
export class SiteBrandService {
  private readonly api = inject(LearningApiService);

  readonly siteName = signal('CodeKids');
  readonly logoUrl = signal<string | null>(null);
  readonly bannerUrl = signal<string | null>(null);
  readonly amSessionCount = signal(DEFAULT_SESSION_COUNT);
  readonly pmSessionCount = signal(DEFAULT_SESSION_COUNT);
  readonly pmStartMinutes = signal(DEFAULT_PM_START_MINUTES);
  readonly tenantStyle = signal<TenantStyle>('School');
  /** True when students and teachers each have their own time zone. */
  readonly multiTimeZone = signal(false);
  /** Zone the school's timetable times are written in. */
  readonly timeZone = signal<string | null>(null);
  readonly loaded = signal(false);

  load(): void {
    this.api.getSiteSettings().subscribe({
      next: (settings) => this.apply(settings),
      error: () => this.loaded.set(true)
    });
  }

  apply(settings: SiteSettings): void {
    this.siteName.set(settings.siteName || 'CodeKids');
    this.tenantStyle.set(normalizeTenantStyle(settings.tenantStyle));
    this.multiTimeZone.set(settings.multiTimeZone === true);
    this.timeZone.set(settings.timeZone || null);
    const stamp = settings.updatedAtUtc || Date.now().toString();
    const logo = this.api.siteAssetUrl(settings.logoUrl);
    const banner = this.api.siteAssetUrl(settings.bannerUrl);
    this.logoUrl.set(logo ? `${logo}?v=${stamp}` : null);
    this.bannerUrl.set(banner ? `${banner}?v=${stamp}` : null);
    this.amSessionCount.set(normalizeSessionCount(settings.amSessionCount));
    this.pmSessionCount.set(normalizeSessionCount(settings.pmSessionCount));
    this.pmStartMinutes.set(normalizePmStartMinutes(settings.pmStartMinutes));
    document.title = this.siteName();
    this.loaded.set(true);
  }

  shortName(): string {
    const name = this.siteName().trim();
    if (!name) return 'CK';
    const parts = name.split(/\s+/).filter(Boolean);
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return name.slice(0, 2).toUpperCase();
  }
}
