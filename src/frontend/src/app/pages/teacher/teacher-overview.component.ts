import { Component, inject, signal } from '@angular/core';
import { LearningApiService } from '../../learning-api.service';
import { Classroom, TeacherDashboard } from '../../models';

@Component({
  selector: 'app-teacher-overview',
  templateUrl: './teacher-overview.component.html',
  styleUrl: './teacher-panel.css'
})
export class TeacherOverviewComponent {
  private readonly api = inject(LearningApiService);
  readonly dashboard = signal<TeacherDashboard | null>(null);
  readonly classrooms = signal<Classroom[]>([]);

  constructor() {
    this.api.getTeacherDashboard().subscribe((dashboard) => this.dashboard.set(dashboard));
    this.api.getClassrooms().subscribe((classrooms) => this.classrooms.set(classrooms));
  }
}
