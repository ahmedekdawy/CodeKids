import { Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../auth.service';
import { map } from 'rxjs';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { includesIgnoreCase, paginate, totalPages } from '../../list-query.util';
import {
  Course,
  ManagedUser,
  TeacherContractType,
  TeacherCourseRate,
  TeacherWorkShift,
  UserRole
} from '../../models';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import {
  MultiSelectOption,
  SearchableMultiSelectComponent
} from '../../shared/searchable-multi-select/searchable-multi-select.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { SortDir, nextSort, sortBy } from '../../sort.util';
import { STAGE_CODES, formatCourseLabel, formatStageLabel } from '../../grade.util';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';

type ManagedRole = Extract<UserRole, 'SuperAdmin' | 'Teacher' | 'Parent'>;
type CourseRateRow = { courseId: string; sessionAmount: number | null; monthlySalary: number | null };

const ROLE_META: Record<
  ManagedRole,
  { titleKey: string; subtitleKey: string; createKey: string; listKey: string; apiRole: string }
> = {
  SuperAdmin: {
    titleKey: 'admin.admins.title',
    subtitleKey: 'admin.admins.subtitle',
    createKey: 'admin.admins.create',
    listKey: 'admin.admins.list',
    apiRole: 'SuperAdmin'
  },
  Teacher: {
    titleKey: 'admin.teachers.title',
    subtitleKey: 'admin.teachers.subtitle',
    createKey: 'admin.teachers.create',
    listKey: 'admin.teachers.list',
    apiRole: 'Teacher'
  },
  Parent: {
    titleKey: 'admin.parents.title',
    subtitleKey: 'admin.parents.subtitle',
    createKey: 'admin.parents.create',
    listKey: 'admin.parents.list',
    apiRole: 'Parent'
  }
};

@Component({
  selector: 'app-admin-users',
  imports: [PageFeedbackComponent, SearchableSelectComponent, FormsModule, IconActionButtonComponent, SearchableMultiSelectComponent, TranslatePipe],
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminUsersComponent {
  private readonly api = inject(LearningApiService);
  private readonly auth = inject(AuthService);
  private readonly locale = inject(LocaleService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly managedRole = toSignal(
    this.route.data.pipe(map((data) => (data['role'] as ManagedRole) || 'SuperAdmin')),
    { initialValue: (this.route.snapshot.data['role'] as ManagedRole) || 'SuperAdmin' }
  );

  readonly meta = computed(() => ROLE_META[this.managedRole()]);
  readonly users = signal<ManagedUser[]>([]);
  readonly courses = signal<Course[]>([]);
  readonly message = signal('');
  readonly error = signal('');
  readonly sortKey = signal<string>('displayName');
  readonly sortDir = signal<SortDir>('asc');
  readonly editingId = signal<string | null>(null);
  readonly impersonatingId = signal<string | null>(null);
  readonly page = signal(1);
  readonly pageSize = signal(10);
  readonly filterName = signal('');
  readonly filterEmail = signal('');
  readonly filterMobile = signal('');
  readonly filterParent = signal('');

  readonly stages = STAGE_CODES;
  readonly stageOptions = computed<MultiSelectOption[]>(() => {
    this.locale.lang();
    return this.stages.map((s) => ({
      value: s,
      label: formatStageLabel((k, p) => this.locale.t(k, p), s)
    }));
  });
  readonly workShifts: TeacherWorkShift[] = ['Am', 'Pm', 'Both'];
  readonly contractTypes: TeacherContractType[] = ['Session', 'Monthly'];
  readonly pageSizeOptions = [10, 25, 50];

  userEmail = '';
  userName = '';
  userPassword = '';
  userMobile = '';
  userWorkShift: TeacherWorkShift = 'Both';
  userStages: number[] = [...STAGE_CODES];
  userContractType: TeacherContractType = 'Session';
  userPrimaryAmount: number | null = null;
  userPrepAmount: number | null = null;
  userSecondaryAmount: number | null = null;
  userMonthlySalary: number | null = null;
  readonly userCourseRates = signal<CourseRateRow[]>([]);

  editEmail = '';
  editName = '';
  editMobile = '';
  editWorkShift: TeacherWorkShift = 'Both';
  editStages: number[] = [...STAGE_CODES];
  editPassword = '';
  editContractType: TeacherContractType = 'Session';
  editPrimaryAmount: number | null = null;
  editPrepAmount: number | null = null;
  editSecondaryAmount: number | null = null;
  editMonthlySalary: number | null = null;
  readonly editCourseRates = signal<CourseRateRow[]>([]);

  readonly filteredUsers = computed(() => {
    const name = this.filterName();
    const email = this.filterEmail();
    const mobile = this.filterMobile();
    const parentQ = this.filterParent().trim().toLowerCase();
    const role = this.managedRole();

    return this.users().filter((user) => {
      if (!includesIgnoreCase(user.displayName, name)) return false;
      if (!includesIgnoreCase(user.email, email)) return false;
      if (!includesIgnoreCase(user.mobilePhone, mobile)) return false;
      if (role === 'Parent' && parentQ) {
        const haystack = [user.displayName, user.email, user.mobilePhone, user.id]
          .map((v) => (v ?? '').toLowerCase())
          .join(' ');
        if (!haystack.includes(parentQ)) return false;
      }
      return true;
    });
  });

  readonly sortedUsers = computed(() =>
    sortBy(this.filteredUsers(), this.sortKey(), this.sortDir())
  );

  readonly totalFiltered = computed(() => this.sortedUsers().length);
  readonly totalPages = computed(() => totalPages(this.totalFiltered(), this.pageSize()));
  readonly pagedUsers = computed(() =>
    paginate(this.sortedUsers(), this.page(), this.pageSize())
  );

  readonly courseOptions = computed(() => {
    this.locale.lang();
    return this.courses()
      .slice()
      .sort((a, b) => {
        const ga = a.grade ?? 999;
        const gb = b.grade ?? 999;
        if (ga !== gb) return ga - gb;
        return a.title.localeCompare(b.title);
      });
  });

  constructor() {
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((data) => {
      const role = (data['role'] as ManagedRole) || 'SuperAdmin';
      this.resetFilters();
      this.cancelEdit();
      this.clearStatus();
      this.reload(role);
      if (role === 'Teacher') {
        this.api.getCourses().subscribe((courses) => this.courses.set(courses ?? []));
      }
    });
  }

  reload(role: ManagedRole = this.managedRole()): void {
    this.api.getUsers(ROLE_META[role].apiRole).subscribe((users) => {
      this.users.set(users);
      this.clampPage();
    });
  }

  setSort(key: string): void {
    this.sortDir.set(nextSort(this.sortKey(), key, this.sortDir()));
    this.sortKey.set(key);
    this.page.set(1);
  }

  sortMark(key: string): string {
    if (this.sortKey() !== key) return '';
    return this.sortDir() === 'asc' ? '↑' : '↓';
  }

  onFilterChange(): void {
    this.page.set(1);
  }

  setFilterName(value: string): void {
    this.filterName.set(value);
    this.onFilterChange();
  }

  setFilterEmail(value: string): void {
    this.filterEmail.set(value);
    this.onFilterChange();
  }

  setFilterMobile(value: string): void {
    this.filterMobile.set(value);
    this.onFilterChange();
  }

  setFilterParent(value: string): void {
    this.filterParent.set(value);
    this.onFilterChange();
  }

  setPageSize(value: string | number): void {
    this.pageSize.set(Number(value) || 10);
    this.page.set(1);
  }

  goToPage(page: number): void {
    this.page.set(Math.min(Math.max(1, page), this.totalPages()));
  }

  resetFilters(): void {
    this.filterName.set('');
    this.filterEmail.set('');
    this.filterMobile.set('');
    this.filterParent.set('');
    this.page.set(1);
  }

  hasActiveFilters(): boolean {
    return !!(
      this.filterName().trim() ||
      this.filterEmail().trim() ||
      this.filterMobile().trim() ||
      this.filterParent().trim()
    );
  }

  stageLabel(stage: number): string {
    return formatStageLabel((k, p) => this.locale.t(k, p), stage);
  }

  stagesLabel(stages?: number[] | null): string {
    if (!stages?.length) return this.locale.t('common.emDash');
    return [...stages]
      .sort((a, b) => a - b)
      .map((s) => this.stageLabel(s))
      .join(', ');
  }

  workShiftLabel(shift?: string | null): string {
    if (!shift) return this.locale.t('common.emDash');
    switch (shift) {
      case 'Am':
        return this.locale.t('admin.users.shiftAm');
      case 'Pm':
        return this.locale.t('admin.users.shiftPm');
      case 'Both':
        return this.locale.t('admin.users.shiftBoth');
      default:
        return shift;
    }
  }

  contractTypeLabel(type?: string | null): string {
    if (!type) return this.locale.t('common.emDash');
    if (type === 'Session') return this.locale.t('admin.users.contractSession');
    if (type === 'Monthly') return this.locale.t('admin.users.contractMonthly');
    return type;
  }

  courseOptionLabel(course: Course): string {
    return formatCourseLabel((k, p) => this.locale.t(k, p), course.title, course.grade, 'common.allGrades', course.stageId);
  }

  moneyLabel(value?: number | null): string {
    if (value == null) return this.locale.t('common.emDash');
    return String(value);
  }

  addUserRateRow(): void {
    this.userCourseRates.update((rows) => [
      ...rows,
      { courseId: '', sessionAmount: null, monthlySalary: null }
    ]);
  }

  removeUserRateRow(index: number): void {
    this.userCourseRates.update((rows) => rows.filter((_, i) => i !== index));
  }

  onUserRateCourseChange(index: number, courseId: string): void {
    this.userCourseRates.update((rows) =>
      rows.map((row, i) => (i === index ? { ...row, courseId } : row))
    );
  }

  onUserRateSessionChange(index: number, value: string | number): void {
    this.userCourseRates.update((rows) =>
      rows.map((row, i) => (i === index ? { ...row, sessionAmount: parseMoney(value) } : row))
    );
  }

  onUserRateMonthlyChange(index: number, value: string | number): void {
    this.userCourseRates.update((rows) =>
      rows.map((row, i) => (i === index ? { ...row, monthlySalary: parseMoney(value) } : row))
    );
  }

  addEditRateRow(): void {
    this.editCourseRates.update((rows) => [
      ...rows,
      { courseId: '', sessionAmount: null, monthlySalary: null }
    ]);
  }

  removeEditRateRow(index: number): void {
    this.editCourseRates.update((rows) => rows.filter((_, i) => i !== index));
  }

  onEditRateCourseChange(index: number, courseId: string): void {
    this.editCourseRates.update((rows) =>
      rows.map((row, i) => (i === index ? { ...row, courseId } : row))
    );
  }

  onEditRateSessionChange(index: number, value: string | number): void {
    this.editCourseRates.update((rows) =>
      rows.map((row, i) => (i === index ? { ...row, sessionAmount: parseMoney(value) } : row))
    );
  }

  onEditRateMonthlyChange(index: number, value: string | number): void {
    this.editCourseRates.update((rows) =>
      rows.map((row, i) => (i === index ? { ...row, monthlySalary: parseMoney(value) } : row))
    );
  }

  createUser(): void {
    this.clearStatus();
    const role = this.managedRole();
    if (!this.userEmail.trim() && !this.userMobile.trim()) {
      this.error.set(this.locale.t('admin.users.emailOrMobileRequired'));
      return;
    }
    if (role === 'Teacher' && this.userStages.length === 0) {
      this.error.set(this.locale.t('admin.users.stagesRequired'));
      return;
    }
    const rates = role === 'Teacher' ? this.toRatePayload(this.userCourseRates()) : null;
    if (rates === false) {
      this.error.set(this.locale.t('admin.users.rateIncomplete'));
      return;
    }
    this.api
      .createUser({
        email: this.userEmail.trim() || null,
        displayName: this.userName,
        password: this.userPassword,
        role,
        mobilePhone: this.userMobile || null,
        workShift: role === 'Teacher' ? this.userWorkShift : null,
        stages: role === 'Teacher' ? this.userStages : null,
        contractType: role === 'Teacher' ? this.userContractType : null,
        primaryAmount: role === 'Teacher' ? this.userPrimaryAmount : null,
        prepAmount: role === 'Teacher' ? this.userPrepAmount : null,
        secondaryAmount: role === 'Teacher' ? this.userSecondaryAmount : null,
        monthlySalary: role === 'Teacher' ? this.userMonthlySalary : null,
        courseRates: rates
      })
      .subscribe({
        next: () => {
          this.message.set(this.locale.t('admin.users.created'));
          this.resetCreateForm();
          this.reload();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.users.createFailed'))
      });
  }

  startEdit(user: ManagedUser): void {
    this.editingId.set(user.id);
    this.editEmail = user.email;
    this.editName = user.displayName;
    this.editMobile = user.mobilePhone || '';
    this.editWorkShift = (user.workShift as TeacherWorkShift) || 'Both';
    this.editStages = user.stages?.length ? [...user.stages] : [...STAGE_CODES];
    this.editPassword = '';
    this.editContractType = (user.contractType as TeacherContractType) || 'Session';
    this.editPrimaryAmount = user.primaryAmount ?? null;
    this.editPrepAmount = user.prepAmount ?? null;
    this.editSecondaryAmount = user.secondaryAmount ?? null;
    this.editMonthlySalary = user.monthlySalary ?? null;
    this.editCourseRates.set(toRateRows(user.courseRates));
    this.clearStatus();
  }

  cancelEdit(): void {
    this.editingId.set(null);
    this.editCourseRates.set([]);
  }

  saveEdit(userId: string): void {
    this.clearStatus();
    const role = this.managedRole();
    if (!this.editEmail.trim() && !this.editMobile.trim()) {
      this.error.set(this.locale.t('admin.users.emailOrMobileRequired'));
      return;
    }
    if (role === 'Teacher' && this.editStages.length === 0) {
      this.error.set(this.locale.t('admin.users.stagesRequired'));
      return;
    }
    const rates = role === 'Teacher' ? this.toRatePayload(this.editCourseRates()) : null;
    if (rates === false) {
      this.error.set(this.locale.t('admin.users.rateIncomplete'));
      return;
    }
    this.api
      .updateUser(userId, {
        email: this.editEmail.trim() || null,
        displayName: this.editName,
        role,
        mobilePhone: this.editMobile || null,
        workShift: role === 'Teacher' ? this.editWorkShift : null,
        stages: role === 'Teacher' ? this.editStages : null,
        contractType: role === 'Teacher' ? this.editContractType : null,
        primaryAmount: role === 'Teacher' ? this.editPrimaryAmount : null,
        prepAmount: role === 'Teacher' ? this.editPrepAmount : null,
        secondaryAmount: role === 'Teacher' ? this.editSecondaryAmount : null,
        monthlySalary: role === 'Teacher' ? this.editMonthlySalary : null,
        courseRates: rates,
        password: this.editPassword || null
      })
      .subscribe({
        next: () => {
          this.message.set(this.locale.t('admin.users.updated'));
          this.cancelEdit();
          this.reload();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.users.updateFailed'))
      });
  }

  deleteUser(user: ManagedUser): void {
    if (!confirm(this.locale.t('admin.users.confirmDelete', { name: user.displayName }))) return;
    this.clearStatus();
    this.api.deleteUser(user.id).subscribe({
      next: () => {
        this.message.set(this.locale.t('admin.users.deleted'));
        this.reload();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.users.deleteFailed'))
    });
  }

  loginAs(user: ManagedUser): void {
    this.clearStatus();
    this.impersonatingId.set(user.id);
    this.auth.impersonate(user.id).subscribe({
      next: () => {
        this.impersonatingId.set(null);
        void this.router.navigateByUrl(this.auth.roleHome());
      },
      error: (err) => {
        this.impersonatingId.set(null);
        this.error.set(this.locale.fromApiError(err, 'admin.users.loginAsFailed'));
      }
    });
  }

  private resetCreateForm(): void {
    this.userEmail = '';
    this.userName = '';
    this.userPassword = '';
    this.userMobile = '';
    this.userWorkShift = 'Both';
    this.userStages = [...STAGE_CODES];
    this.userContractType = 'Session';
    this.userPrimaryAmount = null;
    this.userPrepAmount = null;
    this.userSecondaryAmount = null;
    this.userMonthlySalary = null;
    this.userCourseRates.set([]);
  }

  private toRatePayload(
    rows: CourseRateRow[]
  ): Array<{ courseId: string; sessionAmount?: number | null; monthlySalary?: number | null }> | false {
    const filled = rows.filter(
      (r) => r.courseId || r.sessionAmount != null || r.monthlySalary != null
    );
    if (filled.some((r) => !r.courseId || (r.sessionAmount == null && r.monthlySalary == null))) {
      return false;
    }
    return filled.map((r) => ({
      courseId: r.courseId,
      sessionAmount: r.sessionAmount,
      monthlySalary: r.monthlySalary
    }));
  }

  private clampPage(): void {
    if (this.page() > this.totalPages()) {
      this.page.set(this.totalPages());
    }
  }

  private clearStatus(): void {
    this.message.set('');
    this.error.set('');
  }
}

function parseMoney(value: string | number | null | undefined): number | null {
  if (value == null || value === '') return null;
  if (typeof value === 'number') return Number.isFinite(value) ? value : null;
  const trimmed = value.trim();
  if (!trimmed) return null;
  const n = Number(trimmed);
  return Number.isFinite(n) ? n : null;
}

function toRateRows(rates?: TeacherCourseRate[] | null): CourseRateRow[] {
  if (!rates?.length) return [];
  return rates.map((r) => ({
    courseId: r.courseId,
    sessionAmount: r.sessionAmount ?? null,
    monthlySalary: r.monthlySalary ?? null
  }));
}
