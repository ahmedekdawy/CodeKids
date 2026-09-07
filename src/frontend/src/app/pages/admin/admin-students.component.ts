import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../auth.service';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { includesIgnoreCase, paginate, totalPages } from '../../list-query.util';
import { Grade, ManagedUser } from '../../models';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { SortDir, nextSort, sortBy } from '../../sort.util';
import { GRADE_CODES, formatGradeLabel } from '../../grade.util';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';
import { UserPhotoComponent } from '../../shared/user-photo/user-photo.component';

@Component({
  selector: 'app-admin-students',
  imports: [PageFeedbackComponent, SearchableSelectComponent, FormsModule, IconActionButtonComponent, TranslatePipe, UserPhotoComponent],
  templateUrl: './admin-students.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminStudentsComponent {
  private readonly api = inject(LearningApiService);
  private readonly auth = inject(AuthService);
  private readonly locale = inject(LocaleService);
  private readonly router = inject(Router);
  readonly students = signal<ManagedUser[]>([]);
  readonly parents = signal<ManagedUser[]>([]);
  readonly catalogGrades = signal<Grade[]>([]);
  readonly message = signal('');
  readonly error = signal('');
  readonly sortKey = signal('displayName');
  readonly sortDir = signal<SortDir>('asc');
  readonly editingId = signal<string | null>(null);
  readonly impersonatingId = signal<string | null>(null);
  readonly page = signal(1);
  readonly pageSize = signal(10);
  readonly filterName = signal('');
  readonly filterEmail = signal('');
  readonly filterMobile = signal('');
  readonly filterParent = signal('');
  readonly filterSchoolType = signal('');
  readonly filterStatus = signal<'active' | 'inactive' | ''>('');
  readonly pageSizeOptions = [10, 25, 50];

  studentEmail = '';
  studentName = '';
  studentPassword = '';
  studentParentId = '';
  studentMobile = '';
  studentSchoolType = '';
  studentGrade: number | null = null;

  editEmail = '';
  editName = '';
  editParentId = '';
  editPassword = '';
  editMobile = '';
  editSchoolType = '';
  editGrade: number | null = null;

  readonly parentOptions = computed(() =>
    this.parents()
      .slice()
      .sort((a, b) => a.displayName.localeCompare(b.displayName))
  );

  readonly gradeOptions = computed(() => {
    this.locale.lang();
    const catalog = this.catalogGrades();
    if (catalog.length) {
      return catalog.map((g) => ({
        value: g.id,
        label: this.locale.lang() === 'ar' ? g.name : g.nameEn
      }));
    }
    return GRADE_CODES.map((g) => ({
      value: g,
      label: formatGradeLabel((k, p) => this.locale.t(k, p), g)
    }));
  });

  readonly filteredStudents = computed(() => {
    const name = this.filterName();
    const email = this.filterEmail();
    const mobile = this.filterMobile();
    const parentQ = this.filterParent().trim().toLowerCase();
    const schoolType = this.filterSchoolType();
    const status = this.filterStatus();

    return this.students().filter((student) => {
      if (!includesIgnoreCase(student.displayName, name)) return false;
      if (!includesIgnoreCase(student.email, email)) return false;
      if (!includesIgnoreCase(student.mobilePhone, mobile)) return false;
      if (schoolType && (student.schoolType || '') !== schoolType) return false;
      if (status === 'active' && !this.isActive(student)) return false;
      if (status === 'inactive' && this.isActive(student)) return false;
      if (parentQ) {
        const parentLabel = this.parentLabel(student.parentId).toLowerCase();
        const parentId = (student.parentId || '').toLowerCase();
        if (!parentLabel.includes(parentQ) && !parentId.includes(parentQ)) return false;
      }
      return true;
    });
  });

  readonly sortedStudents = computed(() =>
    sortBy(this.filteredStudents(), this.sortKey(), this.sortDir())
  );

  readonly totalFiltered = computed(() => this.sortedStudents().length);
  readonly totalPages = computed(() => totalPages(this.totalFiltered(), this.pageSize()));
  readonly pagedStudents = computed(() =>
    paginate(this.sortedStudents(), this.page(), this.pageSize())
  );

  constructor() {
    this.reload();
    this.api.getGrades().subscribe({
      next: (grades) => this.catalogGrades.set(grades ?? []),
      error: () => undefined
    });
  }

  reload(): void {
    this.api.getUsers('Student').subscribe((students) => {
      this.students.set(students);
      this.clampPage();
    });
    this.api.getUsers('Parent').subscribe((parents) => this.parents.set(parents));
  }

  parentLabel(parentId?: string | null): string {
    if (!parentId) return this.locale.t('common.emDash');
    const parent = this.parents().find((p) => p.id === parentId);
    return parent?.displayName || parentId;
  }

  schoolTypeOptions(): { value: string; label: string }[] {
    this.locale.lang();
    return [
      { value: 'Arabic', label: this.locale.t('common.schoolTypeArabic') },
      { value: 'Language', label: this.locale.t('common.schoolTypeLanguage') }
    ];
  }

  schoolTypeLabel(value?: string | null): string {
    if (!value) return this.locale.t('common.emDash');
    if (value === 'Arabic') return this.locale.t('common.schoolTypeArabic');
    if (value === 'Language') return this.locale.t('common.schoolTypeLanguage');
    return value;
  }

  gradeLabel(grade?: number | null): string {
    if (grade == null) return this.locale.t('common.emDash');
    return formatGradeLabel((k, p) => this.locale.t(k, p), grade);
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

  setFilterSchoolType(value: string): void {
    this.filterSchoolType.set(value);
    this.onFilterChange();
  }

  setFilterStatus(value: 'active' | 'inactive' | ''): void {
    this.filterStatus.set(value);
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
    this.filterSchoolType.set('');
    this.filterStatus.set('');
    this.page.set(1);
  }

  hasActiveFilters(): boolean {
    return !!(
      this.filterName().trim() ||
      this.filterEmail().trim() ||
      this.filterMobile().trim() ||
      this.filterParent().trim() ||
      this.filterSchoolType() ||
      this.filterStatus()
    );
  }

  submitForm(): void {
    if (this.editingId()) {
      this.saveEdit(this.editingId()!);
    } else {
      this.createStudent();
    }
  }

  createStudent(): void {
    this.clearStatus();
    if (!this.studentEmail.trim() && !this.studentMobile.trim()) {
      this.error.set(this.locale.t('admin.users.emailOrMobileRequired'));
      return;
    }
    this.api
      .createUser({
        email: this.studentEmail.trim() || null,
        displayName: this.studentName,
        password: this.studentPassword,
        role: 'Student',
        parentId: this.studentParentId || null,
        grade: this.studentGrade,
        schoolType: this.studentSchoolType || null,
        mobilePhone: this.studentMobile || null
      })
      .subscribe({
        next: () => {
          this.message.set(this.locale.t('admin.students.created'));
          this.resetCreateForm();
          this.reload();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.students.createFailed'))
      });
  }

  startEdit(student: ManagedUser): void {
    this.editingId.set(student.id);
    this.editEmail = student.email;
    this.editName = student.displayName;
    this.editParentId = student.parentId || '';
    this.editMobile = student.mobilePhone || '';
    this.editSchoolType = student.schoolType || '';
    this.editGrade = student.grade ?? null;
    this.editPassword = '';
    this.clearStatus();
  }

  cancelEdit(): void {
    this.editingId.set(null);
  }

  saveEdit(studentId: string): void {
    this.clearStatus();
    if (!this.editEmail.trim() && !this.editMobile.trim()) {
      this.error.set(this.locale.t('admin.users.emailOrMobileRequired'));
      return;
    }
    this.api
      .updateUser(studentId, {
        email: this.editEmail.trim() || null,
        displayName: this.editName,
        role: 'Student',
        parentId: this.editParentId || null,
        password: this.editPassword || null,
        grade: this.editGrade,
        schoolType: this.editSchoolType || null,
        mobilePhone: this.editMobile || null
      })
      .subscribe({
        next: () => {
          this.message.set(this.locale.t('admin.students.updated'));
          this.cancelEdit();
          this.reload();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.students.updateFailed'))
      });
  }

  deleteStudent(student: ManagedUser): void {
    if (!confirm(this.locale.t('admin.students.confirmDelete', { name: student.displayName }))) return;
    this.clearStatus();
    this.api.deleteUser(student.id).subscribe({
      next: () => {
        this.message.set(this.locale.t('admin.students.deleted'));
        if (this.editingId() === student.id) {
          this.cancelEdit();
        }
        this.reload();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.students.deleteFailed'))
    });
  }

  isActive(user: ManagedUser): boolean {
    return user.isActive !== false;
  }

  toggleActive(student: ManagedUser): void {
    const next = !this.isActive(student);
    if (!next && !confirm(this.locale.t('admin.users.confirmDeactivate', { name: student.displayName }))) {
      return;
    }
    this.clearStatus();
    this.api.setUserActive(student.id, next).subscribe({
      next: (updated) => {
        this.students.update((list) =>
          list.map((item) => (item.id === updated.id ? { ...item, ...updated } : item))
        );
        this.message.set(this.locale.t(next ? 'admin.users.activated' : 'admin.users.deactivated'));
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'admin.users.activeFailed'))
    });
  }

  loginAs(student: ManagedUser): void {
    this.clearStatus();
    this.impersonatingId.set(student.id);
    this.auth.impersonate(student.id).subscribe({
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
    this.studentEmail = '';
    this.studentName = '';
    this.studentPassword = '';
    this.studentMobile = '';
    this.studentSchoolType = '';
    this.studentParentId = '';
    this.studentGrade = null;
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
