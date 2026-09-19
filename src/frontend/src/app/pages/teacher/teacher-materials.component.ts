import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LearningApiService } from '../../learning-api.service';
import { LocaleService } from '../../i18n/locale.service';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { MaterialViewerComponent } from '../../shared/material-viewer/material-viewer.component';
import { Course, CourseLesson, CourseUnit, LearningMaterial, TeacherLearningMaterial } from '../../models';
import { formatCourseLabel } from '../../grade.util';

@Component({
  selector: 'app-teacher-materials',
  imports: [
    FormsModule,
    PageFeedbackComponent,
    SearchableSelectComponent,
    TranslatePipe,
    MaterialViewerComponent
  ],
  templateUrl: './teacher-materials.component.html',
  styleUrls: ['./teacher-panel.css', './teacher-videos.component.css', './teacher-materials.component.css']
})
export class TeacherMaterialsComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  readonly courses = signal<Course[]>([]);
  readonly materials = signal<TeacherLearningMaterial[]>([]);
  readonly info = signal('');
  readonly error = signal('');
  readonly uploading = signal(false);
  readonly uploadProgress = signal<number | null>(null);
  readonly file = signal<File | null>(null);

  selectedCourseId = '';
  selectedUnitId = '';
  selectedLessonId = '';
  materialTitle = '';

  filterCourseId = '';
  filterSearch = '';

  readonly preview = signal<LearningMaterial | null>(null);
  readonly previewKey = signal(0);

  readonly filteredMaterials = computed(() => {
    let list = this.materials();
    if (this.filterCourseId) {
      list = list.filter((m) => m.courseId === this.filterCourseId);
    }
    const q = this.filterSearch.trim().toLowerCase();
    if (q) {
      list = list.filter(
        (m) =>
          m.title.toLowerCase().includes(q) ||
          m.fileName.toLowerCase().includes(q) ||
          m.courseTitle.toLowerCase().includes(q) ||
          (m.unitTitle ?? '').toLowerCase().includes(q) ||
          (m.lessonTitle ?? '').toLowerCase().includes(q)
      );
    }
    return list;
  });

  constructor() {
    this.reload();
  }

  reload(): void {
    this.api.getCourses().subscribe({
      next: (courses) => {
        this.courses.set(courses);
        if (!this.selectedCourseId && courses[0]) {
          this.selectedCourseId = courses[0].id;
          this.selectFirstUnit();
        }
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'materials.loadCoursesFailed'))
    });
    this.api.getLearningMaterials().subscribe({
      next: (materials) => this.materials.set(materials),
      error: (err) => this.error.set(this.locale.fromApiError(err, 'materials.loadFailed'))
    });
  }

  unitsForCourse(courseId = this.selectedCourseId): CourseUnit[] {
    const units = [...(this.courses().find((c) => c.id === courseId)?.units ?? [])];
    return units.sort((a, b) => a.sortOrder - b.sortOrder || a.title.localeCompare(b.title));
  }

  lessonsForUnit(courseId = this.selectedCourseId, unitId = this.selectedUnitId): CourseLesson[] {
    const course = this.courses().find((c) => c.id === courseId);
    if (!course) return [];
    const units = this.unitsForCourse(courseId);
    if (!units.length) {
      return [...(course.lessons ?? [])].sort(
        (a, b) => a.sortOrder - b.sortOrder || a.title.localeCompare(b.title)
      );
    }
    if (!unitId) return [];
    const fromUnit = course.units?.find((u) => u.id === unitId)?.lessons;
    const lessons = fromUnit?.length
      ? fromUnit
      : (course.lessons ?? []).filter((l) => l.unitId === unitId);
    return [...lessons].sort((a, b) => a.sortOrder - b.sortOrder || a.title.localeCompare(b.title));
  }

  courseLabel(course: Course): string {
    return formatCourseLabel(
      (k, p) => this.locale.t(k, p),
      course.title,
      course.grade,
      'common.allGrades',
      course.stageId
    );
  }

  onCourseChange(): void {
    this.selectFirstUnit();
  }

  onUnitChange(): void {
    this.selectedLessonId = '';
  }

  private selectFirstUnit(): void {
    this.selectedUnitId = this.unitsForCourse()[0]?.id || '';
    this.selectedLessonId = '';
  }

  scopeLabel(material: TeacherLearningMaterial): string {
    if (material.scope === 'lesson') {
      return `${this.locale.t('materials.scopeUnit')}: ${material.unitTitle ?? ''} · ${this.locale.t('materials.scopeLesson')}: ${material.lessonTitle ?? ''}`;
    }
    if (material.scope === 'unit') {
      return `${this.locale.t('materials.scopeUnit')}: ${material.unitTitle ?? ''}`;
    }
    return this.locale.t('materials.scopeCourse');
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.file.set(input.files?.[0] ?? null);
  }

  attachMaterial(): void {
    const file = this.file();
    if (!file) {
      this.error.set(this.locale.t('materials.fileRequired'));
      return;
    }
    if (!this.selectedCourseId) {
      this.error.set(this.locale.t('materials.courseRequired'));
      return;
    }

    this.error.set('');
    this.info.set('');
    this.uploading.set(true);
    this.uploadProgress.set(0);

    this.api.uploadLearningMaterialFile(file).subscribe({
      next: (asset) => {
        this.uploadProgress.set(null);
        this.api
          .attachLearningMaterial({
            courseId: this.selectedCourseId || null,
            unitId: this.selectedUnitId || null,
            lessonId: this.selectedLessonId || null,
            mediaAssetId: asset.id,
            title: this.materialTitle.trim() || null,
            sortOrder: 1
          })
          .subscribe({
            next: () => {
              this.uploading.set(false);
              this.info.set(this.locale.t('materials.uploaded'));
              this.materialTitle = '';
              this.file.set(null);
              this.api.getLearningMaterials().subscribe((materials) => this.materials.set(materials));
            },
            error: (err) => {
              this.uploading.set(false);
              this.error.set(this.locale.fromApiError(err, 'materials.attachFailed'));
            }
          });
      },
      error: (err) => {
        this.uploading.set(false);
        this.uploadProgress.set(null);
        this.error.set(this.locale.fromApiError(err, 'materials.uploadFailed'));
      }
    });
  }

  deleteMaterial(material: TeacherLearningMaterial): void {
    if (!confirm(this.locale.t('materials.confirmDelete', { title: material.title }))) return;
    this.error.set('');
    this.api.deleteLearningMaterial(material.id).subscribe({
      next: () => {
        this.info.set(this.locale.t('materials.deleted'));
        if (this.preview()?.id === material.id) this.preview.set(null);
        this.materials.update((list) => list.filter((m) => m.id !== material.id));
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'materials.deleteFailed'))
    });
  }

  openPreview(material: LearningMaterial): void {
    this.preview.set(material);
    this.previewKey.update((k) => k + 1);
  }

  closePreview(): void {
    this.preview.set(null);
  }

  formatBytes(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  kindIcon(kind: string): string {
    if (kind === 'Pdf') return '📄';
    if (kind === 'Audio') return '🎧';
    return '🖼️';
  }
}
