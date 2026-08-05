import { Component, inject, signal } from '@angular/core';
import { LearningApiService } from '../../learning-api.service';
import { Classroom, ClassroomDiagnosis, TeacherDashboard } from '../../models';
import { TranslatePipe } from '../../shared/translate.pipe';

@Component({
  selector: 'app-teacher-overview',
  imports: [TranslatePipe],
  templateUrl: './teacher-overview.component.html',
  styleUrl: './teacher-panel.css'
})
export class TeacherOverviewComponent {
  private readonly api = inject(LearningApiService);
  readonly dashboard = signal<TeacherDashboard | null>(null);
  readonly classrooms = signal<Classroom[]>([]);
  readonly diagnosis = signal<ClassroomDiagnosis | null>(null);

  constructor() {
    this.api.getTeacherDashboard().subscribe((dashboard) => this.dashboard.set(dashboard));
    this.api.getClassrooms().subscribe((classrooms) => {
      this.classrooms.set(classrooms);
      if (classrooms[0]) this.loadDiagnosis(classrooms[0].id);
    });
  }

  loadDiagnosis(classroomId: string): void {
    this.api.getClassroomDiagnosis(classroomId).subscribe({
      next: (diagnosis) => this.diagnosis.set(diagnosis),
      error: () => this.diagnosis.set(null)
    });
  }
}
