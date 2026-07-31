import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LearningApiService } from '../../learning-api.service';
import { ManagedUser } from '../../models';
import { SortDir, nextSort, sortBy } from '../../sort.util';

@Component({
  selector: 'app-admin-users',
  imports: [FormsModule],
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminUsersComponent {
  private readonly api = inject(LearningApiService);
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
          this.message.set('User created.');
          this.userEmail = '';
          this.userName = '';
          this.userPassword = '';
          this.reload();
        },
        error: (err) => this.error.set(err?.error?.message || 'Could not create user.')
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
          this.message.set('User updated.');
          this.editingId.set(null);
          this.reload();
        },
        error: (err) => this.error.set(err?.error?.message || 'Could not update user.')
      });
  }

  deleteUser(user: ManagedUser): void {
    if (!confirm(`Delete ${user.displayName}?`)) return;
    this.clearStatus();
    this.api.deleteUser(user.id).subscribe({
      next: () => {
        this.message.set('User deleted.');
        this.reload();
      },
      error: (err) => this.error.set(err?.error?.message || 'Could not delete user.')
    });
  }

  private clearStatus(): void {
    this.message.set('');
    this.error.set('');
  }
}
