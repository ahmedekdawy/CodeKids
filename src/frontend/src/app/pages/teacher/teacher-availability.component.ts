import { Component, computed, inject } from '@angular/core';
import { AuthService } from '../../auth.service';
import { AvailabilityEditorComponent } from '../../shared/availability-editor/availability-editor.component';
import { TranslatePipe } from '../../shared/translate.pipe';

@Component({
  selector: 'app-teacher-availability',
  imports: [TranslatePipe, AvailabilityEditorComponent],
  template: `
    <div class="panel-page">
      <h2>{{ 'availability.title' | t }}</h2>
      <p class="meta">{{ 'availability.teacherSubtitle' | t }}</p>

      @if (userId(); as id) {
        <app-availability-editor [userId]="id" [showVacations]="true" />
      }
    </div>
  `,
  styleUrl: './teacher-panel.css'
})
export class TeacherAvailabilityComponent {
  private readonly auth = inject(AuthService);

  readonly userId = computed(() => this.auth.user()?.id ?? '');
}
