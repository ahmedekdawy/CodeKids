import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LearningApiService } from '../../learning-api.service';
import { LocaleService } from '../../i18n/locale.service';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import { ProtectedVideoPlayerComponent } from '../../shared/protected-video-player/protected-video-player.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import {
  Assignment,
  Course,
  CourseLesson,
  CourseUnit,
  TeacherLessonVideo,
  TeacherSolutionVideo,
  WatchSession
} from '../../models';
import { formatCourseLabel } from '../../grade.util';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';

type VideoTab = 'lesson' | 'solution' | 'analytics';

interface VideoPreview {
  mediaAssetId: string;
  title: string;
  lessonId?: string | null;
}

@Component({
  selector: 'app-teacher-videos',
  imports: [PageFeedbackComponent, SearchableSelectComponent, FormsModule, IconActionButtonComponent, ProtectedVideoPlayerComponent, TranslatePipe],
  templateUrl: './teacher-videos.component.html',
  styleUrls: ['./teacher-panel.css', './teacher-videos.component.css']
})
export class TeacherVideosComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  readonly courses = signal<Course[]>([]);
  readonly assignments = signal<Assignment[]>([]);
  readonly lessonVideos = signal<TeacherLessonVideo[]>([]);
  readonly solutionVideos = signal<TeacherSolutionVideo[]>([]);
  readonly watchSessions = signal<WatchSession[]>([]);
  readonly info = signal('');
  readonly error = signal('');
  readonly uploading = signal(false);
  readonly lessonVideoFile = signal<File | null>(null);
  readonly solutionVideoFile = signal<File | null>(null);
  readonly tab = signal<VideoTab>('lesson');
  readonly preview = signal<VideoPreview | null>(null);
  readonly previewKey = signal(0);

  selectedCourseId = '';
  selectedUnitId = '';
  selectedLessonId = '';
  videoTitle = '';
  lessonVideoUrl = '';
  solutionVideoUrl = '';
  selectedAssignmentId = '';
  selectedMediaAssetId = '';

  lessonFilterCourseId = '';
  lessonFilterUnitId = '';
  lessonFilterLessonId = '';
  lessonSearch = '';

  solutionFilterClassroomId = '';
  solutionSearch = '';

  analyticsSearch = '';

  readonly filteredLessonVideos = computed(() => {
    let list = this.lessonVideos();
    if (this.lessonFilterCourseId) {
      list = list.filter((v) => v.courseId === this.lessonFilterCourseId);
    }
    if (this.lessonFilterUnitId) {
      const lessonIds = new Set(this.lessonsForUnit(this.lessonFilterCourseId, this.lessonFilterUnitId).map((l) => l.id));
      list = list.filter((v) => lessonIds.has(v.lessonId));
    }
    if (this.lessonFilterLessonId) {
      list = list.filter((v) => v.lessonId === this.lessonFilterLessonId);
    }
    const q = this.lessonSearch.trim().toLowerCase();
    if (q) {
      list = list.filter(
        (v) =>
          v.title.toLowerCase().includes(q) ||
          v.fileName.toLowerCase().includes(q) ||
          v.courseTitle.toLowerCase().includes(q) ||
          v.lessonTitle.toLowerCase().includes(q)
      );
    }
    return list;
  });

  readonly filteredSolutionVideos = computed(() => {
    let list = this.solutionVideos();
    if (this.solutionFilterClassroomId) {
      list = list.filter((v) => v.classroomId === this.solutionFilterClassroomId);
    }
    const q = this.solutionSearch.trim().toLowerCase();
    if (q) {
      list = list.filter(
        (v) =>
          v.assignmentTitle.toLowerCase().includes(q) ||
          v.fileName.toLowerCase().includes(q) ||
          v.classroomName.toLowerCase().includes(q)
      );
    }
    return list;
  });

  readonly classroomFilterOptions = computed(() => {
    const map = new Map<string, string>();
    for (const v of this.solutionVideos()) {
      map.set(v.classroomId, v.classroomName);
    }
    return [...map.entries()]
      .map(([id, name]) => ({ id, name }))
      .sort((a, b) => a.name.localeCompare(b.name));
  });

  readonly filteredWatchSessions = computed(() => {
    const q = this.analyticsSearch.trim().toLowerCase();
    const list = this.watchSessions();
    if (!q) return list;
    return list.filter((s) => s.studentName.toLowerCase().includes(q));
  });

  constructor() {
    this.api.getCourses().subscribe((courses) => {
      this.courses.set(courses);
      if (courses[0]) {
        this.selectedCourseId = courses[0].id;
        this.selectFirstUnitAndLesson(courses[0].id);
      }
    });
    this.api.getAssignments().subscribe((assignments) => {
      this.assignments.set(assignments);
      if (assignments[0]) this.selectedAssignmentId = assignments[0].id;
    });
    this.reloadLibrary();
  }

  setTab(tab: VideoTab): void {
    this.tab.set(tab);
    this.error.set('');
    this.info.set('');
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
      return [...(course.lessons ?? [])].sort((a, b) => a.sortOrder - b.sortOrder || a.title.localeCompare(b.title));
    }
    if (!unitId) return [];
    const fromUnit = course.units?.find((u) => u.id === unitId)?.lessons;
    const lessons = fromUnit?.length
      ? fromUnit
      : (course.lessons ?? []).filter((l) => l.unitId === unitId);
    return [...lessons].sort((a, b) => a.sortOrder - b.sortOrder || a.title.localeCompare(b.title));
  }

  private selectFirstUnitAndLesson(courseId: string): void {
    const units = this.unitsForCourse(courseId);
    this.selectedUnitId = units[0]?.id || '';
    this.selectedLessonId = this.lessonsForUnit(courseId, this.selectedUnitId)[0]?.id || '';
  }

  courseLabel(course: Course): string {
    return formatCourseLabel((k, p) => this.locale.t(k, p), course.title, course.grade, 'common.allGrades', course.stageId);
  }

  assignmentOptionLabel(assignment: Assignment): string {
    return assignment.solutionVideoMediaAssetId
      ? `${assignment.title} (${this.locale.t('videos.hasSolution')})`
      : assignment.title;
  }

  courseLabelById(courseId: string, fallbackTitle?: string | null): string {
    const course = this.courses().find((c) => c.id === courseId);
    if (course) return this.courseLabel(course);
    return formatCourseLabel((k, p) => this.locale.t(k, p), fallbackTitle, null);
  }

  lessonFilterOptions(): { id: string; title: string }[] {
    if (!this.lessonFilterCourseId) {
      const map = new Map<string, string>();
      for (const v of this.lessonVideos()) {
        map.set(v.lessonId, v.lessonTitle);
      }
      return [...map.entries()]
        .map(([id, title]) => ({ id, title }))
        .sort((a, b) => a.title.localeCompare(b.title));
    }
    if (this.lessonFilterUnitId) {
      return this.lessonsForUnit(this.lessonFilterCourseId, this.lessonFilterUnitId).map((l) => ({
        id: l.id,
        title: l.title
      }));
    }
    return [];
  }

  onCourseChange(): void {
    this.selectFirstUnitAndLesson(this.selectedCourseId);
  }

  onUnitChange(): void {
    this.selectedLessonId = this.lessonsForUnit()[0]?.id || '';
  }

  onLessonFilterCourseChange(): void {
    this.lessonFilterUnitId = '';
    this.lessonFilterLessonId = '';
  }

  onLessonFilterUnitChange(): void {
    this.lessonFilterLessonId = '';
  }

  clearLessonFilters(): void {
    this.lessonFilterCourseId = '';
    this.lessonFilterUnitId = '';
    this.lessonFilterLessonId = '';
    this.lessonSearch = '';
  }

  clearSolutionFilters(): void {
    this.solutionFilterClassroomId = '';
    this.solutionSearch = '';
  }

  formatBytes(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  formatDuration(seconds?: number | null): string {
    if (seconds == null || seconds <= 0) return '—';
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return `${m}:${String(s).padStart(2, '0')}`;
  }

  formatDate(iso: string): string {
    const d = new Date(iso);
    if (Number.isNaN(d.getTime())) return '—';
    return d.toLocaleDateString(this.locale.lang(), { year: 'numeric', month: 'short', day: 'numeric' });
  }

  shownLabel(shown: number, total: number): string {
    return this.locale.t('common.ofShown', { shown, total });
  }

  reloadLibrary(): void {
    this.api.getVideoLibrary().subscribe({
      next: (lib) => {
        this.lessonVideos.set(lib.lessonVideos || []);
        this.solutionVideos.set(lib.solutionVideos || []);
      },
      error: (err) => this.error.set(this.locale.fromApiError(err,'videos.loadLibraryFailed'))
    });
  }

  openPreview(item: VideoPreview): void {
    this.preview.set(item);
    this.previewKey.update((k) => k + 1);
  }

  closePreview(): void {
    this.preview.set(null);
  }

  playLessonVideo(video: TeacherLessonVideo): void {
    this.openPreview({
      mediaAssetId: video.mediaAssetId,
      title: video.title,
      lessonId: video.lessonId
    });
  }

  playSolutionVideo(video: TeacherSolutionVideo): void {
    this.openPreview({
      mediaAssetId: video.mediaAssetId,
      title: `${video.assignmentTitle} — ${this.locale.t('videos.solutionTitleSuffix')}`
    });
  }

  attachLessonVideoLink(): void {
    const url = this.lessonVideoUrl.trim();
    if (!url || !this.selectedLessonId) {
      this.error.set(this.locale.t('videos.linkRequired'));
      return;
    }

    this.uploadLessonAssetFromUrl(url);
  }

  onLessonFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.lessonVideoFile.set(input.files?.[0] ?? null);
  }

  onSolutionFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.solutionVideoFile.set(input.files?.[0] ?? null);
  }

  attachLessonVideoFile(): void {
    const file = this.lessonVideoFile();
    if (!file || !this.selectedLessonId) {
      this.error.set(this.locale.t('videos.fileRequired'));
      return;
    }

    this.error.set('');
    this.info.set('');
    this.uploading.set(true);

    this.api.uploadMedia(file).subscribe({
      next: (asset) => {
        this.selectedMediaAssetId = asset.id;
        this.api
          .attachLessonVideo(this.selectedLessonId, {
            mediaAssetId: asset.id,
            title: this.videoTitle.trim() || asset.fileName,
            sortOrder: 1
          })
          .subscribe({
            next: () => {
              this.uploading.set(false);
              this.info.set(this.locale.t('videos.uploadedLessonFile'));
              this.videoTitle = '';
              this.lessonVideoFile.set(null);
              this.reloadLibrary();
            },
            error: (err) => {
              this.uploading.set(false);
              this.error.set(this.locale.fromApiError(err, 'videos.attachLessonFailed'));
            }
          });
      },
      error: (err) => {
        this.uploading.set(false);
        this.error.set(this.locale.fromApiError(err, 'videos.uploadFailed'));
      }
    });
  }

  private uploadLessonAssetFromUrl(url: string): void {
    this.error.set('');
    this.info.set('');
    this.uploading.set(true);

    this.api.registerMediaFromUrl({ url, title: this.videoTitle.trim() || null }).subscribe({
      next: (asset) => {
        this.selectedMediaAssetId = asset.id;
        this.api
          .attachLessonVideo(this.selectedLessonId, {
            mediaAssetId: asset.id,
            title: this.videoTitle.trim() || asset.fileName,
            sortOrder: 1
          })
          .subscribe({
            next: () => {
              this.uploading.set(false);
              this.info.set(this.locale.t('videos.uploadedLesson'));
              this.videoTitle = '';
              this.lessonVideoUrl = '';
              this.reloadLibrary();
            },
            error: (err) => {
              this.uploading.set(false);
              this.error.set(this.locale.fromApiError(err, 'videos.attachLessonFailed'));
            }
          });
      },
      error: (err) => {
        this.uploading.set(false);
        this.error.set(this.locale.fromApiError(err, 'videos.uploadFailed'));
      }
    });
  }

  attachSolutionVideoLink(): void {
    const url = this.solutionVideoUrl.trim();
    if (!url || !this.selectedAssignmentId) {
      this.error.set(this.locale.t('videos.linkRequired'));
      return;
    }

    this.uploadSolutionAssetFromUrl(url);
  }

  attachSolutionVideoFile(): void {
    const file = this.solutionVideoFile();
    if (!file || !this.selectedAssignmentId) {
      this.error.set(this.locale.t('videos.fileRequired'));
      return;
    }

    this.error.set('');
    this.info.set('');
    this.uploading.set(true);

    this.api.uploadMedia(file).subscribe({
      next: (asset) => {
        this.api.attachAssignmentSolutionVideo(this.selectedAssignmentId, asset.id).subscribe({
          next: () => {
            this.uploading.set(false);
            this.info.set(this.locale.t('videos.uploadedSolutionFile'));
            this.solutionVideoFile.set(null);
            this.api.getAssignments().subscribe((assignments) => this.assignments.set(assignments));
            this.reloadLibrary();
          },
          error: (err) => {
            this.uploading.set(false);
            this.error.set(this.locale.fromApiError(err, 'videos.attachSolutionFailed'));
          }
        });
      },
      error: (err) => {
        this.uploading.set(false);
        this.error.set(this.locale.fromApiError(err, 'videos.uploadFailed'));
      }
    });
  }

  private uploadSolutionAssetFromUrl(url: string): void {
    this.error.set('');
    this.info.set('');
    this.uploading.set(true);

    this.api.registerMediaFromUrl({ url }).subscribe({
      next: (asset) => {
        this.api.attachAssignmentSolutionVideo(this.selectedAssignmentId, asset.id).subscribe({
          next: () => {
            this.uploading.set(false);
            this.info.set(this.locale.t('videos.uploadedSolution'));
            this.solutionVideoUrl = '';
            this.api.getAssignments().subscribe((assignments) => this.assignments.set(assignments));
            this.reloadLibrary();
          },
          error: (err) => {
            this.uploading.set(false);
            this.error.set(this.locale.fromApiError(err, 'videos.attachSolutionFailed'));
          }
        });
      },
      error: (err) => {
        this.uploading.set(false);
        this.error.set(this.locale.fromApiError(err, 'videos.uploadFailed'));
      }
    });
  }

  deleteLessonVideo(video: TeacherLessonVideo): void {
    if (!confirm(this.locale.t('videos.confirmDeleteLesson', { title: video.title }))) return;
    this.error.set('');
    this.api.deleteLessonVideo(video.id).subscribe({
      next: () => {
        this.info.set(this.locale.t('videos.deletedLesson'));
        if (this.selectedMediaAssetId === video.mediaAssetId) this.selectedMediaAssetId = '';
        if (this.preview()?.mediaAssetId === video.mediaAssetId) this.closePreview();
        this.reloadLibrary();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err,'videos.deleteLessonFailed'))
    });
  }

  deleteSolutionVideo(video: TeacherSolutionVideo): void {
    if (!confirm(this.locale.t('videos.confirmDeleteSolution', { title: video.assignmentTitle }))) return;
    this.error.set('');
    this.api.deleteAssignmentSolutionVideo(video.assignmentId).subscribe({
      next: () => {
        this.info.set(this.locale.t('videos.deletedSolution'));
        if (this.preview()?.mediaAssetId === video.mediaAssetId) this.closePreview();
        this.api.getAssignments().subscribe((assignments) => this.assignments.set(assignments));
        this.reloadLibrary();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err,'videos.deleteSolutionFailed'))
    });
  }

  inspectWatch(mediaAssetId: string): void {
    this.selectedMediaAssetId = mediaAssetId;
    this.setTab('analytics');
    this.loadWatchSessions();
  }

  loadWatchSessions(): void {
    if (!this.selectedMediaAssetId) {
      this.error.set(this.locale.t('videos.selectMediaFirst'));
      return;
    }
    this.api.getWatchSessions(this.selectedMediaAssetId).subscribe({
      next: (sessions) => this.watchSessions.set(sessions),
      error: (err) => this.error.set(this.locale.fromApiError(err,'videos.loadSessionsFailed'))
    });
  }
}
