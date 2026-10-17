import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { CataloguePage, ManagedUser, RoleMember, RoleSummary } from '../../models';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { TranslatePipe } from '../../shared/translate.pipe';

type BaseRole = 'Teacher' | 'SuperAdmin';

/**
 * Lets an admin create roles from the fixed page catalogue, choose the pages each role exposes, and
 * add or remove users. The seeded SuperAdmin and Teacher roles are system roles and stay locked.
 */
@Component({
  selector: 'app-admin-roles',
  imports: [PageFeedbackComponent, SearchableSelectComponent, TranslatePipe, FormsModule],
  templateUrl: './admin-roles.component.html',
  styleUrl: './admin-panel.css'
})
export class AdminRolesComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  readonly roles = signal<RoleSummary[]>([]);
  readonly pages = signal<CataloguePage[]>([]);
  readonly error = signal('');
  readonly message = signal('');

  readonly formOpen = signal(false);
  readonly editingId = signal<string | null>(null);
  readonly formName = signal('');
  readonly formDisplayName = signal('');
  readonly formDescription = signal('');
  readonly formBaseRole = signal<BaseRole>('Teacher');
  readonly formPageKeys = signal<ReadonlySet<string>>(new Set<string>());

  readonly membersRoleId = signal('');
  readonly members = signal<RoleMember[]>([]);
  readonly eligibleUsers = signal<ManagedUser[]>([]);
  readonly selectedUserId = signal('');

  readonly roleOptions = computed(() => {
    this.locale.lang();
    return this.roles().map((role) => ({
      value: role.id,
      label: `${role.displayName} (${role.memberCount})`
    }));
  });

  readonly baseRoleOptions = computed(() => {
    this.locale.lang();
    return [
      { value: 'Teacher', label: this.locale.t('roles.teacher') },
      { value: 'SuperAdmin', label: this.locale.t('roles.superAdmin') }
    ];
  });

  readonly userOptions = computed(() => {
    const memberIds = new Set(this.members().map((member) => member.userId));
    return this.eligibleUsers()
      .filter((user) => !memberIds.has(user.id))
      .map((user) => ({
        value: user.id,
        label: user.displayName || user.email || user.mobilePhone || user.id
      }));
  });

  /** Catalogue pages that belong to the panel the form's role type uses. */
  readonly cataloguePages = computed(() => {
    const panel = this.formBaseRole() === 'SuperAdmin' ? 'admin' : 'teacher';
    return this.pages().filter((page) => page.panel === panel);
  });

  /** Pages grouped by their menu category, in catalogue order. */
  readonly catalogueGroups = computed(() => {
    const groups: Array<{ labelKey: string; pages: CataloguePage[] }> = [];
    const byKey = new Map<string, { labelKey: string; pages: CataloguePage[] }>();
    for (const page of this.cataloguePages()) {
      const key = page.categoryKey || '';
      let group = byKey.get(key);
      if (!group) {
        group = { labelKey: page.labelKey, pages: [] };
        byKey.set(key, group);
        groups.push(group);
      }
      group.pages.push(page);
    }
    return groups;
  });

  readonly selectedMembersRole = computed(() =>
    this.roles().find((role) => role.id === this.membersRoleId()) ?? null
  );

  constructor() {
    this.reload();
  }

  reload(): void {
    this.error.set('');
    this.api.getRoles().subscribe({
      next: (roles) => {
        const list = roles ?? [];
        this.roles.set(list);
        if (!this.membersRoleId() && list.length) {
          this.selectMembersRole(list[0].id);
        }
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'roles.loadFailed'))
    });

    if (!this.pages().length) {
      this.api.getPages().subscribe({
        next: (pages) => this.pages.set(pages ?? []),
        error: (err) => this.error.set(this.locale.fromApiError(err, 'roles.loadFailed'))
      });
    }
  }

  roleTypeLabel(role: RoleSummary): string {
    return role.baseRole === 'SuperAdmin'
      ? this.locale.t('roles.superAdmin')
      : this.locale.t('roles.teacher');
  }

  openCreate(): void {
    this.editingId.set(null);
    this.formName.set('');
    this.formDisplayName.set('');
    this.formDescription.set('');
    this.formBaseRole.set('Teacher');
    this.formPageKeys.set(new Set<string>());
    this.error.set('');
    this.formOpen.set(true);
  }

  openEdit(role: RoleSummary): void {
    if (role.isSystem) {
      return;
    }

    this.editingId.set(role.id);
    this.formName.set(role.name);
    this.formDisplayName.set(role.displayName);
    this.formDescription.set(role.description);
    this.formBaseRole.set(role.baseRole);
    this.formPageKeys.set(new Set(role.pages.map((page) => page.key)));
    this.error.set('');
    this.formOpen.set(true);
  }

  closeForm(): void {
    this.formOpen.set(false);
  }

  isPageSelected(key: string): boolean {
    return this.formPageKeys().has(key);
  }

  togglePage(key: string): void {
    const next = new Set(this.formPageKeys());
    if (next.has(key)) {
      next.delete(key);
    } else {
      next.add(key);
    }
    this.formPageKeys.set(next);
  }

  save(): void {
    this.error.set('');
    const pageKeys = [...this.formPageKeys()];
    const editingId = this.editingId();
    const request = editingId
      ? this.api.updateRole(editingId, {
          displayName: this.formDisplayName(),
          description: this.formDescription(),
          pageKeys
        })
      : this.api.createRole({
          name: this.formName(),
          displayName: this.formDisplayName(),
          description: this.formDescription(),
          baseRole: this.formBaseRole(),
          pageKeys
        });

    request.subscribe({
      next: () => {
        this.formOpen.set(false);
        this.message.set(this.locale.t(editingId ? 'roles.updated' : 'roles.created'));
        this.reload();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'roles.saveFailed'))
    });
  }

  deleteRole(role: RoleSummary): void {
    if (role.isSystem) {
      return;
    }

    if (!window.confirm(this.locale.t('roles.deleteConfirm', { name: role.displayName }))) {
      return;
    }

    this.error.set('');
    this.api.deleteRole(role.id).subscribe({
      next: () => {
        this.message.set(this.locale.t('roles.deleted'));
        if (this.membersRoleId() === role.id) {
          this.membersRoleId.set('');
          this.members.set([]);
        }
        this.reload();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'roles.deleteFailed'))
    });
  }

  selectMembersRole(roleId: string): void {
    this.membersRoleId.set(roleId);
    this.selectedUserId.set('');
    const role = this.roles().find((item) => item.id === roleId);
    if (!role) {
      this.members.set([]);
      this.eligibleUsers.set([]);
      return;
    }

    this.api.getRoleMembers(roleId).subscribe({
      next: (members) => this.members.set(members ?? []),
      error: (err) => this.error.set(this.locale.fromApiError(err, 'roles.loadFailed'))
    });

    this.api.getUsers(role.baseRole).subscribe({
      next: (users) => this.eligibleUsers.set(users ?? []),
      error: (err) => this.error.set(this.locale.fromApiError(err, 'roles.loadFailed'))
    });
  }

  assignUser(): void {
    const roleId = this.membersRoleId();
    const userId = this.selectedUserId();
    if (!roleId || !userId) {
      return;
    }

    this.error.set('');
    this.api.assignRoleUser(roleId, userId).subscribe({
      next: () => {
        this.selectedUserId.set('');
        this.message.set(this.locale.t('roles.memberAdded'));
        this.selectMembersRole(roleId);
        this.refreshMemberCounts();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'roles.saveFailed'))
    });
  }

  removeMember(member: RoleMember): void {
    const roleId = this.membersRoleId();
    if (!roleId) {
      return;
    }

    this.error.set('');
    this.api.removeRoleUser(roleId, member.userId).subscribe({
      next: () => {
        this.message.set(this.locale.t('roles.memberRemoved'));
        this.selectMembersRole(roleId);
        this.refreshMemberCounts();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'roles.saveFailed'))
    });
  }

  formatUtc(value?: string | null): string {
    if (!value) return this.locale.t('common.emDash');
    return new Date(value).toLocaleString(this.locale.lang());
  }

  private refreshMemberCounts(): void {
    this.api.getRoles().subscribe({
      next: (roles) => this.roles.set(roles ?? []),
      error: (err) => this.error.set(this.locale.fromApiError(err, 'roles.loadFailed'))
    });
  }
}
