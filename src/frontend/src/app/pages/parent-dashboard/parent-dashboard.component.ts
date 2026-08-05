import { Component, inject, signal } from '@angular/core';
import { AuthService } from '../../auth.service';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { LiveSession, ParentDashboard } from '../../models';
import { SiteBrandComponent } from '../../shared/site-brand/site-brand.component';
import { TranslatePipe } from '../../shared/translate.pipe';

@Component({
  selector: 'app-parent-dashboard',
  imports: [TranslatePipe, SiteBrandComponent],
  templateUrl: './parent-dashboard.component.html',
  styleUrl: './parent-dashboard.component.css'
})
export class ParentDashboardComponent {
  readonly auth = inject(AuthService);
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  readonly dashboard = signal<ParentDashboard | null>(null);
  readonly meetings = signal<LiveSession[]>([]);

  constructor() {
    this.api.getParentDashboard().subscribe((dashboard) => this.dashboard.set(dashboard));
    this.api.getMeetings().subscribe((meetings) => this.meetings.set(meetings));
  }

  formatWhen(iso: string): string {
    return new Date(iso).toLocaleString(this.locale.lang());
  }
}
