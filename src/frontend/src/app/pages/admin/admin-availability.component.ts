import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { ManagedUser } from '../../models';
import { AvailabilityEditorComponent } from '../../shared/availability-editor/availability-editor.component';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { TranslatePipe } from '../../shared/translate.pipe';

type PersonKind = 'Teacher' | 'Student';

@Component({
  selector: 'app-admin-availability',
  imports: [FormsModule, TranslatePipe, SearchableSelectComponent, AvailabilityEditorComponent],
  template: `
    <div class="panel-page">
      <h2>{{ 'availability.title' | t }}</h2>
      <p class="meta">{{ 'availability.adminSubtitle' | t }}</p>

      <section class="block">
        <div class="list-filters">
          <label>
            {{ 'availability.personKind' | t }}
            <app-searchable-select
              [ngModel]="kind()"
              name="availabilityKind"
              (ngModelChange)="setKind($event)"
              [options]="kindOptions()"
            />
          </label>
          <label>
            {{ (kind() === 'Teacher' ? 'common.teacher' : 'common.student') | t }}
            <app-searchable-select
              [ngModel]="userId()"
              name="availabilityUser"
              (ngModelChange)="userId.set($event || '')"
              [emptyLabel]="'common.select' | t"
              [options]="userOptions()"
            />
          </label>
        </div>
        @if (error()) {
          <p class="error">{{ error() }}</p>
        }
      </section>

      @if (userId()) {
        <app-availability-editor [userId]="userId()" [showVacations]="kind() === 'Teacher'" />
      }
    </div>
  `,
  styleUrl: './admin-panel.css'
})
export class AdminAvailabilityComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  readonly kind = signal<PersonKind>('Teacher');
  readonly userId = signal('');
  readonly users = signal<ManagedUser[]>([]);
  readonly error = signal('');

  readonly userOptions = computed(() =>
    this.users().map((u) => ({ value: u.id, label: `${u.displayName} (${u.email || u.mobilePhone || ''})` }))
  );

  constructor() {
    this.loadUsers();
  }

  kindOptions(): { value: PersonKind; label: string }[] {
    return [
      { value: 'Teacher', label: this.locale.t('common.teacher') },
      { value: 'Student', label: this.locale.t('common.student') }
    ];
  }

  setKind(kind: PersonKind): void {
    if (!kind || kind === this.kind()) return;
    this.kind.set(kind);
    this.userId.set('');
    this.loadUsers();
  }

  private loadUsers(): void {
    const kind = this.kind();
    this.error.set('');
    this.users.set([]);
    this.api.getUsers(kind).subscribe({
      next: (users) => {
        if (kind === this.kind()) this.users.set(users);
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'availability.loadFailed'))
    });
  }
}
