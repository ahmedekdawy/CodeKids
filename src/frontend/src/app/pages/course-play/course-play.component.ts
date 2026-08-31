import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../auth.service';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { Course, CourseVideoSummary } from '../../models';
import { ProtectedVideoPlayerComponent } from '../../shared/protected-video-player/protected-video-player.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { ApiBusyIndicatorComponent } from '../../shared/api-busy-indicator/api-busy-indicator.component';

@Component({
  selector: 'app-course-play',
  imports: [RouterLink, ProtectedVideoPlayerComponent, TranslatePipe, ApiBusyIndicatorComponent],
  templateUrl: './course-play.component.html',
  styleUrl: '../lesson-play/lesson-play.component.css'
})
export class CoursePlayComponent {
  private readonly api = inject(LearningApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly locale = inject(LocaleService);
  private readonly auth = inject(AuthService);

  readonly backLink = computed(() => {
    const role = this.auth.user()?.role;
    if (role === 'Teacher') return '/teacher/videos';
    if (role === 'SuperAdmin') return '/admin/videos';
    return '/student';
  });

  readonly backLabelKey = computed(() => {
    const role = this.auth.user()?.role;
    return role === 'Teacher' || role === 'SuperAdmin' ? 'play.backToVideos' : 'common.backMissions';
  });

  readonly course = signal<Course | null>(null);
  readonly selectedVideo = signal<CourseVideoSummary | null>(null);
  readonly error = signal('');

  constructor() {
    const courseId = this.route.snapshot.paramMap.get('courseId')!;
    const requestedVideoId = this.route.snapshot.queryParamMap.get('video');
    this.api.getCourse(courseId).subscribe({
      next: (course) => {
        this.course.set(course);
        const videos = course.videos ?? [];
        this.selectedVideo.set(
          videos.find((v) => v.id === requestedVideoId) ?? videos[0] ?? null
        );
      },
      error: () => this.error.set(this.locale.t('play.courseNotFound'))
    });
  }

  chooseVideo(video: CourseVideoSummary): void {
    this.selectedVideo.set(video);
  }
}
