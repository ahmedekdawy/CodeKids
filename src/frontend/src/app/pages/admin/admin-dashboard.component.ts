import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { AdminLoginDashboard, AdminLoginDashboardDay, AdminLoginUser } from '../../models';
import { TranslatePipe } from '../../shared/translate.pipe';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';

type LoginRole = 'teachers' | 'parents' | 'students';

@Component({
  selector: 'app-admin-dashboard',
  imports: [PageFeedbackComponent, FormsModule, TranslatePipe],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminDashboardComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  readonly report = signal<AdminLoginDashboard | null>(null);
  readonly error = signal('');
  readonly selectedRole = signal<LoginRole>('students');

  filterFromDate = startOfMonthLocal();
  filterToDate = endOfMonthLocal();

  readonly maxDayCount = computed(() => {
    const days = this.report()?.days ?? [];
    let max = 0;
    for (const day of days) {
      max = Math.max(max, day.teachers, day.parents, day.students);
    }
    return Math.max(1, max);
  });

  readonly hasLogins = computed(() => {
    const r = this.report();
    if (!r) return false;
    return r.teacherCount + r.parentCount + r.studentCount > 0;
  });

  readonly loggedInUsers = computed<AdminLoginUser[]>(() => {
    const r = this.report();
    if (!r) return [];
    const role = this.selectedRole();
    if (role === 'teachers') return r.teachers ?? [];
    if (role === 'parents') return r.parents ?? [];
    return r.students ?? [];
  });

  readonly loggedInTitleKey = computed(() => {
    const role = this.selectedRole();
    if (role === 'teachers') return 'admin.dashboard.loggedInTeachers';
    if (role === 'parents') return 'admin.dashboard.loggedInParents';
    return 'admin.dashboard.loggedInStudents';
  });

  constructor() {
    this.reload();
  }

  selectRole(role: LoginRole): void {
    this.selectedRole.set(role);
  }

  formatLogin(iso: string): string {
    this.locale.lang();
    return new Date(iso).toLocaleString(this.locale.lang());
  }

  barHeight(count: number): string {
    return `${(count / this.maxDayCount()) * 100}%`;
  }

  dayLabel(date: string): string {
    const parts = date.split('-');
    if (parts.length !== 3) return date;
    return String(Number(parts[2]));
  }

  showDayLabel(index: number, days: AdminLoginDashboardDay[]): boolean {
    if (days.length <= 16) return true;
    if (index === 0 || index === days.length - 1) return true;
    return index % 2 === 0;
  }

  resetFilters(): void {
    this.filterFromDate = startOfMonthLocal();
    this.filterToDate = endOfMonthLocal();
    this.reload();
  }

  reload(): void {
    this.error.set('');
    if (!this.filterFromDate || !this.filterToDate) {
      this.error.set(this.locale.t('admin.dashboard.dateRequired'));
      return;
    }
    if (this.filterToDate < this.filterFromDate) {
      this.error.set(this.locale.t('admin.dashboard.dateRequired'));
      return;
    }

    this.api
      .getAdminLoginDashboard({
        fromDate: this.filterFromDate,
        toDate: this.filterToDate
      })
      .subscribe({
        next: (report) => this.report.set(report),
        error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.dashboard.loadFailed'))
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
