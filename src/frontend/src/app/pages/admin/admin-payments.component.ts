import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { ManagedUser, TuitionPayment } from '../../models';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';

type PayerKind = 'parent' | 'student';

@Component({
  selector: 'app-admin-payments',
  imports: [SearchableSelectComponent, FormsModule, IconActionButtonComponent, TranslatePipe],
  templateUrl: './admin-payments.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminPaymentsComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  readonly parents = signal<ManagedUser[]>([]);
  readonly students = signal<ManagedUser[]>([]);
  readonly rows = signal<TuitionPayment[]>([]);
  readonly message = signal('');
  readonly error = signal('');

  readonly months = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
  readonly years = yearOptions();

  payerKind: PayerKind = 'parent';
  parentId = '';
  studentId = '';
  year = new Date().getFullYear();
  month = new Date().getMonth() + 1;
  amount: number | null = null;
  paymentDate = todayLocal();
  notes = '';

  filterPayerKind: '' | PayerKind = '';
  filterParentId = '';
  filterStudentId = '';
  filterFromDate = startOfMonthLocal();
  filterToDate = endOfMonthLocal();

  readonly orphanStudents = computed(() =>
    this.students()
      .filter((s) => !s.parentId)
      .slice()
      .sort((a, b) => a.displayName.localeCompare(b.displayName))
  );

  readonly filteredTotal = computed(() =>
    this.rows().reduce((sum, row) => sum + (row.amount ?? 0), 0)
  );

  constructor() {
    this.api.getUsers('Parent').subscribe({
      next: (users) => this.parents.set(users ?? []),
      error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.payments.loadFailed'))
    });
    this.api.getUsers('Student').subscribe({
      next: (users) => this.students.set(users ?? []),
      error: () => undefined
    });
    this.reload();
  }

  monthLabel(month: number): string {
    return this.locale.t(`admin.payments.month${month}`);
  }

  moneyLabel(value: number): string {
    return value.toLocaleString(undefined, {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    });
  }

  onPayerKindChange(kind: PayerKind): void {
    this.payerKind = kind;
    this.parentId = '';
    this.studentId = '';
  }

  onFilterPayerKindChange(kind: '' | PayerKind): void {
    this.filterPayerKind = kind;
    this.filterParentId = '';
    this.filterStudentId = '';
    this.reload();
  }

  resetFilters(): void {
    this.filterPayerKind = '';
    this.filterParentId = '';
    this.filterStudentId = '';
    this.filterFromDate = startOfMonthLocal();
    this.filterToDate = endOfMonthLocal();
    this.reload();
  }

  reload(): void {
    this.clearStatus();
    this.api
      .getTuitionPayments({
        parentId: this.filterParentId || undefined,
        studentId: this.filterStudentId || undefined,
        fromDate: this.filterFromDate || undefined,
        toDate: this.filterToDate || undefined
      })
      .subscribe({
        next: (rows) => this.rows.set(rows ?? []),
        error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.payments.loadFailed'))
      });
  }

  create(): void {
    this.clearStatus();
    const amount = Number(this.amount);
    if (
      (this.payerKind === 'parent' && !this.parentId) ||
      (this.payerKind === 'student' && !this.studentId) ||
      !this.year ||
      !this.month ||
      !this.paymentDate ||
      !Number.isFinite(amount) ||
      amount <= 0
    ) {
      this.error.set(this.locale.t('admin.payments.requiredFields'));
      return;
    }

    this.api
      .createTuitionPayment({
        parentId: this.payerKind === 'parent' ? this.parentId : null,
        studentId: this.payerKind === 'student' ? this.studentId : null,
        year: Number(this.year),
        month: Number(this.month),
        amount,
        paymentDate: this.paymentDate,
        notes: this.notes.trim() || null
      })
      .subscribe({
        next: () => {
          this.message.set(this.locale.t('admin.payments.created'));
          this.amount = null;
          this.notes = '';
          this.paymentDate = todayLocal();
          this.reload();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.payments.createFailed'))
      });
  }

  remove(row: TuitionPayment): void {
    const label = `${row.payerLabel} — ${row.year}/${row.month}`;
    if (!confirm(this.locale.t('admin.payments.confirmDelete', { label }))) return;
    this.clearStatus();
    this.api.deleteTuitionPayment(row.id).subscribe({
      next: () => {
        this.message.set(this.locale.t('admin.payments.deleted'));
        this.reload();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.payments.deleteFailed'))
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

function yearOptions(): number[] {
  const current = new Date().getFullYear();
  return Array.from({ length: 8 }, (_, i) => current - 3 + i);
}
