import { Component, inject, signal } from '@angular/core';
import { AuthService } from '../../auth.service';
import { LearningApiService } from '../../learning-api.service';
import { LiveSession, ParentDashboard } from '../../models';

@Component({
  selector: 'app-parent-dashboard',
  templateUrl: './parent-dashboard.component.html',
  styleUrl: './parent-dashboard.component.css'
})
export class ParentDashboardComponent {
  readonly auth = inject(AuthService);
  private readonly api = inject(LearningApiService);
  readonly dashboard = signal<ParentDashboard | null>(null);
  readonly meetings = signal<LiveSession[]>([]);

  constructor() {
    this.api.getParentDashboard().subscribe((dashboard) => this.dashboard.set(dashboard));
    this.api.getMeetings().subscribe((meetings) => this.meetings.set(meetings));
  }

  formatWhen(iso: string): string {
    return new Date(iso).toLocaleString();
  }
}
