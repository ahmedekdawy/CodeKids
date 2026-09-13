import { Component, inject, signal } from '@angular/core';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { AdminTenant } from '../../models';
import { TranslatePipe } from '../../shared/translate.pipe';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';

@Component({
  selector: 'app-admin-tenants',
  imports: [PageFeedbackComponent, TranslatePipe],
  templateUrl: './admin-tenants.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminTenantsComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  readonly rows = signal<AdminTenant[]>([]);
  readonly error = signal('');

  constructor() {
    this.reload();
  }

  reload(): void {
    this.error.set('');
    this.api.getAdminTenants().subscribe({
      next: (rows) => this.rows.set(rows ?? []),
      error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.tenants.loadFailed'))
    });
  }

  statusLabel(status: string): string {
    const key = `admin.tenants.status.${(status || '').toLowerCase()}`;
    const translated = this.locale.t(key);
    return translated === key ? status : translated;
  }

  formatUtc(value?: string | null): string {
    if (!value) return this.locale.t('common.emDash');
    return new Date(value).toLocaleString(this.locale.lang());
  }
}
