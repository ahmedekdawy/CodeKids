import { Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { map } from 'rxjs';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { includesIgnoreCase, paginate, totalPages } from '../../list-query.util';
import { ManagedUser, TeacherWorkShift, UserRole } from '../../models';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import {
  MultiSelectOption,
  SearchableMultiSelectComponent
} from '../../shared/searchable-multi-select/searchable-multi-select.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { SortDir, nextSort, sortBy } from '../../sort.util';
import { STAGE_CODES, formatStageLabel } from '../../grade.util';

type ManagedRole = Extract<UserRole, 'SuperAdmin' | 'Teacher' | 'Parent'>;

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
  imports: [FormsModule, IconActionButtonComponent, SearchableMultiSelectComponent, TranslatePipe],
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminUsersComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  readonly managedRole = toSignal(
    this.route.data.pipe(map((data) => (data['role'] as ManagedRole) || 'SuperAdmin')),
    { initialValue: (this.route.snapshot.data['role'] as ManagedRole) || 'SuperAdmin' }
  );

  readonly meta = computed(() => ROLE_META[this.managedRole()]);
  readonly users = signal<ManagedUser[]>([]);
  readonly message = signal('');
  readonly error = signal('');
  readonly sortKey = signal<string>('displayName');
  readonly sortDir = signal<SortDir>('asc');
  readonly editingId = signal<string | null>(null);
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
  readonly pageSizeOptions = [10, 25, 50];

  userEmail = '';
  userName = '';
  userPassword = '';
  userMobile = '';
  userWorkShift: TeacherWorkShift = 'Both';
  userStages: number[] = [...STAGE_CODES];

  editEmail = '';
  editName = '';
  editMobile = '';
  editWorkShift: TeacherWorkShift = 'Both';
  editStages: number[] = [...STAGE_CODES];
  editPassword = '';

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

  constructor() {
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((data) => {
      const role = (data['role'] as ManagedRole) || 'SuperAdmin';
      this.resetFilters();
      this.cancelEdit();
      this.clearStatus();
      this.reload(role);
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
    this.api
      .createUser({
        email: this.userEmail.trim() || null,
        displayName: this.userName,
        password: this.userPassword,
        role,
        mobilePhone: this.userMobile || null,
        workShift: role === 'Teacher' ? this.userWorkShift : null,
        stages: role === 'Teacher' ? this.userStages : null
      })
      .subscribe({
        next: () => {
          this.message.set(this.locale.t('admin.users.created'));
          this.userEmail = '';
          this.userName = '';
          this.userPassword = '';
          this.userMobile = '';
          this.userWorkShift = 'Both';
          this.userStages = [...STAGE_CODES];
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
  }

  cancelEdit(): void {
    this.editingId.set(null);
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
    this.api
      .updateUser(userId, {
        email: this.editEmail.trim() || null,
        displayName: this.editName,
        role,
        mobilePhone: this.editMobile || null,
        workShift: role === 'Teacher' ? this.editWorkShift : null,
        stages: role === 'Teacher' ? this.editStages : null,
        password: this.editPassword || null
      })
      .subscribe({
        next: () => {
          this.message.set(this.locale.t('admin.users.updated'));
          this.editingId.set(null);
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
