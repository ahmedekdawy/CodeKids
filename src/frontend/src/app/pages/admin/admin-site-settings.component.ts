import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { SiteSettings } from '../../models';
import { SiteBrandService } from '../../site-brand.service';
import { TranslatePipe } from '../../shared/translate.pipe';

@Component({
  selector: 'app-admin-site-settings',
  imports: [FormsModule, TranslatePipe],
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

  constructor() {
    this.reload();
  }

  reload(): void {
    this.api.getSiteSettings().subscribe({
      next: (settings) => {
        this.settings.set(settings);
        this.siteName = settings.siteName;
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

  save(): void {
    this.message.set('');
    this.error.set('');
    this.saving.set(true);
    this.api.updateSiteSettings({ siteName: this.siteName }).subscribe({
      next: (settings) => {
        this.saving.set(false);
        this.settings.set(settings);
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
        siteName: this.siteName,
        clearLogo: kind === 'logo',
        clearBanner: kind === 'banner'
      })
      .subscribe({
        next: (settings) => {
          this.settings.set(settings);
          this.brand.apply(settings);
          this.message.set(this.locale.t('admin.site.saved'));
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.site.saveFailed'))
      });
  }
}
