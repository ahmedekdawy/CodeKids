import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { ManagedUser } from '../../models';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { SortDir, nextSort, sortBy } from '../../sort.util';

@Component({
  selector: 'app-admin-users',
  imports: [FormsModule, IconActionButtonComponent, TranslatePipe],
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminUsersComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  readonly users = signal<ManagedUser[]>([]);
  readonly message = signal('');
  readonly error = signal('');
  readonly sortKey = signal<string>('displayName');
  readonly sortDir = signal<SortDir>('asc');
  readonly editingId = signal<string | null>(null);

  userEmail = '';
  userName = '';
  userPassword = '';
  userRole: 'Teacher' | 'Student' | 'Parent' | 'SuperAdmin' = 'Teacher';
  userParentId = '';

  editEmail = '';
  editName = '';
  editRole: 'Teacher' | 'Student' | 'Parent' | 'SuperAdmin' = 'Teacher';
  editParentId = '';
  editPassword = '';

  readonly sortedUsers = computed(() =>
    sortBy(this.users(), this.sortKey(), this.sortDir())
  );

  constructor() {
    this.reload();
  }

  reload(): void {
    this.api.getUsers().subscribe((users) => this.users.set(users));
  }

  setSort(key: string): void {
    this.sortDir.set(nextSort(this.sortKey(), key, this.sortDir()));
    this.sortKey.set(key);
  }

  sortMark(key: string): string {
    if (this.sortKey() !== key) return '';
    return this.sortDir() === 'asc' ? '↑' : '↓';
  }

  roleLabel(role: string): string {
    switch (role) {
      case 'Teacher':
        return this.locale.t('role.teacher');
      case 'Student':
        return this.locale.t('role.student');
      case 'Parent':
        return this.locale.t('role.parent');
      case 'SuperAdmin':
        return this.locale.t('role.superAdmin');
      default:
        return role;
    }
  }

  createUser(): void {
    this.clearStatus();
    this.api
      .createUser({
        email: this.userEmail,
        displayName: this.userName,
        password: this.userPassword,
        role: this.userRole,
        parentId: this.userParentId || null
      })
      .subscribe({
        next: () => {
          this.message.set(this.locale.t('admin.users.created'));
          this.userEmail = '';
          this.userName = '';
          this.userPassword = '';
          this.reload();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err,'admin.users.createFailed'))
      });
  }

  startEdit(user: ManagedUser): void {
    this.editingId.set(user.id);
    this.editEmail = user.email;
    this.editName = user.displayName;
    this.editRole = user.role as typeof this.editRole;
    this.editParentId = user.parentId || '';
    this.editPassword = '';
  }

  cancelEdit(): void {
    this.editingId.set(null);
  }

  saveEdit(userId: string): void {
    this.clearStatus();
    this.api
      .updateUser(userId, {
        email: this.editEmail,
        displayName: this.editName,
        role: this.editRole,
        parentId: this.editParentId || null,
        password: this.editPassword || null
      })
      .subscribe({
        next: () => {
          this.message.set(this.locale.t('admin.users.updated'));
          this.editingId.set(null);
          this.reload();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err,'admin.users.updateFailed'))
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
      error: (err) => this.error.set(this.locale.fromApiError(err,'admin.users.deleteFailed'))
    });
  }

  private clearStatus(): void {
    this.message.set('');
    this.error.set('');
  }
}
