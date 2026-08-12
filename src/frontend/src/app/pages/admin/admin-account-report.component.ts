import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { AccountReport } from '../../models';
import { TranslatePipe } from '../../shared/translate.pipe';

@Component({
  selector: 'app-admin-account-report',
  imports: [FormsModule, TranslatePipe],
  templateUrl: './admin-account-report.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminAccountReportComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  readonly report = signal<AccountReport | null>(null);
  readonly error = signal('');

  filterFromDate = startOfMonthLocal();
  filterToDate = endOfMonthLocal();

  constructor() {
    this.reload();
  }

  moneyLabel(value: number): string {
    return value.toLocaleString(undefined, {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    });
  }

  resetFilters(): void {
    this.filterFromDate = startOfMonthLocal();
    this.filterToDate = endOfMonthLocal();
    this.reload();
  }

  reload(): void {
    this.error.set('');
    if (!this.filterFromDate || !this.filterToDate) {
      this.error.set(this.locale.t('admin.accountReport.dateRequired'));
      return;
    }

    this.api
      .getAccountReport({
        fromDate: this.filterFromDate,
        toDate: this.filterToDate
      })
      .subscribe({
        next: (report) => this.report.set(report),
        error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.accountReport.loadFailed'))
      });
  }
}

function startOfMonthLocal(): string {
  const d = new Date();
  return toLocalDateString(new Date(d.getFullYear(), d.getMonth(), 1));
}

function endOfMonthLocal(): string {
  const d = new Date();
  return toLocalDateString(new Date(d.getFullYear(), d.getMonth() + 1, 0));
}

function toLocalDateString(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}
