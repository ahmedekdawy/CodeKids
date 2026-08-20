import { Component, computed, inject, input, signal } from '@angular/core';
import { LocaleService } from '../../i18n/locale.service';
import { WeeklyStudyPlan } from '../../models';
import { formatCourseLabel } from '../../grade.util';
import { TranslatePipe } from '../translate.pipe';
import { StudyPlanSheetComponent } from './study-plan-sheet.component';

@Component({
  selector: 'app-study-plan-viewer',
  imports: [StudyPlanSheetComponent, TranslatePipe],
  templateUrl: './study-plan-viewer.component.html',
  styleUrl: './study-plan-viewer.component.css'
})
export class StudyPlanViewerComponent {
  private readonly locale = inject(LocaleService);

  readonly plans = input<WeeklyStudyPlan[]>([]);
  readonly showTeacher = input(false);
  readonly emptyKey = input('common.noData');

  private readonly selectedId = signal<string | null>(null);

  readonly selected = computed(() => {
    const plans = this.plans();
    const id = this.selectedId();
    return plans.find((plan) => plan.id === id) ?? plans[0] ?? null;
  });

  select(plan: WeeklyStudyPlan): void {
    this.selectedId.set(plan.id);
  }

  isSelected(plan: WeeklyStudyPlan): boolean {
    return this.selected()?.id === plan.id;
  }

  planLabel(plan: WeeklyStudyPlan): string {
    const course = formatCourseLabel((k, p) => this.locale.t(k, p), plan.courseName, plan.courseGrade, 'common.allGrades', plan.courseStageId);
    const range = `${plan.fromDate} – ${plan.toDate}`;
    return this.showTeacher() && plan.teacherName ? `${course} · ${plan.teacherName} · ${range}` : `${course} · ${range}`;
  }
}
