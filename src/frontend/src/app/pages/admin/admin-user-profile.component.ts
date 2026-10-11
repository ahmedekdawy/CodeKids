import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { formatGradeLabel, formatStageLabel } from '../../grade.util';
import { LocaleService } from '../../i18n/locale.service';
import { LearningApiService } from '../../learning-api.service';
import { ManagedUser } from '../../models';
import { AvailabilityEditorComponent } from '../../shared/availability-editor/availability-editor.component';
import { TranslatePipe } from '../../shared/translate.pipe';
import { UserPhotoComponent } from '../../shared/user-photo/user-photo.component';
import { SiteBrandService } from '../../site-brand.service';
import { StyledField, showsField } from '../../tenant-style';

type DetailRow = { labelKey: string; value: string };

/** Read-only profile of one teacher, student or parent, opened from the admin lists. */
@Component({
  selector: 'app-admin-user-profile',
  imports: [RouterLink, TranslatePipe, UserPhotoComponent, AvailabilityEditorComponent],
  templateUrl: './admin-user-profile.component.html',
  styleUrls: ['./admin-panel.css', './admin-user-profile.component.css']
})
export class AdminUserProfileComponent {
  private readonly api = inject(LearningApiService);
  private readonly locale = inject(LocaleService);
  private readonly brand = inject(SiteBrandService);

  readonly user = signal<ManagedUser | null>(null);
  /** A student's parent, when they have one. */
  readonly parent = signal<ManagedUser | null>(null);
  /** A parent's children. */
  readonly children = signal<ManagedUser[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');

  /** The list page this profile was opened from. */
  readonly listPath = computed(() => {
    switch (this.user()?.role) {
      case 'Teacher':
        return '/admin/teachers';
      case 'Parent':
        return '/admin/parents';
      case 'SuperAdmin':
        return '/admin/admins';
      default:
        return '/admin/students';
    }
  });

  readonly roleLabel = computed(() => {
    const role = this.user()?.role;
    return role ? this.locale.t(`role.${role.charAt(0).toLowerCase()}${role.slice(1)}`) : '';
  });

  readonly details = computed<DetailRow[]>(() => {
    const user = this.user();
    if (!user) return [];
    this.locale.lang();

    const dash = this.locale.t('common.emDash');
    const rows: DetailRow[] = [
      { labelKey: 'common.email', value: user.email || dash },
      { labelKey: 'common.mobile', value: user.mobilePhone || dash }
    ];
    const add = (labelKey: string, value: string | number | null | undefined) =>
      rows.push({ labelKey, value: value == null || value === '' ? dash : String(value) });

    if (user.role === 'Student') {
      add('common.grade', user.grade == null ? null : formatGradeLabel((k, p) => this.locale.t(k, p), user.grade));
      if (this.shows('studentSchoolType')) add('common.schoolType', this.schoolTypeLabel(user.schoolType));
      add('common.gender', this.genderLabel(user.gender));
      if (this.brand.multiTimeZone()) add('common.timeZone', user.timeZone?.replace(/_/g, ' '));
      if (this.shows('studentSessions')) {
        add('admin.students.sessionMinutes', user.sessionMinutes);
        add('admin.students.sessionsPerWeek', user.sessionsPerWeek);
      }
      add('common.xp', user.totalXp);
    }

    if (user.role === 'Teacher') {
      add('common.gender', this.genderLabel(user.gender));
      if (this.brand.multiTimeZone()) add('common.timeZone', user.timeZone?.replace(/_/g, ' '));
      if (this.shows('teacherShiftAndStages')) {
        add('admin.users.workShift', this.workShiftLabel(user.workShift));
        add(
          'admin.users.stages',
          [...(user.stages ?? [])]
            .sort((a, b) => a - b)
            .map((s) => formatStageLabel((k, p) => this.locale.t(k, p), s))
            .join(', ')
        );
      }
      if (this.shows('teacherPay')) {
        add('admin.users.contractType', this.contractTypeLabel(user.contractType));
        add('admin.users.teacherMonthlySalary', user.monthlySalary);
      }
      if (this.shows('teacherStageAmounts')) {
        add('admin.users.primaryAmount', user.primaryAmount);
        add('admin.users.prepAmount', user.prepAmount);
        add('admin.users.secondaryAmount', user.secondaryAmount);
      }
    }

    return rows;
  });

  constructor() {
    inject(ActivatedRoute)
      .paramMap.pipe(takeUntilDestroyed())
      .subscribe((params) => this.load(params.get('id') ?? ''));
  }

  shows(field: StyledField): boolean {
    return showsField(this.brand.tenantStyle(), field);
  }

  isActive(user: ManagedUser): boolean {
    return user.isActive !== false;
  }

  gradeLabel(grade?: number | null): string {
    return grade == null ? this.locale.t('common.emDash') : formatGradeLabel((k, p) => this.locale.t(k, p), grade);
  }

  moneyLabel(value?: number | null): string {
    return value == null ? this.locale.t('common.emDash') : String(value);
  }

  private load(id: string): void {
    this.user.set(null);
    this.parent.set(null);
    this.children.set([]);
    this.error.set('');
    this.loading.set(true);

    this.api.getUser(id).subscribe({
      next: (user) => {
        this.user.set(user);
        this.loading.set(false);
        this.loadRelatives(user);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(this.locale.fromApiError(err, 'admin.profile.notFound'));
      }
    });
  }

  private loadRelatives(user: ManagedUser): void {
    if (user.role === 'Student' && user.parentId) {
      this.api.getUser(user.parentId).subscribe({
        next: (parent) => {
          if (this.user()?.id === user.id) this.parent.set(parent);
        },
        error: () => undefined
      });
    }

    if (user.role === 'Parent') {
      this.api.getUsers('Student').subscribe({
        next: (students) => {
          if (this.user()?.id === user.id) this.children.set(students.filter((s) => s.parentId === user.id));
        },
        error: () => undefined
      });
    }
  }

  private genderLabel(gender?: string | null): string {
    if (gender === 'Male') return this.locale.t('common.genderMale');
    if (gender === 'Female') return this.locale.t('common.genderFemale');
    return '';
  }

  private schoolTypeLabel(value?: string | null): string {
    if (value === 'Arabic') return this.locale.t('common.schoolTypeArabic');
    if (value === 'Language') return this.locale.t('common.schoolTypeLanguage');
    return value || '';
  }

  private workShiftLabel(shift?: string | null): string {
    if (shift === 'Am') return this.locale.t('admin.users.shiftAm');
    if (shift === 'Pm') return this.locale.t('admin.users.shiftPm');
    if (shift === 'Both') return this.locale.t('admin.users.shiftBoth');
    return shift || '';
  }

  private contractTypeLabel(type?: string | null): string {
    if (type === 'Session') return this.locale.t('admin.users.contractSession');
    if (type === 'Monthly') return this.locale.t('admin.users.contractMonthly');
    return type || '';
  }
}
