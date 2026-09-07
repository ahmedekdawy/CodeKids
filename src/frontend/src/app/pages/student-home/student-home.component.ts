import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../auth.service';
import { LearningApiService } from '../../learning-api.service';
import { Assignment, Avatar, Badge, Classroom, Course, CourseQuiz, CourseTerm, CourseVideoSummary, Exam, StudentSummary } from '../../models';
import { LanguageSwitcherComponent } from '../../shared/language-switcher/language-switcher.component';
import { ThemeSwitcherComponent } from '../../shared/theme-switcher/theme-switcher.component';
import { SiteBrandComponent } from '../../shared/site-brand/site-brand.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { LocaleService } from '../../i18n/locale.service';
import { formatGradeLabel } from '../../grade.util';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { StudentAskPanelComponent, StudentAskCourseChoice } from '../../shared/student-ask-panel/student-ask-panel.component';
import { NotificationBellComponent } from '../../shared/notification-bell/notification-bell.component';
import { ApiBusyIndicatorComponent } from '../../shared/api-busy-indicator/api-busy-indicator.component';
import { UserPhotoComponent } from '../../shared/user-photo/user-photo.component';
import { PROFILE_PHOTO_MAX_BYTES, PROFILE_PHOTO_TYPES } from '../../shared/user-photo/profile-photo.rules';

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
    ApiBusyIndicatorComponent,
    UserPhotoComponent
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
  readonly classrooms = signal<Classroom[]>([]);
  readonly assignments = signal<Assignment[]>([]);
  readonly exams = signal<Exam[]>([]);

  readonly photoBusy = signal(false);
  readonly photoError = signal('');

  readonly hasPhoto = computed(() => !!this.auth.user()?.profilePhotoUrl);
  readonly selectedAvatar = computed(() => this.avatars().find((a) => a.isSelected) ?? null);
  readonly earnedBadges = computed(() => this.badges().filter((b) => b.isEarned));
  readonly publishedAssignments = computed(() => this.assignments().filter((a) => a.isPublished === true));
  readonly publishedExams = computed(() => this.exams().filter((e) => e.isPublished === true));
  readonly leftoverAssignments = computed(() =>
    this.publishedAssignments().filter((assignment) => !this.courses().some((course) => this.assignmentBelongsToCourse(assignment, course)))
  );
  readonly leftoverExams = computed(() =>
    this.publishedExams().filter((exam) => !this.courses().some((course) => this.examBelongsToCourse(exam, course)))
  );
  readonly askCourses = computed<StudentAskCourseChoice[]>(() =>
    this.courses()
      .filter((course) => course.studentAskEnabled)
      .map((course) => ({ id: course.id, title: course.title }))
  );

  constructor() {
    this.api.getCourses().subscribe((courses) => this.courses.set(courses));
    this.api.getStudentSummary().subscribe((summary) => this.summary.set(summary));
    this.api.getBadges().subscribe((badges) => this.badges.set(badges));
    this.api.getAvatars().subscribe((avatars) => this.avatars.set(avatars));
    this.api.getClassrooms().subscribe((classrooms) => this.classrooms.set(classrooms));
    this.api.getAssignments().subscribe((assignments) => this.assignments.set(assignments));
    this.api.getExams().subscribe((exams) => this.exams.set(exams));
  }

  onPhotoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    this.photoError.set('');
    if (!PROFILE_PHOTO_TYPES.includes(file.type)) {
      this.photoError.set(this.locale.t('student.photo.invalidType'));
      return;
    }
    if (file.size > PROFILE_PHOTO_MAX_BYTES) {
      this.photoError.set(this.locale.t('student.photo.tooLarge'));
      return;
    }

    this.photoBusy.set(true);
    this.auth.uploadProfilePhoto(file).subscribe({
      next: () => this.photoBusy.set(false),
      error: (err) => {
        this.photoBusy.set(false);
        this.photoError.set(this.locale.fromApiError(err, 'student.photo.uploadFailed'));
      }
    });
  }

  removePhoto(): void {
    this.photoError.set('');
    this.photoBusy.set(true);
    this.auth.removeProfilePhoto().subscribe({
      next: () => this.photoBusy.set(false),
      error: (err) => {
        this.photoBusy.set(false);
        this.photoError.set(this.locale.fromApiError(err, 'student.photo.removeFailed'));
      }
    });
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

  courseAssignments(course: Course): Assignment[] {
    return this.publishedAssignments().filter((assignment) => this.assignmentBelongsToCourse(assignment, course));
  }

  courseExams(course: Course): Exam[] {
    return this.publishedExams().filter((exam) => this.examBelongsToCourse(exam, course));
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

  courseVideos(course: Course): CourseVideoSummary[] {
    return course.videos ?? [];
  }

  publishedQuizzes(course: Course): CourseQuiz[] {
    return (course.quizzes ?? []).filter((quiz) => quiz.isPublished === true);
  }

  private assignmentBelongsToCourse(assignment: Assignment, course: Course): boolean {
    return this.classroomLinksOnlyCourse(assignment.classroomId, course.id);
  }

  private examBelongsToCourse(exam: Exam, course: Course): boolean {
    if (exam.courseId) return exam.courseId === course.id;
    return this.classroomLinksOnlyCourse(exam.classroomId, course.id);
  }

  /** Match a classroom task to a subject only when the room clearly points at that one course. */
  private classroomLinksOnlyCourse(classroomId: string, courseId: string): boolean {
    const room = this.classrooms().find((classroom) => classroom.id === classroomId);
    if (!room) return false;
    if (room.courseId) return room.courseId === courseId;
    const linked = room.courses ?? [];
    if (linked.length === 1) return linked[0].courseId === courseId;
    return false;
  }
}
