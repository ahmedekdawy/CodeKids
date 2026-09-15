import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../auth.service';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Course, CourseLesson } from '../../models';
import { LanguageSwitcherComponent } from '../../shared/language-switcher/language-switcher.component';
import { ThemeSwitcherComponent } from '../../shared/theme-switcher/theme-switcher.component';
import { SiteBrandComponent } from '../../shared/site-brand/site-brand.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { ApiBusyIndicatorComponent } from '../../shared/api-busy-indicator/api-busy-indicator.component';

interface LessonGroup {
  id: string;
  title: string;
  lessons: CourseLesson[];
}

@Component({
  selector: 'app-student-lessons',
  imports: [
    RouterLink,
    TranslatePipe,
    LanguageSwitcherComponent,
    ThemeSwitcherComponent,
    SiteBrandComponent,
    ApiBusyIndicatorComponent
  ],
  templateUrl: './student-lessons.component.html',
  styleUrl: './student-lessons.component.css'
})
export class StudentLessonsComponent {
  readonly auth = inject(AuthService);
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  private readonly route = inject(ActivatedRoute);

  readonly course = signal<Course | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');

  readonly groups = computed<LessonGroup[]>(() => {
    const course = this.course();
    if (!course) return [];

    const units = [...(course.units ?? [])].sort((a, b) => a.sortOrder - b.sortOrder || a.title.localeCompare(b.title));
    if (units.length) {
      return units
        .map((unit) => {
          const fromUnit = unit.lessons?.length
            ? unit.lessons
            : (course.lessons ?? []).filter((lesson) => lesson.unitId === unit.id);
          return {
            id: unit.id,
            title: unit.title,
            lessons: [...fromUnit].sort((a, b) => a.sortOrder - b.sortOrder || a.title.localeCompare(b.title))
          };
        })
        .filter((group) => group.lessons.length);
    }

    const lessons = [...(course.lessons ?? [])].sort((a, b) => a.sortOrder - b.sortOrder || a.title.localeCompare(b.title));
    return lessons.length ? [{ id: course.id, title: '', lessons }] : [];
  });

  constructor() {
    const courseId = this.route.snapshot.paramMap.get('courseId')!;
    this.api.getCourse(courseId).subscribe({
      next: (course) => {
        this.course.set(course);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.locale.t('play.courseNotFound'));
        this.loading.set(false);
      }
    });
  }
}
