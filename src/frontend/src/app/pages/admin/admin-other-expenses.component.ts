import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { OtherExpense } from '../../models';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';

@Component({
  selector: 'app-admin-other-expenses',
  imports: [PageFeedbackComponent, FormsModule, IconActionButtonComponent, TranslatePipe],
  templateUrl: './admin-other-expenses.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminOtherExpensesComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  readonly rows = signal<OtherExpense[]>([]);
  readonly message = signal('');
  readonly error = signal('');

  name = '';
  amount: number | null = null;
  expenseDate = todayLocal();
  notes = '';

  filterName = '';
  filterFromDate = startOfMonthLocal();
  filterToDate = endOfMonthLocal();

  readonly filteredTotal = computed(() =>
    this.rows().reduce((sum, row) => sum + (row.amount ?? 0), 0)
  );

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
    this.filterName = '';
    this.filterFromDate = startOfMonthLocal();
    this.filterToDate = endOfMonthLocal();
    this.reload();
  }

  reload(): void {
    this.clearStatus();
    this.api
      .getOtherExpenses({
        fromDate: this.filterFromDate || undefined,
        toDate: this.filterToDate || undefined,
        name: this.filterName.trim() || undefined
      })
      .subscribe({
        next: (rows) => this.rows.set(rows ?? []),
        error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.expenses.loadFailed'))
      });
  }

  create(): void {
    this.clearStatus();
    const amount = Number(this.amount);
    if (!this.name.trim() || !this.expenseDate || !Number.isFinite(amount) || amount <= 0) {
      this.error.set(this.locale.t('admin.expenses.requiredFields'));
      return;
    }

    this.api
      .createOtherExpense({
        name: this.name.trim(),
        amount,
        expenseDate: this.expenseDate,
        notes: this.notes.trim() || null
      })
      .subscribe({
        next: () => {
          this.message.set(this.locale.t('admin.expenses.created'));
          this.name = '';
          this.amount = null;
          this.notes = '';
          this.expenseDate = todayLocal();
          this.reload();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.expenses.createFailed'))
      });
  }

  remove(row: OtherExpense): void {
    if (!confirm(this.locale.t('admin.expenses.confirmDelete', { label: row.name }))) return;
    this.clearStatus();
    this.api.deleteOtherExpense(row.id).subscribe({
      next: () => {
        this.message.set(this.locale.t('admin.expenses.deleted'));
        this.reload();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.expenses.deleteFailed'))
    });
  }

  private clearStatus(): void {
    this.message.set('');
    this.error.set('');
  }
}

function todayLocal(): string {
  return toLocalDateString(new Date());
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
