import { Component, Input, OnChanges, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { AvailabilityPeriod, TeacherVacation } from '../../models';
import { PageFeedbackComponent } from '../page-feedback/page-feedback.component';
import { TranslatePipe } from '../translate.pipe';

type PeriodRow = { from: string; to: string };
type DayRow = { dayOfWeek: number; periods: PeriodRow[] };

const DAYS = [0, 1, 2, 3, 4, 5, 6];
const MAX_PERIODS_PER_DAY = 12;

/** Weekly availability (several from–to periods per day) for one teacher or student, plus teacher vacations. */
@Component({
  selector: 'app-availability-editor',
  imports: [FormsModule, TranslatePipe, PageFeedbackComponent],
  templateUrl: './availability-editor.component.html',
  styleUrls: ['../../pages/admin/admin-panel.css', './availability-editor.component.css']
})
export class AvailabilityEditorComponent implements OnChanges {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);

  @Input({ required: true }) userId = '';
  /** Vacations only exist for teachers. */
  @Input() showVacations = false;

  readonly days = signal<DayRow[]>(emptyWeek());
  readonly vacations = signal<TeacherVacation[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly message = signal('');
  readonly error = signal('');
  readonly maxPeriodsPerDay = MAX_PERIODS_PER_DAY;

  vacationFrom = '';
  vacationTo = '';
  vacationNote = '';

  ngOnChanges(): void {
    this.message.set('');
    this.error.set('');
    this.days.set(emptyWeek());
    this.vacations.set([]);
    if (this.userId) {
      this.load();
    }
  }

  dayLabel(dayOfWeek: number): string {
    // 4 Jan 2026 is a Sunday.
    return new Date(2026, 0, 4 + dayOfWeek).toLocaleDateString(this.locale.lang(), { weekday: 'long' });
  }

  addPeriod(day: DayRow): void {
    const last = day.periods[day.periods.length - 1];
    // Start the new period where the previous one ended, an hour long.
    const start = last ? toMinutes(last.to) ?? 8 * 60 : 8 * 60;
    const from = Math.min(start, 23 * 60);
    day.periods.push({ from: toTimeInput(from), to: toTimeInput(Math.min(from + 60, 23 * 60 + 59)) });
    this.days.update((days) => [...days]);
  }

  removePeriod(day: DayRow, index: number): void {
    day.periods.splice(index, 1);
    this.days.update((days) => [...days]);
  }

  save(): void {
    this.message.set('');
    this.error.set('');
    const periods = this.collectPeriods();
    if (!periods) return;

    this.saving.set(true);
    this.api.saveAvailability(this.userId, periods).subscribe({
      next: (saved) => {
        this.saving.set(false);
        this.days.set(toWeek(saved));
        this.message.set(this.locale.t('availability.saved'));
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(this.locale.fromApiError(err, 'availability.saveFailed'));
      }
    });
  }

  addVacation(): void {
    this.message.set('');
    this.error.set('');
    if (!this.vacationFrom || !this.vacationTo) {
      this.error.set(this.locale.t('availability.vacationDatesRequired'));
      return;
    }
    if (this.vacationTo < this.vacationFrom) {
      this.error.set(this.locale.t('availability.vacationOrder'));
      return;
    }

    this.api
      .createTeacherVacation({
        teacherId: this.userId,
        startDate: this.vacationFrom,
        endDate: this.vacationTo,
        note: this.vacationNote.trim() || null
      })
      .subscribe({
        next: () => {
          this.vacationFrom = '';
          this.vacationTo = '';
          this.vacationNote = '';
          this.message.set(this.locale.t('availability.vacationAdded'));
          this.loadVacations();
        },
        error: (err) => this.error.set(this.locale.fromApiError(err, 'availability.vacationFailed'))
      });
  }

  removeVacation(vacation: TeacherVacation): void {
    this.message.set('');
    this.error.set('');
    this.api.deleteTeacherVacation(vacation.id).subscribe({
      next: () => this.loadVacations(),
      error: (err) => this.error.set(this.locale.fromApiError(err, 'availability.vacationFailed'))
    });
  }

  formatDate(value: string): string {
    const date = new Date(`${value}T00:00:00`);
    return Number.isNaN(date.getTime())
      ? value
      : date.toLocaleDateString(this.locale.lang(), { year: 'numeric', month: 'short', day: 'numeric' });
  }

  private load(): void {
    const userId = this.userId;
    this.loading.set(true);
    this.api.getAvailability(userId).subscribe({
      next: (periods) => {
        if (userId !== this.userId) return;
        this.days.set(toWeek(periods));
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(this.locale.fromApiError(err, 'availability.loadFailed'));
      }
    });
    this.loadVacations();
  }

  private loadVacations(): void {
    if (!this.showVacations) return;
    const userId = this.userId;
    this.api.getTeacherVacations(userId).subscribe({
      next: (vacations) => {
        if (userId === this.userId) this.vacations.set(vacations);
      },
      error: (err) => this.error.set(this.locale.fromApiError(err, 'availability.loadFailed'))
    });
  }

  /** Validates the rows and converts them to API periods; sets the error and returns null when invalid. */
  private collectPeriods(): AvailabilityPeriod[] | null {
    const result: AvailabilityPeriod[] = [];
    for (const day of this.days()) {
      const sorted = day.periods
        .map((p) => ({ start: toMinutes(p.from), end: toMinutes(p.to) }))
        .sort((a, b) => (a.start ?? 0) - (b.start ?? 0));
      let previousEnd = -1;
      for (const period of sorted) {
        if (period.start == null || period.end == null || period.start >= period.end) {
          this.error.set(this.locale.t('availability.periodInvalid', { day: this.dayLabel(day.dayOfWeek) }));
          return null;
        }
        if (period.start < previousEnd) {
          this.error.set(this.locale.t('availability.periodOverlap', { day: this.dayLabel(day.dayOfWeek) }));
          return null;
        }
        previousEnd = period.end;
        result.push({ dayOfWeek: day.dayOfWeek, startMinutes: period.start, endMinutes: period.end });
      }
    }
    return result;
  }
}

function emptyWeek(): DayRow[] {
  return DAYS.map((dayOfWeek) => ({ dayOfWeek, periods: [] }));
}

function toWeek(periods: AvailabilityPeriod[]): DayRow[] {
  const week = emptyWeek();
  for (const period of [...periods].sort((a, b) => a.startMinutes - b.startMinutes)) {
    week[period.dayOfWeek]?.periods.push({
      from: toTimeInput(period.startMinutes),
      // A period ending at midnight cannot be typed into a time input; show the last minute of the day.
      to: toTimeInput(Math.min(period.endMinutes, 23 * 60 + 59))
    });
  }
  return week;
}

function toMinutes(value: string): number | null {
  const match = /^(\d{1,2}):(\d{2})$/.exec((value ?? '').trim());
  if (!match) return null;
  const minutes = Number(match[1]) * 60 + Number(match[2]);
  return minutes >= 0 && minutes < 24 * 60 ? minutes : null;
}

function toTimeInput(totalMinutes: number): string {
  return `${String(Math.floor(totalMinutes / 60)).padStart(2, '0')}:${String(totalMinutes % 60).padStart(2, '0')}`;
}
