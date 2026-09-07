import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LearningApiService } from '../../learning-api.service';
import { LocaleService } from '../../i18n/locale.service';
import { IconActionButtonComponent } from '../../shared/icon-action-button/icon-action-button.component';
import { ProtectedVideoPlayerComponent } from '../../shared/protected-video-player/protected-video-player.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { Course, CourseVideoLibraryItem } from '../../models';
import { formatCourseLabel } from '../../grade.util';
import { SearchableSelectComponent } from '../../shared/searchable-select/searchable-select.component';
import { PageFeedbackComponent } from '../../shared/page-feedback/page-feedback.component';

interface VideoPreview {
  mediaAssetId: string;
  title: string;
}

@Component({
  selector: 'app-admin-videos',
  imports: [
    PageFeedbackComponent,
    SearchableSelectComponent,
    FormsModule,
    IconActionButtonComponent,
    ProtectedVideoPlayerComponent,
    TranslatePipe
  ],
  templateUrl: './admin-videos.component.html',
  styleUrls: ['./admin-panel.css', '../teacher/teacher-videos.component.css']
})
export class AdminVideosComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  readonly courses = signal<Course[]>([]);
  readonly videos = signal<CourseVideoLibraryItem[]>([]);
  readonly info = signal('');
  readonly error = signal('');
  readonly uploading = signal(false);
  readonly videoFile = signal<File | null>(null);
  readonly preview = signal<VideoPreview | null>(null);
  readonly previewKey = signal(0);

  selectedCourseId = '';
  videoTitle = '';
  videoUrl = '';
  readonly filterCourseId = signal('');
  readonly search = signal('');

  readonly filteredVideos = computed(() => {
    let list = this.videos();
    const courseId = this.filterCourseId();
    if (courseId) {
      list = list.filter((v) => v.courseId === courseId);
    }
    const q = this.search().trim().toLowerCase();
    if (q) {
      list = list.filter(
        (v) =>
          v.title.toLowerCase().includes(q) ||
          v.fileName.toLowerCase().includes(q) ||
          v.courseTitle.toLowerCase().includes(q)
      );
    }
    return list;
  });

  constructor() {
    this.api.getCourses(false).subscribe((courses) => {
      this.courses.set(courses);
      if (courses[0]) this.selectedCourseId = courses[0].id;
    });
    this.reloadLibrary();
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

  courseLabelById(courseId: string, fallbackTitle?: string | null): string {
    const course = this.courses().find((c) => c.id === courseId);
    if (course) return this.courseLabel(course);
    return formatCourseLabel((k, p) => this.locale.t(k, p), fallbackTitle, null);
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
    this.api.getCourseVideoLibrary().subscribe({
      next: (videos) => this.videos.set(videos || []),
      error: (err) => this.error.set(this.locale.fromApiError(err, 'videos.loadLibraryFailed'))
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.videoFile.set(input.files?.[0] ?? null);
  }

  openPreview(item: VideoPreview): void {
    this.preview.set(item);
    this.previewKey.update((k) => k + 1);
  }

  closePreview(): void {
    this.preview.set(null);
  }

  playVideo(video: CourseVideoLibraryItem): void {
    this.openPreview({ mediaAssetId: video.mediaAssetId, title: video.title });
  }

  attachLink(): void {
    const url = this.videoUrl.trim();
    if (!url || !this.selectedCourseId) {
      this.error.set(this.locale.t('videos.linkRequired'));
      return;
    }
    this.saveFromAsset((title) => this.api.registerMediaFromUrl({ url, title: title || null }), false);
  }

  attachFile(): void {
    const file = this.videoFile();
    if (!file || !this.selectedCourseId) {
      this.error.set(this.locale.t('videos.fileRequired'));
      return;
    }
    this.saveFromAsset(() => this.api.uploadMedia(file), true);
  }

  private saveFromAsset(
    createAsset: (title: string) => ReturnType<LearningApiService['uploadMedia']>,
    fromFile: boolean
  ): void {
    this.error.set('');
    this.info.set('');
    this.uploading.set(true);
    const title = this.videoTitle.trim();

    createAsset(title).subscribe({
      next: (asset) => {
        this.api
          .attachCourseVideo(this.selectedCourseId, {
            mediaAssetId: asset.id,
            title: title || asset.fileName,
            sortOrder: 1
          })
          .subscribe({
            next: () => {
              this.uploading.set(false);
              this.info.set(
                fromFile
                  ? this.locale.t('videos.uploadedCourseFile')
                  : this.locale.t('videos.uploadedCourse')
              );
              this.videoTitle = '';
              this.videoUrl = '';
              this.videoFile.set(null);
              this.reloadLibrary();
            },
            error: (err) => {
              this.uploading.set(false);
              this.error.set(this.locale.fromApiError(err, 'videos.attachCourseFailed'));
            }
          });
      },
      error: (err) => {
        this.uploading.set(false);
        this.error.set(this.locale.fromApiError(err, 'videos.uploadFailed'));
      }
    });
  }

  deleteVideo(video: CourseVideoLibraryItem): void {
    if (!confirm(this.locale.t('videos.confirmDeleteCourse', { title: video.title }))) return;
    this.error.set('');
    this.api.deleteCourseVideo(video.id).subscribe({
      next: () => {
        this.info.set(this.locale.t('videos.deletedCourse'));
        if (this.preview()?.mediaAssetId === video.mediaAssetId) this.closePreview();
        this.reloadLibrary();
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'videos.deleteCourseFailed'))
    });
  }

  clearFilters(): void {
    this.filterCourseId.set('');
    this.search.set('');
  }
}
