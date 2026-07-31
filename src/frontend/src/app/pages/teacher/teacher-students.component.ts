import { Component, inject, signal } from '@angular/core';
import { LearningApiService } from '../../learning-api.service';
import { TeacherDashboard } from '../../models';

@Component({
  selector: 'app-teacher-students',
  templateUrl: './teacher-students.component.html',
  styleUrl: './teacher-panel.css'
})
export class TeacherStudentsComponent {
  private readonly api = inject(LearningApiService);
  readonly dashboard = signal<TeacherDashboard | null>(null);

  constructor() {
    this.api.getTeacherDashboard().subscribe((dashboard) => this.dashboard.set(dashboard));
  }
}
