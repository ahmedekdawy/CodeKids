import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../auth.service';
import { LearningApiService } from '../../learning-api.service';
import { Assignment, Avatar, Badge, Course, LiveSession, StudentSummary } from '../../models';

@Component({
  selector: 'app-student-home',
  imports: [RouterLink],
  templateUrl: './student-home.component.html',
  styleUrl: './student-home.component.css'
})
export class StudentHomeComponent {
  readonly auth = inject(AuthService);
  private readonly api = inject(LearningApiService);

  readonly courses = signal<Course[]>([]);
  readonly summary = signal<StudentSummary | null>(null);
  readonly badges = signal<Badge[]>([]);
  readonly avatars = signal<Avatar[]>([]);
  readonly meetings = signal<LiveSession[]>([]);
  readonly assignments = signal<Assignment[]>([]);

  constructor() {
    this.api.getCourses().subscribe((courses) => this.courses.set(courses));
    this.api.getStudentSummary().subscribe((summary) => this.summary.set(summary));
    this.api.getBadges().subscribe((badges) => this.badges.set(badges));
    this.api.getAvatars().subscribe((avatars) => this.avatars.set(avatars));
    this.api.getMeetings().subscribe((meetings) => this.meetings.set(meetings));
    this.api.getAssignments().subscribe((assignments) => this.assignments.set(assignments));
  }

  selectAvatar(avatar: Avatar): void {
    if (!avatar.isUnlocked) return;
    this.api.selectAvatar(avatar.id).subscribe(() => {
      this.api.getAvatars().subscribe((avatars) => this.avatars.set(avatars));
    });
  }

  formatWhen(iso: string): string {
    return new Date(iso).toLocaleString();
  }
}
