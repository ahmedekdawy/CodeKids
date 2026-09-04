import { Component, OnInit, inject, input, signal } from '@angular/core';
import { LearningApiService } from '../../learning-api.service';
import { TopWeeklyStudent } from '../../models';
import { LocaleService } from '../../i18n/locale.service';
import { formatGradeLabel } from '../../grade.util';
import { TranslatePipe } from '../translate.pipe';

@Component({
  selector: 'app-top-students-board',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './top-students-board.component.html',
  styleUrl: './top-students-board.component.css'
})
export class TopStudentsBoardComponent implements OnInit {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  /** compact = login sidebar; section = landing page block */
  readonly variant = input<'compact' | 'section'>('section');

  readonly students = signal<TopWeeklyStudent[]>([]);
  readonly loaded = signal(false);

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
