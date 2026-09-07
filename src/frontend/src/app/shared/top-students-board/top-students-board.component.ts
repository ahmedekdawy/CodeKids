import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { LearningApiService } from '../../learning-api.service';
import { TopWeeklyStudent } from '../../models';
import { LocaleService } from '../../i18n/locale.service';
import { formatGradeLabel } from '../../grade.util';
import { TranslatePipe } from '../translate.pipe';
import { UserPhotoComponent } from '../user-photo/user-photo.component';

@Component({
  selector: 'app-top-students-board',
  standalone: true,
  imports: [TranslatePipe, UserPhotoComponent],
  templateUrl: './top-students-board.component.html',
  styleUrl: './top-students-board.component.css'
})
export class TopStudentsBoardComponent implements OnInit {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  /** showcase = login page panel; section = landing page block */
  readonly variant = input<'showcase' | 'section'>('section');

  readonly students = signal<TopWeeklyStudent[]>([]);
  readonly loaded = signal(false);

  /** Read by the login page so it can fall back to its decorative card when nobody qualifies. */
  readonly hasStudents = computed(() => this.loaded() && this.students().length > 0);

  ngOnInit(): void {
    this.api.listTopWeeklyStudents().subscribe({
      next: (rows) => {
        this.students.set(rows ?? []);
        this.loaded.set(true);
      },
      error: () => {
        this.students.set([]);
        this.loaded.set(true);
      }
    });
  }

  metaLine(student: TopWeeklyStudent): string {
    const parts: string[] = [];
    if (student.studentGrade != null) {
      parts.push(this.gradeLabel(student.studentGrade));
    }
    if (student.subjectCount > 0) {
      parts.push(this.locale.t('topStudents.subjectCount', { count: student.subjectCount }));
    }
    return parts.join(' · ');
  }

  gradeLabel(grade: number | null | undefined): string {
    return formatGradeLabel((k, p) => this.locale.t(k, p), grade);
  }

  rankMedal(index: number): string {
    if (index === 0) return '🥇';
    if (index === 1) return '🥈';
    if (index === 2) return '🥉';
    return `${index + 1}`;
  }
}
