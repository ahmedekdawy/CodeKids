import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../auth.service';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { LearningMaterial } from '../../models';
import { LanguageSwitcherComponent } from '../../shared/language-switcher/language-switcher.component';
import { ThemeSwitcherComponent } from '../../shared/theme-switcher/theme-switcher.component';
import { SiteBrandComponent } from '../../shared/site-brand/site-brand.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { ApiBusyIndicatorComponent } from '../../shared/api-busy-indicator/api-busy-indicator.component';
import { MaterialViewerComponent } from '../../shared/material-viewer/material-viewer.component';

interface MaterialGroup {
  id: string;
  title: string;
  items: LearningMaterial[];
}

@Component({
  selector: 'app-student-materials',
  imports: [
    RouterLink,
    TranslatePipe,
    LanguageSwitcherComponent,
    ThemeSwitcherComponent,
    SiteBrandComponent,
    ApiBusyIndicatorComponent,
    MaterialViewerComponent
  ],
  templateUrl: './student-materials.component.html',
  styleUrls: ['../student-lessons/student-lessons.component.css', './student-materials.component.css']
})
export class StudentMaterialsComponent {
  readonly auth = inject(AuthService);
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  private readonly route = inject(ActivatedRoute);

  readonly courseTitle = signal('');
  readonly loading = signal(true);
  readonly error = signal('');
  readonly materials = signal<LearningMaterial[]>([]);

  readonly unitTitles = signal<Map<string, string>>(new Map());
  readonly lessonTitles = signal<Map<string, string>>(new Map());

  readonly openMaterial = signal<LearningMaterial | null>(null);
  readonly previewKey = signal(0);

  readonly groups = computed<MaterialGroup[]>(() => {
    const items = this.materials();
    const unitTitles = this.unitTitles();
    const lessonTitles = this.lessonTitles();

    const courseLevel = items.filter((m) => !m.unitId && !m.lessonId);
    const byUnit = new Map<string, LearningMaterial[]>();
    const byLesson = new Map<string, LearningMaterial[]>();

    for (const item of items) {
      if (item.lessonId) {
        const list = byLesson.get(item.lessonId) ?? [];
        list.push(item);
        byLesson.set(item.lessonId, list);
      } else if (item.unitId) {
        const list = byUnit.get(item.unitId) ?? [];
        list.push(item);
        byUnit.set(item.unitId, list);
      }
    }

    const groups: MaterialGroup[] = [];
    if (courseLevel.length) {
      groups.push({ id: 'course', title: '', items: courseLevel });
    }
    for (const [unitId, unitItems] of byUnit) {
      groups.push({
        id: `unit-${unitId}`,
        title: unitTitles.get(unitId)
          ? `${this.locale.t('materials.scopeUnit')}: ${unitTitles.get(unitId)}`
          : '',
        items: unitItems
      });
    }
    for (const [lessonId, lessonItems] of byLesson) {
      groups.push({
        id: `lesson-${lessonId}`,
        title: lessonTitles.get(lessonId)
          ? `${this.locale.t('materials.scopeLesson')}: ${lessonTitles.get(lessonId)}`
          : '',
        items: lessonItems
      });
    }
    return groups;
  });

  constructor() {
    const courseId = this.route.snapshot.paramMap.get('courseId')!;
    this.api.getLearningMaterialPage({ courseId, all: true }).subscribe({
      next: (page) => {
        this.courseTitle.set(page.courseTitle);
        this.materials.set(page.items);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(this.locale.fromApiError(err, 'materials.loadFailed'));
        this.loading.set(false);
      }
    });
    this.api.getCourse(courseId).subscribe({
      next: (course) => {
        const units = new Map<string, string>();
        for (const unit of course.units ?? []) {
          units.set(unit.id, unit.title);
        }
        const lessons = new Map<string, string>();
        const unitLessons = (course.units ?? []).flatMap((u) => u.lessons ?? []);
        for (const lesson of [...(course.lessons ?? []), ...unitLessons]) {
          lessons.set(lesson.id, lesson.title);
        }
        this.unitTitles.set(units);
        this.lessonTitles.set(lessons);
      },
      error: () => {
        // Titles stay empty; material rows still render.
      }
    });
  }

  open(material: LearningMaterial): void {
    this.openMaterial.set(material);
    this.previewKey.update((k) => k + 1);
  }

  close(): void {
    this.openMaterial.set(null);
  }

  kindIcon(kind: string): string {
    if (kind === 'Pdf') return '📄';
    if (kind === 'Audio') return '🎧';
    return '🖼️';
  }
}
