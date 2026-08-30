import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../auth.service';
import { LearningApiService } from '../../learning-api.service';
import { Assignment, Avatar, Badge, Classroom, Course, CourseTerm, Exam, LiveSession, StudentSummary } from '../../models';
import { LanguageSwitcherComponent } from '../../shared/language-switcher/language-switcher.component';
import { ThemeSwitcherComponent } from '../../shared/theme-switcher/theme-switcher.component';
import { SiteBrandComponent } from '../../shared/site-brand/site-brand.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { LocaleService } from '../../i18n/locale.service';
import { formatGradeLabel } from '../../grade.util';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { StudentAskPanelComponent } from '../../shared/student-ask-panel/student-ask-panel.component';
import { NotificationBellComponent } from '../../shared/notification-bell/notification-bell.component';
import { ApiBusyIndicatorComponent } from '../../shared/api-busy-indicator/api-busy-indicator.component';

@Component({
  selector: 'app-student-home',
  imports: [
    SearchableSelectComponent,
    FormsModule,
    RouterLink,
    TranslatePipe,
    LanguageSwitcherComponent,
    ThemeSwitcherComponent,
    SiteBrandComponent,
    StudentAskPanelComponent,
    NotificationBellComponent,
    ApiBusyIndicatorComponent
  ],
  templateUrl: './student-home.component.html',
  styleUrl: './student-home.component.css'
})
export class StudentHomeComponent {
  readonly auth = inject(AuthService);
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  readonly courses = signal<Course[]>([]);
  readonly summary = signal<StudentSummary | null>(null);
  readonly badges = signal<Badge[]>([]);
  readonly avatars = signal<Avatar[]>([]);
  readonly meetings = signal<LiveSession[]>([]);
  readonly classrooms = signal<Classroom[]>([]);
  readonly assignments = signal<Assignment[]>([]);
  readonly exams = signal<Exam[]>([]);

  readonly selectedAvatar = computed(() => this.avatars().find((a) => a.isSelected) ?? null);
  readonly earnedBadges = computed(() => this.badges().filter((b) => b.isEarned));
  readonly openTasks = computed(() => this.assignments().length + this.exams().length);
  readonly classroomsWithZoom = computed(() =>
    this.classrooms().filter((room) => (room.zoomMeetingLink || '').trim().length > 0)
  );

  constructor() {
    this.api.getCourses().subscribe((courses) => this.courses.set(courses));
    this.api.getStudentSummary().subscribe((summary) => this.summary.set(summary));
    this.api.getBadges().subscribe((badges) => this.badges.set(badges));
    this.api.getAvatars().subscribe((avatars) => this.avatars.set(avatars));
    this.api.getMeetings().subscribe((meetings) => this.meetings.set(meetings));
    this.api.getClassrooms().subscribe((classrooms) => this.classrooms.set(classrooms));
    this.api.getAssignments().subscribe((assignments) => this.assignments.set(assignments));
    this.api.getExams().subscribe((exams) => this.exams.set(exams));
  }

  selectAvatar(avatar: Avatar): void {
    if (!avatar.isUnlocked || avatar.isSelected) return;
    this.api.selectAvatar(avatar.id).subscribe(() => {
      this.api.getAvatars().subscribe((avatars) => this.avatars.set(avatars));
    });
  }

  selectAvatarById(id: string | number | null): void {
    const avatar = this.avatars().find((a) => a.id === String(id ?? ''));
    if (avatar) this.selectAvatar(avatar);
  }

  avatarOptionLabel(option: Avatar): string {
    const base = `${option.emoji} ${option.name}`;
    return option.isUnlocked
      ? base
      : `${base} (${this.locale.t('student.needsXp', { xp: option.unlockXp })})`;
  }

  formatWhen(iso: string): string {
    return new Date(iso).toLocaleString(this.locale.lang());
  }

  termLabel(term: CourseTerm | string | null | undefined): string {
    if (!term) return this.locale.t('student.allTerms');
    if (term === 'FirstTerm') return this.locale.t('student.firstTerm');
    if (term === 'SecondTerm') return this.locale.t('student.secondTerm');
    return this.locale.t('student.fullYear');
  }

  gradeLabel(grade: number | null | undefined): string {
    return formatGradeLabel((k, p) => this.locale.t(k, p), grade, 'student.allGrades');
  }
}
